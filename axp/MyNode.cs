using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace axp.Core
{
    public class MyNode
    {
        /// <summary>
        /// Giá trị băm A, được sử dụng để xác minh
        /// </summary>
        public uint HashA { get; set; }
        /// <summary>
        /// Giá trị băm B, được sử dụng để xác minh
        /// </summary>
        public uint HashB { get; set; }
        /// <summary>
        /// Dữ liệu
        /// </summary>
        public uint Data { get; set; }

        public uint Offset { get; set; }

        public byte[] Binaries { get; set; }
        public int BlockIndex { get; set; }
    }

    public class MyBlockNode
    {

        /// <summary>
        /// Phần bù của khối dữ liệu tương ứng trong tệp
        /// </summary>
        public uint DataOffset { get; set; }
        /// <summary>
        /// Kích thước tệp tương ứng với khối dữ liệu(bytes)
        /// </summary>
        public uint BlockSize { get; set; }
        /// <summary>
        /// Cờ khối
        /// </summary>
        public uint Flags { get; set; }

        public byte[] Binaries { get; set; }
    }
}
