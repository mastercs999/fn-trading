using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using tar_cs;

namespace DataCenter.Helpers
{
    internal class Compression
    {
        public delegate void OnFile(string file, string content);

        public static void DecompressTgzStream(string inputFile, OnFile onFile)
        {
            using (FileStream inputStream = File.OpenRead(inputFile))
            using (GZipStream tarStream = new GZipStream(inputStream, CompressionMode.Decompress))
            {
                var tarReader = new TarReader(tarStream);
                while (tarReader.MoveNext(true))
                {
                    // Just files
                    if (tarReader.FileInfo.EntryType != EntryType.File && tarReader.FileInfo.EntryType != EntryType.FileObsolete)
                        continue;

                    // Save to string and process it
                    using (var memoryStream = new MemoryStream())
                    {
                        using (var writer = new StreamWriter(memoryStream))
                        {
                            tarReader.Read(writer.BaseStream);
                        }

                        onFile(Path.GetFileName(tarReader.FileInfo.FileName), Encoding.ASCII.GetString(memoryStream.ToArray()));
                    }
                }
            }
        }

        public static void DecompressTgz(string inputFile)
        {
            using (FileStream inputStream = File.OpenRead(inputFile))
                using (GZipStream tarStream = new GZipStream(inputStream, CompressionMode.Decompress))
                {
                    var tarReader = new TarReader(tarStream);
                    while (tarReader.MoveNext(true))
                    {
                        // Just files
                        if (tarReader.FileInfo.EntryType != EntryType.File && tarReader.FileInfo.EntryType != EntryType.FileObsolete)
                            continue;

                        // Create directory for file
                        Directory.CreateDirectory(Path.GetDirectoryName(tarReader.FileInfo.FileName));

                        // Save file
                        using (FileStream outputStram = File.OpenWrite(tarReader.FileInfo.FileName))
                            tarReader.Read(outputStram);
                    }
                }
        }
    }
}
