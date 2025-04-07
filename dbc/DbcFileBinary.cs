using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace axp.Dbc
{
    public partial class DbcFile
    {
        private static async Task<DbcFile> ReadBinaryAsync(Stream stream)
        {
            var head = await LoadFileHeadAsync(stream);
            var fieldTypes = await LoadBinaryFieldTypesAsync(stream, head.FieldCount);
            var dataMap = await LoadBinaryDataMapAsync(stream, head, fieldTypes);
            return new(fieldTypes, dataMap);
        }

        private static async Task<DbcFile> ReadBinaryAsync(Stream stream, Encoding encoding)
        {
            var head = await LoadFileHeadAsync(stream);
            var fieldTypes = await LoadBinaryFieldTypesAsync(stream, head.FieldCount);
            var dataMap = await LoadBinaryDataMapAsync(stream, head, fieldTypes, encoding);
            return new(fieldTypes, dataMap);
        }

        private static async Task<DbcHead> LoadFileHeadAsync(Stream stream)
        {
            var headData = new byte[3 * 4];
            await ReadBytesAsync(stream, headData);
            //
            var offset = 0;
            var readNextInt = () =>
            {
                var s = BitConverter.ToInt32(headData, offset);
                offset += 4;
                return s;
            };
            DbcHead head;
            head.Identity = DBC_IDENTITY;
            head.FieldCount = readNextInt();
            head.RecordCount = readNextInt();
            head.StringBlockSize = readNextInt();
            return head;
        }

        private static async Task<List<DbcFieldType>> LoadBinaryFieldTypesAsync(Stream stream, int fieldCount)
        {
            var fieldTypesData = new byte[fieldCount * 4];
            await ReadBytesAsync(stream, fieldTypesData);
            var fieldTypes = new List<DbcFieldType>(fieldCount);
            //
            var offset = 0;
            var readNextInt = () =>
            {
                var s = BitConverter.ToInt32(fieldTypesData, offset);
                offset += 4;
                return s;
            };
            int fieldType;
            for (var i = 0; i < fieldCount; i++)
            {
                fieldType = readNextInt();
                fieldTypes.Add((DbcFieldType)fieldType);
            }
            return fieldTypes;
        }

        private static async Task<SortedDictionary<int, List<DbcField>>> LoadBinaryDataMapAsync(Stream stream, DbcHead head, List<DbcFieldType> fieldTypes)
        {
            //var padNode = new DbcField(string.Empty);
            var dataMap = new SortedDictionary<int, List<DbcField>>();
            //Dữ liệu mỗi hàng
            var rowData = new byte[head.FieldCount * 4];
            var offset = 0;
            var readNextInt = () =>
            {
                var s = BitConverter.ToInt32(rowData, offset);
                offset += 4;
                return s;
            };
            var readNextFloat = () =>
            {
                var s = BitConverter.ToSingle(rowData, offset);
                offset += 4;
                return s;
            };
            //Đọc lần lượt từng dòng
            for (var rowIndex = 0; rowIndex < head.RecordCount; rowIndex++)
            {
                //Đọc dữ liệu hàng
                await ReadBytesAsync(stream, rowData);
                var rowID = readNextInt();
                var row = new List<DbcField>(head.FieldCount)
            {
                //Phần tử đầu tiên của mỗi dòng
                new(rowID)
            };
                //Đọc phần tử còn lại
                for (var fieldIndex = 1; fieldIndex < head.FieldCount; fieldIndex++)
                {
                    var fieldType = fieldTypes[fieldIndex];
                    if (fieldType == DbcFieldType.T_FLOAT)
                    {
                        row.Add(new(readNextFloat()));
                    }
                    else
                    {
                        row.Add(new(readNextInt()));
                    }

                }
                //Phần bù đắp
                offset = 0;
                dataMap[rowID] = row;
            }
            await LoadBinaryStringBlockAsync(stream, head.StringBlockSize, dataMap, fieldTypes);
            return dataMap;
        }

        private static async Task<SortedDictionary<int, List<DbcField>>> LoadBinaryDataMapAsync(Stream stream, DbcHead head, List<DbcFieldType> fieldTypes, Encoding encoding)
        {
            //var padNode = new DbcField(string.Empty);
            var dataMap = new SortedDictionary<int, List<DbcField>>();
            //Dữ liệu mỗi hàng
            var rowData = new byte[head.FieldCount * 4];
            var offset = 0;
            var readNextInt = () =>
            {
                var s = BitConverter.ToInt32(rowData, offset);
                offset += 4;
                return s;
            };
            var readNextFloat = () =>
            {
                var s = BitConverter.ToSingle(rowData, offset);
                offset += 4;
                return s;
            };
            //Đọc lần lượt từng dòng
            for (var rowIndex = 0; rowIndex < head.RecordCount; rowIndex++)
            {
                //Đọc dữ liệu hàng
                await ReadBytesAsync(stream, rowData);
                var rowID = readNextInt();
                var row = new List<DbcField>(head.FieldCount)
            {
                //Phần tử đầu tiên của mỗi dòng
                new(rowID)
            };
                //Đọc phần tử còn lại
                for (var fieldIndex = 1; fieldIndex < head.FieldCount; fieldIndex++)
                {
                    var fieldType = fieldTypes[fieldIndex];
                    if (fieldType == DbcFieldType.T_FLOAT)
                    {
                        row.Add(new(readNextFloat()));
                    }
                    else
                    {
                        row.Add(new(readNextInt()));
                    }

                }
                //Phần bù đắp
                offset = 0;
                dataMap[rowID] = row;
            }
            await LoadBinaryStringBlockAsync(stream, head.StringBlockSize, dataMap, fieldTypes, encoding);
            return dataMap;
        }

        private static async Task LoadBinaryStringBlockAsync(Stream stream, int blockSize, SortedDictionary<int, List<DbcField>> dataMap, List<DbcFieldType> fieldTypes)
        {
            //Đọc toàn bộ vùng chuỗi
            var stringData = new byte[blockSize];
            {
                var buff = new byte[0x1000];
                var readLength = 0;
                int rCount;
                while (readLength < blockSize)
                {
                    rCount = Math.Min(buff.Length, blockSize - readLength);
                    readLength += await stream.ReadAsync(stringData, readLength, rCount);
                }
            }
            //Công cụ để đọc chuỗi
            var readCString = (int p0) =>
            {
                string? cstr = null;
                int p1 = p0;
                while (p1 < blockSize)
                {
                    if (stringData[p1] == 0)
                    {
                        if (p1 > p0)
                        {
                            try
                            {
                                cstr = _textEncoding.GetString(stringData, p0, p1 - p0);
                            }
                            catch (Exception ex)
                            {
                                throw;
                            }
                        }
                        else
                        {
                            cstr = string.Empty;
                        }
                        break;
                    }
                    p1++;
                }
                return cstr;
            };
            //Đọc từng hàng
            foreach (var row in dataMap.Values)
            {
                for (var fieldIndex = 0; fieldIndex < fieldTypes.Count; fieldIndex++)
                {
                    var fieldType = fieldTypes[fieldIndex];
                    //Chỉ xử lý các trường dữ liệu chuỗi
                    if (fieldType != DbcFieldType.T_STRING)
                    {
                        continue;
                    }
                    var rowField = row[fieldIndex];
                    var pos = rowField.IntValue;
                    var cstr = readCString(pos) ?? string.Empty;
                    row[fieldIndex] = new(cstr);
                }
            }

        }

        private static async Task LoadBinaryStringBlockAsync(Stream stream, int blockSize, SortedDictionary<int, List<DbcField>> dataMap, List<DbcFieldType> fieldTypes, Encoding encoding)
        {
            //Đọc toàn bộ vùng chuỗi
            var stringData = new byte[blockSize];
            {
                var buff = new byte[0x1000];
                var readLength = 0;
                int rCount;
                while (readLength < blockSize)
                {
                    rCount = Math.Min(buff.Length, blockSize - readLength);
                    readLength += await stream.ReadAsync(stringData, readLength, rCount);
                }
            }
            //Công cụ để đọc chuỗi
            var readCString = (int p0) =>
            {
                string? cstr = null;
                int p1 = p0;
                while (p1 < blockSize)
                {
                    if (stringData[p1] == 0)
                    {
                        if (p1 > p0)
                        {
                            try
                            {
                                cstr = encoding.GetString(stringData, p0, p1 - p0);
                            }
                            catch (Exception ex)
                            {
                                throw;
                            }
                        }
                        else
                        {
                            cstr = string.Empty;
                        }
                        break;
                    }
                    p1++;
                }
                return cstr;
            };
            //Đọc từng hàng
            foreach (var row in dataMap.Values)
            {
                for (var fieldIndex = 0; fieldIndex < fieldTypes.Count; fieldIndex++)
                {
                    var fieldType = fieldTypes[fieldIndex];
                    //Chỉ xử lý các trường dữ liệu chuỗi
                    if (fieldType != DbcFieldType.T_STRING)
                    {
                        continue;
                    }
                    var rowField = row[fieldIndex];
                    var pos = rowField.IntValue;
                    var cstr = readCString(pos) ?? string.Empty;
                    row[fieldIndex] = new(cstr);
                }
            }

        }
    }
}