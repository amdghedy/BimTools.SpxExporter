namespace BimTools.SpxExporter.Infrastructure
{
    internal static class CsvUtil
    {
        public static string Escape(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            if (value.IndexOfAny(new[] { ',', '"', '\r', '\n' }) < 0)
            {
                return value;
            }

            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }

        public static string Row(params string[] cells)
        {
            string[] escaped = new string[cells.Length];
            for (int i = 0; i < cells.Length; i++)
            {
                escaped[i] = Escape(cells[i]);
            }

            return string.Join(",", escaped);
        }
    }
}
