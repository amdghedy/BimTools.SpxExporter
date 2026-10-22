using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using BimTools.SpxExporter.Models;

namespace BimTools.SpxExporter.Services
{
    public sealed class PostProcessService
    {
        public string BuildOutputPath(ExportSettings settings, string inputFile)
        {
            string extension = FileNamingService.NormalizeExtension(settings.PostProcessOutputExtension, ".spx");
            string directory = Path.GetDirectoryName(inputFile) ?? string.Empty;
            return Path.Combine(directory, Path.GetFileNameWithoutExtension(inputFile) + extension);
        }

        public Process Start(ExportSettings settings, string inputFile, string outputFile, StringBuilder outputSink)
        {
            string arguments = (settings.PostProcessArguments ?? string.Empty)
                .Replace("{input}", inputFile)
                .Replace("{output}", outputFile)
                .Replace("{outputDir}", Path.GetDirectoryName(outputFile) ?? string.Empty)
                .Replace("{name}", Path.GetFileNameWithoutExtension(inputFile));

            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = settings.PostProcessExecutable,
                Arguments = arguments,
                WorkingDirectory = Path.GetDirectoryName(settings.PostProcessExecutable) ?? string.Empty,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            Process process = new Process { StartInfo = startInfo };
            DataReceivedEventHandler handler = delegate (object sender, DataReceivedEventArgs args)
            {
                if (!string.IsNullOrEmpty(args.Data))
                {
                    lock (outputSink)
                    {
                        outputSink.AppendLine(args.Data);
                    }
                }
            };

            process.OutputDataReceived += handler;
            process.ErrorDataReceived += handler;
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            return process;
        }

        public static string Tail(StringBuilder sink, int maxLength)
        {
            string text;
            lock (sink)
            {
                text = sink.ToString().Trim();
            }

            return text.Length <= maxLength ? text : "..." + text.Substring(text.Length - maxLength);
        }
    }
}
