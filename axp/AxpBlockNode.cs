using System.Runtime.InteropServices;

namespace axp.Core
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]

    public struct AxpBlockNode
    {
        /// <summary>
        /// Phần bù của khối dữ liệu tương ứng trong tệp
        /// </summary>
        public uint DataOffset;
        /// <summary>
        /// Kích thước tệp tương ứng với khối dữ liệu(bytes)
        /// </summary>
        public uint BlockSize;
        /// <summary>
        /// Cờ khối
        /// </summary>
        public uint Flags;
    }
}