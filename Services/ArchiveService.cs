using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;

namespace BimTools.SpxExporter.Services
{
    public sealed class ArchiveService
    {
        public int CreateZip(string zipPath, IEnumerable<string> files)
        {
            string directory = Path.GetDirectoryName(zipPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            if (File.Exists(zipPath))
            {
                File.Delete(zipPath);
            }

            HashSet<string> usedNames = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
            int count = 0;
            using (ZipArchive archive = ZipFile.Open(zipPath, ZipArchiveMode.Create))
            {
                foreach (string file in files)
                {
                    if (!File.Exists(file))
                    {
                        continue;
                    }

                    string entryName = Path.GetFileName(file);
                    int suffix = 1;
                    while (!usedNames.Add(entryName))
                    {
                        entryName = Path.GetFileNameWithoutExtension(file) + "_" + suffix.ToString(CultureInfo.InvariantCulture) + Path.GetExtension(file);
                        suffix++;
                    }

                    archive.CreateEntryFromFile(file, entryName, CompressionLevel.Optimal);
                    count++;
                }
            }

            return count;
        }
    }
}
