using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace BimTools.SpxExporter.Infrastructure
{
    internal static class PatternMatcher
    {
        public static string[] Split(string patterns)
        {
            if (string.IsNullOrWhiteSpace(patterns))
            {
                return new string[0];
            }

            List<string> result = new List<string>();
            foreach (string part in patterns.Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries))
            {
                string trimmed = part.Trim();
                if (trimmed.Length > 0)
                {
                    result.Add(trimmed);
                }
            }

            return result.ToArray();
        }

        public static bool MatchesAny(string name, string[] patterns)
        {
            if (string.IsNullOrEmpty(name))
            {
                return false;
            }

            foreach (string pattern in patterns)
            {
                if (pattern.IndexOf('*') >= 0 || pattern.IndexOf('?') >= 0)
                {
                    string regex = "^" + Regex.Escape(pattern).Replace("\\*", ".*").Replace("\\?", ".") + "$";
                    if (Regex.IsMatch(name, regex, RegexOptions.IgnoreCase))
                    {
                        return true;
                    }
                }
                else if (name.IndexOf(pattern, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
