using System.Runtime.InteropServices;

namespace Madness
{
    // ReSharper disable InconsistentNaming
    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int Left;        // x position of upper-left corner
        public int Top;         // y position of upper-left corner
        public int Right;       // x position of lower-right corner
        public int Bottom;      // y position of lower-right corner
    }
    // ReSharper restore InconsistentNaming
}