using System;
using System.Globalization;
using System.IO;
using System.Text;
using BimTools.SpxExporter.Infrastructure;
using BimTools.SpxExporter.Models;

namespace BimTools.SpxExporter.Services
{
    public sealed class FileNamingService
    {
        public string BuildName(string template, ExportJobItem item, ExportSettings settings, int index)
        {
            string pattern = string.IsNullOrWhiteSpace(template) ? "{ModelName}" : template;
            DateTime now = DateTime.Now;

            StringBuilder builder = new StringBuilder(pattern);
            builder.Replace("{ModelName}", item.ModelName);
            builder.Replace("{Date}", now.ToString("yyyyMMdd", CultureInfo.InvariantCulture));
            builder.Replace("{Time}", now.ToString("HHmmss", CultureInfo.InvariantCulture));
            builder.Replace("{DateTime}", now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture));
            builder.Replace("{RevitVersion}", AppPaths.RevitYear.ToString(CultureInfo.InvariantCulture));
            builder.Replace("{SourceVersion}", string.IsNullOrEmpty(item.RevitVersion) ? "NA" : item.RevitVersion);
            builder.Replace("{ViewName}", settings.ViewName ?? string.Empty);
            builder.Replace("{User}", Environment.UserName);
            builder.Replace("{Index}", index.ToString("000", CultureInfo.InvariantCulture));

            string name = Sanitize(builder.ToString());
            return string.IsNullOrWhiteSpace(name) ? Sanitize(item.ModelName) : name;
        }

        public string ResolveOutputDirectory(ExportSettings settings, ExportJobItem item)
        {
            string directory;
            if (settings.OutputToSourceFolder || string.IsNullOrWhiteSpace(settings.OutputFolder))
            {
                directory = Path.GetDirectoryName(item.FilePath);
            }
            else
            {
                directory = settings.OutputFolder;
                if (settings.PreserveFolderStructure && !string.IsNullOrEmpty(item.RelativeDirectory))
                {
                    directory = Path.Combine(directory, item.RelativeDirectory);
                }
            }

            if (settings.CreateDateSubfolder)
            {
                directory = Path.Combine(directory, DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
            }

            return directory;
        }

        public string ApplyOverwritePolicy(string directory, string baseName, string extension, OverwritePolicy policy, out bool skip)
        {
            skip = false;
            string target = Path.Combine(directory, baseName + extension);
            if (!File.Exists(target) || policy == OverwritePolicy.Overwrite)
            {
                return baseName;
            }

            if (policy == OverwritePolicy.Skip)
            {
                skip = true;
                return baseName;
            }

            return baseName + "_" + DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);
        }

        public static string NormalizeExtension(string extension, string fallback)
        {
            if (string.IsNullOrWhiteSpace(extension))
            {
                return fallback;
            }

            string trimmed = extension.Trim();
            return trimmed.StartsWith(".", StringComparison.Ordinal) ? trimmed : "." + trimmed;
        }

        private static string Sanitize(string value)
        {
            char[] invalid = Path.GetInvalidFileNameChars();
            StringBuilder builder = new StringBuilder(value.Length);
            foreach (char c in value)
            {
                builder.Append(Array.IndexOf(invalid, c) >= 0 ? '_' : c);
            }

            return builder.ToString().Trim().TrimEnd('.');
        }
    }
}
