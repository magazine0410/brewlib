using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace BrewLib.Util
{
    public static class Native
    {
        public static unsafe void memcpy(IntPtr dest, IntPtr src, uint count)
            => Buffer.MemoryCopy((void*)src, (void*)dest, count, count);

        [DllImport("user32.dll")]
        public static extern void SwitchToThisWindow(IntPtr hWnd, bool fAltTab);

        [DllImport("user32.dll")]
        public static extern bool EnumThreadWindows(int dwThreadId, EnumThreadDelegate lpfn, IntPtr lParam);
        public delegate bool EnumThreadDelegate(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll")]
        public static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll")]
        public static extern int GetWindowTextLength(IntPtr hWnd);

        public static string GetWindowText(IntPtr hWnd)
        {
            var length = GetWindowTextLength(hWnd);
            if (length == 0) return string.Empty;

            var sb = new StringBuilder(length);
            GetWindowText(hWnd, sb, length + 1);
            return sb.ToString();
        }

        public static IEnumerable<IntPtr> EnumerateProcessWindowHandles(Process process)
        {
            var handles = new List<IntPtr>();
            foreach (ProcessThread thread in process.Threads)
                EnumThreadWindows(thread.Id, (hWnd, lParam) =>
                {
                    handles.Add(hWnd);
                    return true;
                },
                IntPtr.Zero);

            return handles;
        }

        public static IntPtr FindProcessWindow(string title)
        {
            if (!OperatingSystem.IsWindows())
                return IntPtr.Zero;

            foreach (var hWnd in EnumerateProcessWindowHandles(Process.GetCurrentProcess()))
                if (GetWindowText(hWnd) == title)
                    return hWnd;
            return IntPtr.Zero;
        }

        public static unsafe IntPtr MemSet(IntPtr dest, int value, int count)
        {
            new Span<byte>((void*)dest, count).Fill((byte)value);
            return dest;
        }

        public static bool ArrayEquals(byte[] b1, byte[] b2)
            => b1.AsSpan().SequenceEqual(b2);
    }
}
