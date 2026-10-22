using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using BimTools.SpxExporter.Models;

namespace BimTools.SpxExporter.Services
{
    public sealed class JournalCommandScanner
    {
        private static readonly Regex CommandPattern = new Regex(
            "Execute external command:(?<id>[^:\"]+):(?<cls>[^\"]*)\"",
            RegexOptions.Compiled);

        private static readonly Regex SynchroPattern = new Regex(
            "synchro|spx|exportsp|4d",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public IList<JournalCommand> Scan(string journalFolder, int maxFiles)
        {
            Dictionary<string, JournalCommand> found = new Dictionary<string, JournalCommand>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrEmpty(journalFolder) || !Directory.Exists(journalFolder))
            {
                return new List<JournalCommand>();
            }

            IEnumerable<FileInfo> files = new DirectoryInfo(journalFolder)
                .EnumerateFiles("journal.*.txt")
                .OrderByDescending(f => f.LastWriteTimeUtc)
                .Take(maxFiles);

            foreach (FileInfo file in files)
            {
                ScanFile(file.FullName, found);
            }

            return found.Values
                .OrderByDescending(c => c.LooksLikeSynchro)
                .ThenBy(c => c.CommandId, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static void ScanFile(string path, IDictionary<string, JournalCommand> found)
        {
            try
            {
                using (FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    using (StreamReader reader = new StreamReader(stream))
                    {
                        string line;
                        while ((line = reader.ReadLine()) != null)
                        {
                            if (line.IndexOf("Execute external command", StringComparison.Ordinal) < 0)
                            {
                                continue;
                            }

                            Match match = CommandPattern.Match(line);
                            if (!match.Success)
                            {
                                continue;
                            }

                            string id = match.Groups["id"].Value;
                            string className = match.Groups["cls"].Value;
                            if (!found.ContainsKey(id))
                            {
                                found[id] = new JournalCommand
                                {
                                    CommandId = id,
                                    ClassName = className,
                                    LooksLikeSynchro = SynchroPattern.IsMatch(id) || SynchroPattern.IsMatch(className)
                                };
                            }
                        }
                    }
                }
            }
            catch (IOException)
            {
                return;
            }
        }
    }
}
