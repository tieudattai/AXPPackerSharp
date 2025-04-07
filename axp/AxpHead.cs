using System.Runtime.InteropServices;

namespace axp.Core
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct AxpHead
    {
        /// <summary>
        /// Chữ ký 0x4B505841  'AXPK'
        /// </summary>
        //[FieldOffset(0)]
        public uint Identity;
        /// <summary>
        /// Phiên bản (Major|Minor)
        /// </summary>
        /// 
        //[FieldOffset(4)]
        public uint Version;
        /// <summary>
        /// Cờ chỉnh sửa, khi số nguyên này là 1, có nghĩa là tệp đang được chỉnh sửa
        /// </summary>
        /// 
        //[FieldOffset(8)]
        public uint EditFlag;
        /// <summary>
        /// Mảng băm bù đắp trong tệp
        /// </summary>
        /// 
        //[FieldOffset(12)]
        public uint HashTableOffset;
        /// <summary>
        /// Độ lệch của bảng Khối trong tệp
        /// </summary>
        /// 
        //[FieldOffset(16)]
        public uint BlockTableOffset;
        /// <summary>
        /// Số lượng nội dung trong bảng Block
        /// </summary>
        /// 
        //[FieldOffset(18)]
        public uint BlockTableCount;
        /// <summary>
        /// Kích thước tối đa của Block (bytes)
        /// </summary>
        /// 
        //[FieldOffset(24)]
        public uint BlockTableMaxSize;
        /// <summary>
        /// Phần bù của khối dữ liệu trong tệp
        /// </summary>
        /// 
        //[FieldOffset(28)]
        public uint DataOffset;
        /// <summary>
        /// Kích thước của khối dữ liệu, bao gồm cả các lỗ (bytes)
        /// </summary>
        /// 
        //[FieldOffset(32)]
        public uint DataSize;
        /// <summary>
        /// Kích thước của khối dữ liệu trống (bytes)
        /// </summary>
        /// 
        //[FieldOffset(36)]
        public uint DataHoleSize;
       // [FieldOffset(40)]
       // public uint Unknow;
    }
}