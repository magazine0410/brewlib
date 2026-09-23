using System;
using System.IO;

namespace BrewLib.Util
{
    public static class PathHelper
    {
        /// <summary>
        /// Replaces directory separator by Path.DirectorySeparatorChar
        /// </summary>
        public static string WithPlatformSeparators(string path)
        {
            if (Path.DirectorySeparatorChar != '/')
                path = path.Replace('/', Path.DirectorySeparatorChar);
            if (Path.DirectorySeparatorChar != '\\')
                path = path.Replace('\\', Path.DirectorySeparatorChar);
            return path;
        }

        /// <summary>
        /// Replaces directory separator by a StandardDirectorySeparator
        /// </summary>
        public const char StandardDirectorySeparator = '/';
        public static string WithStandardSeparators(string path)
        {
            if (Path.DirectorySeparatorChar != StandardDirectorySeparator)
                path = path.Replace(Path.DirectorySeparatorChar, StandardDirectorySeparator);
            path = path.Replace('\\', StandardDirectorySeparator);
            return path;
        }

        public static bool FolderContainsPath(string folder, string path)
        {
            folder = WithStandardSeparators(Path.GetFullPath(folder)).TrimEnd('/');
            path = WithStandardSeparators(Path.GetFullPath(path)).TrimEnd('/');

            return path.Length >= folder.Length + 1 && path[folder.Length] == '/' && path.StartsWith(folder);
        }

        public static string GetRelativePath(string folder, string path)
        {
            folder = WithStandardSeparators(Path.GetFullPath(folder)).TrimEnd('/');
            path = WithStandardSeparators(Path.GetFullPath(path)).TrimEnd('/');

            if (path.Length < folder.Length + 1 || path[folder.Length] != '/' || !path.StartsWith(folder))
                throw new ArgumentException(path + " isn't contained in " + folder);

            return path.Substring(folder.Length + 1);
        }

        public static bool IsValidPath(string path)
        {
            foreach (var invalidCharacter in Path.GetInvalidPathChars())
                foreach (var character in path)
                    if (character == invalidCharacter)
                        return false;
            return true;
        }

        public static bool IsValidFilename(string filename)
        {
            foreach (var invalidCharacter in Path.GetInvalidFileNameChars())
                foreach (var character in filename)
                    if (character == invalidCharacter)
                        return false;
            return true;
        }

        /// <summary>
        /// Returns the path of the existing file matching this path while ignoring case and treating backslashes as separators,
        /// since osu! and beatmaps made on Windows rely on both.
        /// Returns the path unchanged if there is no such file, or on Windows.
        /// </summary>
        public static string FindFileIgnoringCase(string path)
        {
            if (OperatingSystem.IsWindows() || string.IsNullOrEmpty(path))
                return path;

            var platformPath = WithPlatformSeparators(path);
            if (File.Exists(platformPath))
                return platformPath;

            try
            {
                var fullPath = Path.GetFullPath(platformPath);
                var root = Path.GetPathRoot(fullPath);

                var current = root;
                foreach (var part in fullPath.Substring(root.Length).Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries))
                {
                    var next = Path.Combine(current, part);
                    if (!Directory.Exists(next) && !File.Exists(next))
                    {
                        next = null;
                        foreach (var entry in Directory.EnumerateFileSystemEntries(current))
                            if (string.Equals(Path.GetFileName(entry), part, StringComparison.OrdinalIgnoreCase))
                            {
                                next = entry;
                                break;
                            }
                        if (next == null)
                            return path;
                    }
                    current = next;
                }
                return File.Exists(current) ? current : path;
            }
            catch (IOException)
            {
                return path;
            }
            catch (UnauthorizedAccessException)
            {
                return path;
            }
        }
    }
}
