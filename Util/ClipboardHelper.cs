using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace BrewLib.Util
{
    public interface ClipboardBackend
    {
        void SetText(string text);
        string GetText();
    }

    public static class ClipboardHelper
    {
        /// <summary>
        /// The system clipboard, set by the application on Windows.
        /// When it is null or fails, text is only shared within this process.
        /// </summary>
        public static ClipboardBackend Backend { get; set; } = OperatingSystem.IsWindows() ? null : CommandLineClipboard.Find();

        private static string localText;

        public static void SetText(string text)
        {
            localText = text;
            if (Backend != null)
                Misc.WithRetries(() => Backend.SetText(text), 500, false);
        }

        public static string GetText()
        {
            if (Backend == null)
                return localText;

            return Misc.WithRetries(() => Backend.GetText(), 500, false) ?? localText;
        }
    }

    /// <summary>
    /// Uses wl-clipboard on Wayland, or xclip / xsel on X11.
    /// </summary>
    public class CommandLineClipboard : ClipboardBackend
    {
        private const int timeout = 2000;

        private readonly string copyCommand;
        private readonly string[] copyArguments;
        private readonly string pasteCommand;
        private readonly string[] pasteArguments;

        public CommandLineClipboard(string copyCommand, string[] copyArguments, string pasteCommand, string[] pasteArguments)
        {
            this.copyCommand = copyCommand;
            this.copyArguments = copyArguments;
            this.pasteCommand = pasteCommand;
            this.pasteArguments = pasteArguments;
        }

        public static CommandLineClipboard Find()
        {
            if (Environment.GetEnvironmentVariable("WAYLAND_DISPLAY") != null)
            {
                var wlCopy = findExecutable("wl-copy");
                var wlPaste = findExecutable("wl-paste");
                if (wlCopy != null && wlPaste != null)
                    return new CommandLineClipboard(wlCopy, new string[0], wlPaste, new[] { "--no-newline" });
            }

            var xclip = findExecutable("xclip");
            if (xclip != null)
                return new CommandLineClipboard(xclip, new[] { "-selection", "clipboard", "-in" }, xclip, new[] { "-selection", "clipboard", "-out" });

            var xsel = findExecutable("xsel");
            if (xsel != null)
                return new CommandLineClipboard(xsel, new[] { "--clipboard", "--input" }, xsel, new[] { "--clipboard", "--output" });

            Trace.WriteLine("No clipboard tool found (wl-clipboard, xclip or xsel), the clipboard will only work within storybrew");
            return null;
        }

        public void SetText(string text)
        {
            // Output isn't redirected: these tools keep running in the background to serve the clipboard
            // and would keep the pipes open.
            var startInfo = new ProcessStartInfo(copyCommand)
            {
                UseShellExecute = false,
                RedirectStandardInput = true,
            };
            foreach (var argument in copyArguments)
                startInfo.ArgumentList.Add(argument);

            using (var process = Process.Start(startInfo))
            {
                process.StandardInput.Write(text);
                process.StandardInput.Close();
                process.WaitForExit(timeout);
            }
        }

        public string GetText()
        {
            var startInfo = new ProcessStartInfo(pasteCommand)
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };
            foreach (var argument in pasteArguments)
                startInfo.ArgumentList.Add(argument);

            using (var process = Process.Start(startInfo))
            {
                var output = process.StandardOutput.ReadToEndAsync();
                process.StandardError.ReadToEndAsync();
                if (!process.WaitForExit(timeout))
                {
                    process.Kill();
                    return null;
                }

                // An empty clipboard is reported as an error
                return process.ExitCode == 0 ? output.Result : string.Empty;
            }
        }

        private static string findExecutable(string name)
            => (Environment.GetEnvironmentVariable("PATH") ?? string.Empty)
                .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
                .Select(directory => Path.Combine(directory, name))
                .FirstOrDefault(File.Exists);
    }
}
