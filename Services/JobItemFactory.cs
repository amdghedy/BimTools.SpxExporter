using System;
using System.Globalization;
using System.IO;
using Autodesk.Revit.DB;
using BimTools.SpxExporter.Models;

namespace BimTools.SpxExporter.Services
{
    public sealed class JobItemFactory
    {
        public ExportJobItem Create(string path, string rootFolder)
        {
            ExportJobItem item = new ExportJobItem
            {
                FilePath = path,
                FileName = Path.GetFileName(path),
                ModelName = Path.GetFileNameWithoutExtension(path),
                RelativeDirectory = ComputeRelativeDirectory(path, rootFolder)
            };

            try
            {
                FileInfo info = new FileInfo(path);
                item.SizeText = (info.Length / 1048576.0).ToString("0.0", CultureInfo.InvariantCulture) + " MB";
            }
            catch (IOException)
            {
                item.SizeText = "?";
            }

            try
            {
                BasicFileInfo basicInfo = BasicFileInfo.Extract(path);
                item.IsWorkshared = basicInfo.IsWorkshared;
                item.RevitVersion = basicInfo.Format;
            }
            catch (Exception ex)
            {
                item.RevitVersion = "?";
                item.Message = "Could not read file info: " + ex.Message;
            }

            return item;
        }

        private static string ComputeRelativeDirectory(string path, string rootFolder)
        {
            if (string.IsNullOrEmpty(rootFolder))
            {
                return string.Empty;
            }

            string directory = Path.GetDirectoryName(path) ?? string.Empty;
            string root = rootFolder.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            if (!directory.StartsWith(root, StringComparison.OrdinalIgnoreCase))
            {
                return string.Empty;
            }

            return directory.Substring(root.Length).Trim(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }
    }
}
