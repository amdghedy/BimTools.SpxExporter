using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Json;
using BimTools.SpxExporter.Infrastructure;
using BimTools.SpxExporter.Models;

namespace BimTools.SpxExporter.Services
{
    public sealed class SettingsService
    {
        private readonly DataContractJsonSerializer _serializer = new DataContractJsonSerializer(typeof(ExportSettings));

        public ExportSettings Load()
        {
            string path = AppPaths.SettingsFile;
            if (!File.Exists(path))
            {
                return new ExportSettings();
            }

            try
            {
                return Import(path);
            }
            catch (IOException)
            {
                return new ExportSettings();
            }
            catch (System.Runtime.Serialization.SerializationException)
            {
                return new ExportSettings();
            }
        }

        public IList<string> ListPresets()
        {
            List<string> names = new List<string>();
            foreach (string file in Directory.EnumerateFiles(AppPaths.PresetsFolder, "*.json"))
            {
                names.Add(Path.GetFileNameWithoutExtension(file));
            }

            names.Sort(StringComparer.OrdinalIgnoreCase);
            return names;
        }

        public string PresetPath(string name)
        {
            return Path.Combine(AppPaths.PresetsFolder, name + ".json");
        }

        public void Save(ExportSettings settings)
        {
            Export(settings, AppPaths.SettingsFile);
        }

        public ExportSettings Import(string path)
        {
            using (FileStream stream = File.OpenRead(path))
            {
                ExportSettings settings = _serializer.ReadObject(stream) as ExportSettings;
                return settings ?? new ExportSettings();
            }
        }

        public void Export(ExportSettings settings, string path)
        {
            string directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            using (FileStream stream = File.Create(path))
            {
                using (System.Xml.XmlDictionaryWriter writer = JsonReaderWriterFactory.CreateJsonWriter(stream, System.Text.Encoding.UTF8, false, true))
                {
                    _serializer.WriteObject(writer, settings);
                    writer.Flush();
                }
            }
        }
    }
}
