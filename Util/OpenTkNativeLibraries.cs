using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;

namespace BrewLib.Util
{
    /// <summary>
    /// OpenTK 3 imports some native libraries by unversioned names and relies on Mono's OpenTK.dll.config to map them,
    /// which .NET ignores. Without this, those names only resolve when the distribution's -devel packages are installed.
    /// </summary>
    public static class OpenTkNativeLibraries
    {
        // SDL2.dll is left unmapped to keep OpenTK on its X11 backend.
        private static readonly Dictionary<string, string> linuxLibraries = new Dictionary<string, string>()
        {
            ["libX11"] = "libX11.so.6",
            ["libXi"] = "libXi.so.6",
            ["libXinerama"] = "libXinerama.so.1",
            ["libXxf86vm"] = "libXxf86vm.so.1",
        };

        private static bool registered;

        public static void Register()
        {
            if (registered || !OperatingSystem.IsLinux())
                return;

            registered = true;
            NativeLibrary.SetDllImportResolver(typeof(OpenTK.Toolkit).Assembly, resolve);
        }

        private static IntPtr resolve(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
        {
            if (linuxLibraries.TryGetValue(libraryName, out var mappedName))
            {
                if (NativeLibrary.TryLoad(mappedName, assembly, searchPath, out var handle))
                    return handle;

                Trace.WriteLine($"Failed to load {mappedName} for {libraryName}");
            }
            return IntPtr.Zero;
        }
    }
}
