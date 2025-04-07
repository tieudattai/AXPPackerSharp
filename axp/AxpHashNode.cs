using System.Runtime.InteropServices;

namespace axp.Core
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct AxpHashNode
    {
        /// <summary>
        /// Giá trị băm A, được sử dụng để xác minh
        /// </summary>
        /// 

        public uint HashA;
        /// <summary>
        /// Giá trị băm B, được sử dụng để xác minh
        /// </summary>
        public uint HashB;
        /// <summary>
        /// Dữ liệu
        /// </summary>
        public uint Data;


        public bool Exists()
        {
            return (Data & 0x8000_0000) != 0;
        }

        public uint BlockIndex()
        {
            return Data & 0x3FFF_FFFF;
        }
    }
}