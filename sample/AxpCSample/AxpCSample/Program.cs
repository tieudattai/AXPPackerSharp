using axp.Core;
using axp.Dbc;
using System.IO.Compression;

string dir = @"/app/dir";
string outputFile = @"sampler.axp";

AxpFileC c = new AxpFileC();
if (!Directory.Exists(dir) || c.GetToTalFiles(dir) == 0)
{
    Console.WriteLine(" ***** Directory path does not exist or empty ***** ");
    return;
}

try
{
    c.Create(dir, outputFile);

    // Extract AXP
    // Uncomment to use

    /* 
     
    string writeDir = @"/path/to/save";
    using (var fs = File.OpenRead(@"file.axp"))
    {
        var axpStream = await AxpFile.ReadAsync(fs);
        var stringTableNode = axpStream.GetBlockNode("(list)");
        if (stringTableNode.HasValue)
        {
            fs.Seek(stringTableNode.Value.DataOffset, SeekOrigin.Begin);
            string table = await DbcFile.ReadTableAsync(fs, stringTableNode.Value.BlockSize);
            var splits = table.Split('\n');
            int numberFiles = 0;
            var totalFiles = int.TryParse(splits[1], out numberFiles);
            if (numberFiles < 0 && splits.Length == 1)
            {
                Console.WriteLine("***** Empty AXP *****");
            }
            else
            {
                foreach (var fTable in splits.Skip(2))
                {
                    var stringFileInfo = fTable.Split('|');
                    if (stringFileInfo.Length == 3)
                    {
                        string fPath = stringFileInfo[0];
                        var blockNode = axpStream.GetBlockNode(fPath);
                        if (blockNode == null)
                        {
                            Console.WriteLine("***** Bad Block: {0} *****", fPath);
                            continue;
                        }
                        fs.Seek(blockNode.Value.DataOffset + 4, SeekOrigin.Begin);
                        var blockBytes = await DbcFile.ReadByteAsync(fs, blockNode.Value.BlockSize);

                        string fullFile = Path.Combine(writeDir, fPath);
                        string folder = Path.GetDirectoryName(fullFile);
                        if (!Directory.Exists(folder))
                            Directory.CreateDirectory(folder);

                        File.Create(fullFile).Close();
                        await File.WriteAllBytesAsync(fullFile, blockBytes);
                    }
                }
            }
        }
       
    }
    */
}
catch (Exception ex)
{
    Console.WriteLine(" ***** Throw Exception: {0} ***** ", ex.Message);
}

Console.ReadLine();