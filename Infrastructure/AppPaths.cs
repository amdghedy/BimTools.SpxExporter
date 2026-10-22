using System;
using System.IO;

namespace BimTools.SpxExporter.Infrastructure
{
    internal static class AppPaths
    {
        public const string TabName = "BIM TOOLS";
        public const string PanelName = "SPX Export";
        public const string DialogTitle = "BIM TOOLS - SPX Exporter";
        public const int RevitYear = 2024;

        public static string DataFolder
        {
            get
            {
                string path = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "BimTools",
                    "SpxExporter");
                Directory.CreateDirectory(path);
                return path;
            }
        }

        public static string LogFolder
        {
            get
            {
                string path = Path.Combine(DataFolder, "Logs");
                Directory.CreateDirectory(path);
                return path;
            }
        }

        public static string PresetsFolder
        {
            get
            {
                string path = Path.Combine(DataFolder, "Presets");
                Directory.CreateDirectory(path);
                return path;
            }
        }

        public static string SettingsFile
        {
            get { return Path.Combine(DataFolder, "settings.json"); }
        }
    }
}
