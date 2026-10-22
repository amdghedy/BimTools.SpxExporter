using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Autodesk.Revit.UI;
using BimTools.SpxExporter.Infrastructure;
using BimTools.SpxExporter.Models;

namespace BimTools.SpxExporter.Services
{
    public sealed class SynchroCommandLocator
    {
        private static readonly Regex SynchroPattern = new Regex(
            "synchro|spx|exportsp|4d",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex RegistrationPattern = new Regex(
            @"Added pushbutton Id:\s*\d+,\s*name:\s*(?<name>[^,]+),.*?class:\s*(?<cls>[^,]+),\s*assembly:\s*(?<asm>.+?),\s*parentId:\s*(?<parent>[^\s}]+)",
            RegexOptions.Singleline | RegexOptions.Compiled);

        private const string KnownSynchroCommandId = "CustomCtrl_%CustomCtrl_%Add-Ins%Synchro%ExportToSynchroButton";

        private readonly JournalCommandScanner _journalScanner = new JournalCommandScanner();

        public string FindCommandId(string journalFolder, out string explanation)
        {
            List<string> notes = new List<string>();

            if (RevitCommandId.LookupCommandId(KnownSynchroCommandId) != null)
            {
                explanation = "Found the SYNCHRO 'Export to Synchro' button.";
                return KnownSynchroCommandId;
            }

            string registered = FindFromRegistrations(journalFolder);
            if (registered != null)
            {
                explanation = "Found the SYNCHRO export button from the Revit startup journal.";
                return registered;
            }

            foreach (string manifest in FindManifests())
            {
                string name;
                string id = TryManifest(manifest, out name);
                if (id != null)
                {
                    explanation = "Found the exporter through " + Path.GetFileName(manifest) + " (" + name + ").";
                    return id;
                }

                notes.Add(Path.GetFileName(manifest));
            }

            foreach (JournalCommand command in _journalScanner.Scan(journalFolder, 30))
            {
                if (command.LooksLikeSynchro && RevitCommandId.LookupCommandId(command.CommandId) != null)
                {
                    explanation = "Found the exporter in your Revit journals (" + command.CommandId + ").";
                    return command.CommandId;
                }
            }

            explanation = notes.Count == 0
                ? "No SYNCHRO exporter add-in manifest was found for Revit 2024."
                : "Found " + string.Join(", ", notes) + " but its ribbon command id could not be resolved automatically. "
                  + "Click the SYNCHRO export button once in Revit, then press 'Scan journals'.";
            return null;
        }

        private static string FindFromRegistrations(string journalFolder)
        {
            if (string.IsNullOrEmpty(journalFolder) || !Directory.Exists(journalFolder))
            {
                return null;
            }

            IEnumerable<FileInfo> files = new DirectoryInfo(journalFolder)
                .EnumerateFiles("journal.*.txt")
                .OrderByDescending(f => f.LastWriteTimeUtc)
                .Take(12);

            foreach (FileInfo file in files)
            {
                string text;
                try
                {
                    using (FileStream stream = new FileStream(file.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    using (StreamReader reader = new StreamReader(stream))
                    {
                        text = reader.ReadToEnd();
                    }
                }
                catch (IOException)
                {
                    continue;
                }

                foreach (Match match in RegistrationPattern.Matches(text))
                {
                    string name = match.Groups["name"].Value.Trim();
                    string source = match.Groups["cls"].Value + " " + match.Groups["asm"].Value;
                    if (!SynchroPattern.IsMatch(source) || name.IndexOf("Settings", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        continue;
                    }

                    string candidate = "CustomCtrl_%" + match.Groups["parent"].Value.Trim() + "%" + name;
                    if (RevitCommandId.LookupCommandId(candidate) != null)
                    {
                        return candidate;
                    }
                }
            }

            return null;
        }

        private static IEnumerable<string> FindManifests()
        {
            string[] roots =
            {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Autodesk", "Revit", "Addins", AppPaths.RevitYear.ToString()),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Autodesk", "Revit", "Addins", AppPaths.RevitYear.ToString())
            };

            foreach (string root in roots)
            {
                if (!Directory.Exists(root))
                {
                    continue;
                }

                foreach (string file in Directory.EnumerateFiles(root, "*.addin"))
                {
                    if (SynchroPattern.IsMatch(Path.GetFileName(file)))
                    {
                        yield return file;
                    }
                }
            }
        }

        private static string TryManifest(string manifestPath, out string displayName)
        {
            displayName = string.Empty;
            XDocument document;
            try
            {
                document = XDocument.Load(manifestPath);
            }
            catch (Exception ex)
            {
                if (ex is IOException || ex is System.Xml.XmlException || ex is UnauthorizedAccessException)
                {
                    return null;
                }

                throw;
            }

            foreach (XElement addIn in document.Descendants("AddIn"))
            {
                XAttribute type = addIn.Attribute("Type");
                if (type == null || !string.Equals(type.Value, "Command", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                string text = (string)addIn.Element("Text") ?? (string)addIn.Element("Name");
                if (string.IsNullOrWhiteSpace(text))
                {
                    continue;
                }

                string[] candidates =
                {
                    "CustomCtrl_%CustomCtrl_%Add-Ins%External Tools%" + text,
                    "CustomCtrl_%CustomCtrl_%CustomCtrl_%Add-Ins%External Tools%" + text,
                    "CustomCtrl_%Add-Ins%External Tools%" + text
                };

                foreach (string candidate in candidates)
                {
                    if (RevitCommandId.LookupCommandId(candidate) != null)
                    {
                        displayName = text;
                        return candidate;
                    }
                }
            }

            return null;
        }
    }
}
