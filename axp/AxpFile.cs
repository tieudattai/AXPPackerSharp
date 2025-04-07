using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Policy;
using System.Threading.Tasks;

namespace axp.Core
{
    public class AxpFile
    {
        public static MathTable _mathTable = new MathTable();

        const uint HASH_TABLE_SIZE = 0x8000;
        const uint AXP_IDENTITY = 0x4B505841;

        public AxpHead FileHead;
        public List<AxpHashNode> HashTable;
        public List<AxpBlockNode> BlockTable;

        private AxpFile(List<AxpHashNode> hashTable, List<AxpBlockNode> blockTable)
        {
            HashTable = hashTable;
            BlockTable = blockTable;
        }

        public static async Task<AxpFile> ReadAsync(FileStream fileStream)
        {
            var head = await LoadFileHeadAsync(fileStream);
            if (head.Identity != AXP_IDENTITY)
            {
                throw new Exception("Không phải tệp tin axp");
            }
            if (head.EditFlag != 0)
            {
                throw new Exception("Tệp tin AXP đang được chỉnh sửa");
            }
            var hashTable = new List<AxpHashNode>();
            var blockTable = new List<AxpBlockNode>();
            await LoadHashTableAsync(fileStream, hashTable);
            await LoadBlockTableAsync(fileStream, blockTable, head.BlockTableCount);
            return new AxpFile(hashTable, blockTable) { FileHead = head };
        }

        private static async Task ReadBytesAsync(FileStream fileStream, byte[] data)
        {
            var offset = 0;
            while (offset < data.Length)
            {
                var count = await fileStream.ReadAsync(data, offset, data.Length - offset);
                offset += count;
            }
        }

        /// <summary>
        /// Đọc 4 byte từ vị trí đã chỉ định của mảng và chuyển đổi thành uint
        /// </summary>
        /// <param name="bytes"></param>
        /// <param name="offset"></param>
        /// <returns></returns>
        private static uint ParseUint(byte[] bytes, int offset)
        {
            return BitConverter.ToUInt32(bytes, offset);
        }

        private static async Task<AxpHead> LoadFileHeadAsync(FileStream fileStream)
        {
            var headData = new byte[10 * 4];
            await ReadBytesAsync(fileStream, headData);
            //
            var offset = 0;
            var readNextUint = () =>
            {
                var s = ParseUint(headData, offset);
                offset += 4;
                return s;
            };
            AxpHead head;
            head.Identity = readNextUint();
            head.Version = readNextUint();
            head.EditFlag = readNextUint();
            head.HashTableOffset = readNextUint();
            head.BlockTableOffset = readNextUint();
            head.BlockTableCount = readNextUint();
            head.BlockTableMaxSize = readNextUint();
            head.DataOffset = readNextUint();
            head.DataSize = readNextUint();
            head.DataHoleSize = readNextUint();
            return head;
        }

        /// <summary>
        /// Tải mảng băm
        /// </summary>
        /// <param name="fileStream"></param>
        /// <param name="hashTable"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        private static async Task LoadHashTableAsync(FileStream fileStream, List<AxpHashNode> hashTable)
        {
            var nodeData = new byte[3 * 4];
            //
            int offset = 0;
            var readNextUint = () =>
            {
                var s = ParseUint(nodeData, offset);
                offset += 4;
                return s;
            };
            for (var i = 0; i < HASH_TABLE_SIZE; i++)
            {
                await ReadBytesAsync(fileStream, nodeData);
                AxpHashNode hashNode;   
                hashNode.HashA = readNextUint();
                hashNode.HashB = readNextUint();
                hashNode.Data = readNextUint();
                hashTable.Add(hashNode);
                offset = 0;
            }
        }

        private static async Task LoadBlockTableAsync(FileStream fileStream, List<AxpBlockNode> blockTable, uint count)
        {
            var nodeData = new byte[3 * 4];
            //
            int offset = 0;
            var readNextUint = () =>
            {
                var s = ParseUint(nodeData, offset);
                offset += 4;
                return s;
            };
            for (var i = 0; i < count; i++)
            {
                await ReadBytesAsync(fileStream, nodeData);
                AxpBlockNode blockNode;
                blockNode.DataOffset = readNextUint();
                blockNode.BlockSize = readNextUint();
                blockNode.Flags = readNextUint();
                blockTable.Add(blockNode);
                offset = 0;
            }
        }

        public AxpHashNode? GetHashNode(string filename)
        {
            filename = filename.ToLower();
            var hash = _mathTable.Hash(HashType.TypeOffset, filename);
            var hashA = _mathTable.Hash(HashType.TypeA, filename);
            var hashB = _mathTable.Hash(HashType.TypeB, filename);
            var hashStart = (int)(hash % HASH_TABLE_SIZE);
            var hashPos = hashStart;
            while (true)
            {
                var hashNode = HashTable[hashPos];
                if (hashNode.Exists()
                    && hashNode.HashA == hashA
                    && hashNode.HashB == hashB)
                {
                    return hashNode;
                }
                //Vị trí tiếp theo
                hashPos = (hashPos + 1) % ((int)HASH_TABLE_SIZE);
                if (hashPos == hashStart)
                {
                    break;
                }
            }
            return null;
        }

        public AxpBlockNode? GetBlockNode(string filename)
        {
            var hashNode = GetHashNode(filename);
            if (!hashNode.HasValue)
            {
                return null;
            }
            var blockIndex = hashNode.Value.BlockIndex();
            return BlockTable[(int)blockIndex];
        }
    }
}