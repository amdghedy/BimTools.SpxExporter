using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using BimTools.SpxExporter.Infrastructure;

namespace BimTools.SpxExporter.Services
{
    public sealed class ModelFileScanner
    {
        private static readonly Regex BackupPattern = new Regex(@"\.\d{4}\.rvt$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public IList<string> FindModels(string folder, bool includeSubfolders, string excludePatterns)
        {
            List<string> results = new List<string>();
            string[] excludes = PatternMatcher.Split(excludePatterns);
            SearchOption option = includeSubfolders ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            foreach (string file in Directory.EnumerateFiles(folder, "*.rvt", option))
            {
                if (IsBackup(file) || PatternMatcher.MatchesAny(Path.GetFileName(file), excludes))
                {
                    continue;
                }

                results.Add(file);
            }

            results.Sort(StringComparer.OrdinalIgnoreCase);
            return results;
        }

        public static bool IsBackup(string path)
        {
            return BackupPattern.IsMatch(path);
        }
    }
}
