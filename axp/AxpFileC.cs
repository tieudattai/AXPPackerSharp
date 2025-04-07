using System;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace axp.Core
{
    public unsafe class AxpFileC
    {
        const uint HASH_TABLE_SIZE = 0x8000;
        const uint AXP_IDENTITY = 0x4B505841;
        const uint BLOCK_MAXSIZE = 0x100000;
        const uint BLOCK_TABLEOFFSET = 0x60028;
        const uint StartFileData = 0x160028;
        const uint HASH_TBLOFFSET = 0x28;
        const uint VERSION = 0x00010001;
        const uint BLOCK_SIZE = 0xC;
        public AxpHead FileHead;
        Regex upperAlphabet = new Regex("[A-Z]");
        public AxpFileC()
        {
        }
        public bool WriteHead(MemoryMappedViewAccessor fileStream, AxpHead head)
        {
            byte[] bytePEHeader = Serialize<AxpHead>(head);
            fileStream.WriteArray(0, bytePEHeader, 0, bytePEHeader.Length);
            return true;
        }

        public bool Create(string dir, string fName, bool keepRoot = default)
        {
            uint fOffset = StartFileData;
            DirectoryInfo dirInfo = new DirectoryInfo(dir);

            long size = DirSize(dirInfo);
            if (size < (1.37 * 1024 * 1024))
                size = (long)(1.37 * 1024 * 1024);
            var hashNode = CreateAxpHashNode(dir, keepRoot);

            MemoryMappedFile mp = MemoryMappedFile.CreateNew("swap", (size * 3) + 0x160028, MemoryMappedFileAccess.ReadWrite, MemoryMappedFileOptions.None, HandleInheritability.None);

            using (var view = mp.CreateViewAccessor())
            {
                byte* poke = null;
                view.SafeMemoryMappedViewHandle.AcquirePointer(ref poke);


                int* intPointer = (int*)poke;
                int myCount = 0;
                for (var i = 0; i < HASH_TABLE_SIZE; i++)
                {
                    MyNode no = hashNode[i];

                    AxpHashNode hn;
                    hn.HashA = no.HashA;
                    hn.HashB = no.HashB;
                    hn.Data = no.Offset;

                    byte[] byteOfNode = Serialize<AxpHashNode>(hn);
                    var offsetHashNode = HASH_TBLOFFSET + (i * BLOCK_SIZE);
                    view.WriteArray(offsetHashNode, byteOfNode, 0, byteOfNode.Length);
                }

                int maxBlockSize = hashNode.Max(x => x.BlockIndex) + 1;
                var blockNode = new List<MyBlockNode>(maxBlockSize);
                blockNode.AddRange(Enumerable.Repeat(new MyBlockNode { }, maxBlockSize));
                foreach (var nod in hashNode)
                {
                    if (nod.Binaries == null)
                        continue;

                    MyBlockNode bn = new MyBlockNode { };
                    bn.DataOffset = fOffset;
                    bn.BlockSize = (uint)nod.Binaries.Length;
                    bn.Flags = unchecked((uint)int.MinValue);

                    bn.Binaries = nod.Binaries;

                    blockNode[nod.BlockIndex] = bn;
                }

                for (int i = 0; i < blockNode.Count; i++)
                {
                    var no = blockNode[i];
                    if (no.Binaries == null)
                        no.Binaries = new byte[0];
                    AxpBlockNode nod = new AxpBlockNode
                    {
                        BlockSize = no.BlockSize,
                        DataOffset = fOffset,
                        Flags = no.Flags,
                    };

                    //393256
                    var offsetBlockTable = (HASH_TBLOFFSET) + HASH_TABLE_SIZE * BLOCK_SIZE + (myCount * BLOCK_SIZE);
                    byte[] byteOfBlock = Serialize<AxpBlockNode>(nod);
                    view.WriteArray(offsetBlockTable, byteOfBlock, 0, byteOfBlock.Length);

                    view.WriteArray(fOffset, no.Binaries, 0, no.Binaries.Length);

                    fOffset += (uint)no.Binaries.Length;
                    fOffset += 100; // cách nhau 100byte
                    myCount++;
                }

                WriteHead(view, new AxpHead
                {
                    Identity = AXP_IDENTITY,
                    BlockTableCount = (uint)GetToTalFiles(dir) + 2,
                    BlockTableMaxSize = BLOCK_MAXSIZE,
                    BlockTableOffset = BLOCK_TABLEOFFSET,
                    DataHoleSize = 0xA,
                    DataOffset = StartFileData,
                    DataSize = 0x0,
                    EditFlag = 0x0,
                    HashTableOffset = HASH_TBLOFFSET,
                    Version = VERSION,
                });

                view.SafeMemoryMappedViewHandle.ReleasePointer();
            }
            using (var stream = mp.CreateViewStream())
            {
                var buffers = new byte[fOffset];
                stream.Read(buffers, 0, buffers.Length);
                using (var nfs = File.Create(fName))
                    nfs.Write(buffers, 0, buffers.Length);
            }

            return true;
        }

        public List<MyNode> CreateAxpHashNode(string dir, bool keepRoot = default)
        {
            int min = int.MinValue;
            int offset = min;
            var nodes = new List<MyNode>((int)HASH_TABLE_SIZE);
            nodes.AddRange(Enumerable.Repeat(new MyNode { }, (int)HASH_TABLE_SIZE));
            var fileInDir = Directory.GetFiles(dir, "*.*", SearchOption.AllDirectories);

            StringBuilder tableBuilder = new StringBuilder();
            tableBuilder.AppendLine($"FFFFFFFF");
            tableBuilder.AppendLine($"{fileInDir.Length}");

            Dictionary<uint, uint> tabl = new Dictionary<uint, uint>
            {
                { 29955, 0 }
            };
            for (int i = 0; i < fileInDir.Length; i++)
            {
                offset = min + i + 1; //biến i tăng sau 
                string formatName = GetFileName(fileInDir[i], dir, keepRoot);
                string nameTransform = NameTransform(formatName);
                FileInfo fio = new FileInfo(fileInDir[i]);
                var fBin = File.ReadAllBytes(fio.FullName);
                string hexFSize = fio.Length.ToString("X8");
                tableBuilder.AppendLine($"{nameTransform}|{hexFSize}|FFFFFFFF");

                nameTransform = upperAlphabet.Replace(nameTransform, x => x.Value.ToLower());

                var db = Encoding.GetEncoding("GB18030").GetString(Encoding.GetEncoding(437).GetBytes(nameTransform));

                var hash = AxpFile._mathTable.Hash(HashType.TypeOffset, db);
                var hashA = AxpFile._mathTable.Hash(HashType.TypeA, db);
                var hashB = AxpFile._mathTable.Hash(HashType.TypeB, db);

                var hashStart = hash % HASH_TABLE_SIZE;
                var hashPos = hashStart;
                while (true)
                {
                    if (!tabl.ContainsKey(hashPos))
                        break;
                    hashPos = (hashPos + 1) % ((int)HASH_TABLE_SIZE);
                    if (hashPos == hashStart)
                        break;
                }

                tabl.Add(hashPos, hashA);
                nodes[(int)hashPos] = new MyNode
                {
                    Data = hashPos,
                    HashA = hashA,
                    HashB = hashB,
                    Offset = (uint)offset,
                    Binaries = fBin,
                    BlockIndex = offset & 0x3FFF_FFFF
                };
            }

            #region Table
            string _tableCode = "(list)";
            var hashTB = AxpFile._mathTable.Hash(HashType.TypeOffset, _tableCode);
            var hashTBA = AxpFile._mathTable.Hash(HashType.TypeA, _tableCode);
            var hashTBB = AxpFile._mathTable.Hash(HashType.TypeB, _tableCode);

            var hashTBStart = hashTB % HASH_TABLE_SIZE;
            var hashTBPos = hashTBStart;

            var tableNode = new MyNode
            {
                Data = hashTBPos,
                HashA = hashTBA,
                HashB = hashTBB,
                Offset = unchecked((uint)(int.MinValue + fileInDir.Length + 1)),
                Binaries = Encoding.GetEncoding(437).GetBytes(tableBuilder.ToString()),
                BlockIndex = (int.MinValue + fileInDir.Length + 1) & 0x3FFF_FFFF
            };
            //Encoding.GetEncoding("GB18030").GetString(tableNode.Binaries)
            nodes[(int)hashTBPos] = tableNode;

            #endregion

            return nodes;//, tableBuilder.ToString());
        }

        public static byte[] Serialize<T>(T msg) where T : struct
        {
            int objsize = Marshal.SizeOf(typeof(T));
            byte[] ret = new byte[objsize];

            IntPtr buff = Marshal.AllocHGlobal(objsize);
            Marshal.StructureToPtr(msg, buff, true);
            Marshal.Copy(buff, ret, 0, objsize);
            Marshal.FreeHGlobal(buff);
            return ret;
        }

        public int GetToTalFiles(string dir)
        {
            return Directory.GetFiles(dir, "*.*", SearchOption.AllDirectories).Length;
        }

        string GetFileName(string path, string dir, bool keepRoot = default)
        {
            string rootPath = string.Empty;
            //giữ folder bên ngoài
            if (keepRoot)
                rootPath = new DirectoryInfo(dir).Name + "/";
            path = path.Replace($"{dir}\\", string.Empty).Replace("\\", "/");
            return $"{rootPath}{path}";
        }

        string NameTransform(string name)
        {
            Encoding dstEnc = Encoding.GetEncoding(437);
            Encoding enc = Encoding.GetEncoding(1252);
            byte[] defaultBytes = enc.GetBytes(name);

            return dstEnc.GetString(defaultBytes);
        }

        uint GetNextHash(Dictionary<uint, uint> tables, uint hash)
        {
            if (tables.ContainsKey(hash))
            {
                hash++;
                return GetNextHash(tables, hash);
            }
            return hash;
        }

        public long DirSize(DirectoryInfo d)
        {
            long size = 0;
            // Add file sizes.
            FileInfo[] fis = d.GetFiles();
            foreach (FileInfo fi in fis)
            {
                size += fi.Length;
            }
            // Add subdirectory sizes.
            DirectoryInfo[] dis = d.GetDirectories();
            foreach (DirectoryInfo di in dis)
            {
                size += DirSize(di);
            }
            return size;
        }
    }
}
