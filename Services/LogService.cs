using System;
using System.Globalization;
using System.IO;
using System.Text;
using BimTools.SpxExporter.Infrastructure;

namespace BimTools.SpxExporter.Services
{
    public sealed class LogService
    {
        private readonly object _sync = new object();

        public LogService(string filePath)
        {
            FilePath = filePath;
            string directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }

        public event Action<string> LineWritten;

        public string FilePath { get; }

        public static LogService CreateNew()
        {
            string name = "SpxExport_" + DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture) + ".log";
            return new LogService(Path.Combine(AppPaths.LogFolder, name));
        }

        public void Info(string message)
        {
            Write("INFO ", message);
        }

        public void Warn(string message)
        {
            Write("WARN ", message);
        }

        public void Error(string message)
        {
            Write("ERROR", message);
        }

        private void Write(string level, string message)
        {
            string line = string.Format(
                CultureInfo.InvariantCulture,
                "{0:yyyy-MM-dd HH:mm:ss} [{1}] {2}",
                DateTime.Now,
                level,
                message);

            lock (_sync)
            {
                try
                {
                    File.AppendAllText(FilePath, line + Environment.NewLine, Encoding.UTF8);
                }
                catch (IOException)
                {
                    return;
                }
            }

            Action<string> handler = LineWritten;
            if (handler != null)
            {
                handler(line);
            }
        }
    }
}
