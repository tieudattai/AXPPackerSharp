using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace axp.Dbc
{
    public partial class DbcFile
    {
        const uint DBC_IDENTITY = 0xDDBBCC00;
        /// <summary>
        /// Mã hóa văn bản
        /// </summary>
        private static Encoding _textEncoding = Encoding.GetEncoding("GB18030");
        /// <summary>
        /// Danh sách các trường dữ liệu
        /// </summary>
        public List<DbcFieldType> FieldTypes;
        /// <summary>
        /// Danh sách tên trường
        /// </summary>
        public List<string>? FieldNames;
        /// <summary>
        /// Từ điển, [Mã ID => Hàng dữ liệu]
        /// </summary>
        public SortedDictionary<int, List<DbcField>> DataMap;

        private DbcFile(List<DbcFieldType> fieldTypes, SortedDictionary<int, List<DbcField>> dataMap)
        {
            FieldTypes = fieldTypes;
            DataMap = dataMap;
        }
        private DbcFile(List<DbcFieldType> fieldTypes, List<string>? fieldNames, SortedDictionary<int, List<DbcField>> dataMap) : this(fieldTypes, dataMap)
        {
            FieldNames = fieldNames;
        }
        private static async Task ReadBytesAsync(Stream stream, byte[] data)
        {
            var offset = 0;
            while (offset < data.Length)
            {
                var count = await stream.ReadAsync(data, offset, data.Length - offset);
                offset += count;
            }
        }


        public static async Task<DbcFile> ReadAsync(Stream stream, uint limit)
        {
            var data = new byte[4];
            await ReadBytesAsync(stream, data);
            var headFlag = BitConverter.ToUInt32(data, 0);
            if (headFlag == DBC_IDENTITY)
            {
                return await ReadBinaryAsync(stream);
            }
            stream.Seek(-4, SeekOrigin.Current);
            return await ReadTextAsync(stream, limit);
        }

        public static async Task<string> ReadTableAsync(Stream stream, uint limit)
        {
            var data = new byte[4];
            await ReadBytesAsync(stream, data);
            stream.Seek(-4, SeekOrigin.Current);
            return await ReadStringAsync(stream, limit);
        }

        public static async Task<byte[]> ReadByteAsync(Stream stream, uint limit)
        {
            var data = new byte[limit];
            stream.Seek(-4, SeekOrigin.Current);
            await ReadBytesAsync(stream, data);
            return data;
        }

        public static async Task<DbcFile> ReadAsync(Stream stream, uint limit, Encoding encoding)
        {
            var data = new byte[4];
            await ReadBytesAsync(stream, data);
            var headFlag = BitConverter.ToUInt32(data, 0);
            if (headFlag == DBC_IDENTITY)
            {
                return await ReadBinaryAsync(stream, encoding);
            }
            stream.Seek(-4, SeekOrigin.Current);
            return await ReadTextAsync(stream, limit, encoding);
        }
    }
}