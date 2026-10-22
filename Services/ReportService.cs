using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using BimTools.SpxExporter.Infrastructure;
using BimTools.SpxExporter.Models;

namespace BimTools.SpxExporter.Services
{
    public sealed class ReportService
    {
        public void WriteCsv(string path, IEnumerable<ExportJobItem> items)
        {
            EnsureDirectory(path);

            StringBuilder builder = new StringBuilder();
            builder.AppendLine("Model,Source Path,Revit Version,Workshared,Status,Message,Duration,Output Files");
            foreach (ExportJobItem item in items)
            {
                builder.AppendLine(CsvUtil.Row(
                    item.ModelName,
                    item.FilePath,
                    item.RevitVersion,
                    item.IsWorkshared ? "Yes" : "No",
                    item.Status.ToString(),
                    item.Message,
                    item.Duration,
                    item.OutputFiles));
            }

            File.WriteAllText(path, builder.ToString(), new UTF8Encoding(true));
        }

        public void WriteHtml(string path, IEnumerable<ExportJobItem> items, string summary, string engineName)
        {
            EnsureDirectory(path);

            StringBuilder builder = new StringBuilder();
            builder.AppendLine("<!DOCTYPE html><html><head><meta charset=\"utf-8\"><title>SPX Export Report</title>");
            builder.AppendLine("<style>body{font-family:Segoe UI,Arial,sans-serif;margin:32px;color:#1f2a37}h1{font-size:22px}");
            builder.AppendLine("table{border-collapse:collapse;width:100%}th,td{border:1px solid #d0d5dd;padding:6px 10px;text-align:left;font-size:13px;vertical-align:top}");
            builder.AppendLine("th{background:#f2f4f7}.Success{color:#067647;font-weight:600}.Failed{color:#b42318;font-weight:600}.Skipped,.Cancelled{color:#b54708;font-weight:600}</style></head><body>");
            builder.AppendLine("<h1>SPX Export Report</h1>");
            builder.AppendLine("<p>" + Html(engineName) + " &mdash; " + Html(DateTime.Now.ToString("yyyy-MM-dd HH:mm")) + "</p>");
            builder.AppendLine("<p>" + Html(summary) + "</p>");
            builder.AppendLine("<table><tr><th>Model</th><th>Status</th><th>Time</th><th>Message</th><th>Output</th></tr>");
            foreach (ExportJobItem item in items)
            {
                if (!item.IsSelected)
                {
                    continue;
                }

                string status = item.Status.ToString();
                builder.AppendLine(
                    "<tr><td>" + Html(item.FileName) + "</td><td class=\"" + status + "\">" + status + "</td><td>" +
                    Html(item.Duration) + "</td><td>" + Html(item.Message) + "</td><td>" + Html(item.OutputFiles) + "</td></tr>");
            }

            builder.AppendLine("</table></body></html>");
            File.WriteAllText(path, builder.ToString(), new UTF8Encoding(false));
        }

        private static string Html(string value)
        {
            return WebUtility.HtmlEncode(value ?? string.Empty);
        }

        private static void EnsureDirectory(string path)
        {
            string directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }
    }
}
