using System;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace Ryujinx.Cpu.LightningJit.Cache
{
    [SupportedOSPlatform("macos")]
    static partial class JitSupportDarwin
    {
        [LibraryImport("libarmeilleure-jitsupport", EntryPoint = "armeilleure_jit_memcpy")]
        public static partial void Copy(IntPtr dst, IntPtr src, ulong n);
    }
}
