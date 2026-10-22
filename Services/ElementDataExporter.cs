using System.Collections.Generic;
using System.IO;
using System.Text;
using Autodesk.Revit.DB;
using BimTools.SpxExporter.Infrastructure;
using BimTools.SpxExporter.Models;

namespace BimTools.SpxExporter.Services
{
    public sealed class ElementDataExporter
    {
        public int Write(Document doc, ExportSettings settings, string path)
        {
            string[] parameterNames = PatternMatcher.Split(settings.DataParameterNames);
            string[] categoryFilter = PatternMatcher.Split(settings.DataCategoryFilter);
            Dictionary<string, string> elementNames = new Dictionary<string, string>();
            Dictionary<string, string[]> typeNames = new Dictionary<string, string[]>();
            Dictionary<int, string> worksetNames = new Dictionary<int, string>();
            int rows = 0;

            string directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            using (StreamWriter writer = new StreamWriter(path, false, new UTF8Encoding(true)))
            {
                List<string> header = new List<string>
                {
                    "ElementId", "UniqueId", "Category", "Family", "Type", "Level", "PhaseCreated", "PhaseDemolished", "Workset"
                };
                header.AddRange(parameterNames);
                writer.WriteLine(CsvUtil.Row(header.ToArray()));

                using (FilteredElementCollector collector = new FilteredElementCollector(doc))
                {
                    collector.WhereElementIsNotElementType();
                    foreach (Element element in collector)
                    {
                        try
                        {
                            Category category = element.Category;
                            if (category == null || category.CategoryType != CategoryType.Model || element.ViewSpecific)
                            {
                                continue;
                            }

                            if (categoryFilter.Length > 0 && !PatternMatcher.MatchesAny(category.Name, categoryFilter))
                            {
                                continue;
                            }

                            ElementId typeId = element.GetTypeId();
                            string[] names = ResolveTypeNames(doc, typeId, typeNames);
                            Element typeElement = settings.DataIncludeTypeParameters ? doc.GetElement(typeId) : null;

                            List<string> cells = new List<string>
                            {
                                element.Id.ToString(),
                                element.UniqueId,
                                category.Name,
                                names[0],
                                names[1],
                                NameOf(doc, element.LevelId, elementNames),
                                NameOf(doc, element.CreatedPhaseId, elementNames),
                                NameOf(doc, element.DemolishedPhaseId, elementNames),
                                WorksetNameOf(doc, element, worksetNames)
                            };

                            foreach (string parameterName in parameterNames)
                            {
                                cells.Add(ReadParameter(element, typeElement, parameterName));
                            }

                            writer.WriteLine(CsvUtil.Row(cells.ToArray()));
                            rows++;
                        }
                        finally
                        {
                            element.Dispose();
                        }
                    }
                }
            }

            return rows;
        }

        private static string NameOf(Document doc, ElementId id, Dictionary<string, string> cache)
        {
            if (id == null || ElementId.InvalidElementId.Equals(id))
            {
                return string.Empty;
            }

            string key = id.ToString();
            string name;
            if (cache.TryGetValue(key, out name))
            {
                return name;
            }

            Element target = doc.GetElement(id);
            name = target == null ? string.Empty : target.Name;
            cache[key] = name;
            return name;
        }

        private static string[] ResolveTypeNames(Document doc, ElementId typeId, Dictionary<string, string[]> cache)
        {
            if (typeId == null || ElementId.InvalidElementId.Equals(typeId))
            {
                return new[] { string.Empty, string.Empty };
            }

            string key = typeId.ToString();
            string[] names;
            if (cache.TryGetValue(key, out names))
            {
                return names;
            }

            ElementType type = doc.GetElement(typeId) as ElementType;
            names = type == null
                ? new[] { string.Empty, string.Empty }
                : new[] { type.FamilyName ?? string.Empty, type.Name ?? string.Empty };
            cache[key] = names;
            return names;
        }

        private static string WorksetNameOf(Document doc, Element element, Dictionary<int, string> cache)
        {
            if (!doc.IsWorkshared)
            {
                return string.Empty;
            }

            WorksetId id = element.WorksetId;
            string name;
            if (cache.TryGetValue(id.IntegerValue, out name))
            {
                return name;
            }

            Workset workset = doc.GetWorksetTable().GetWorkset(id);
            name = workset == null ? string.Empty : workset.Name;
            cache[id.IntegerValue] = name;
            return name;
        }

        private static string ReadParameter(Element element, Element typeElement, string name)
        {
            Parameter parameter = element.LookupParameter(name);
            if ((parameter == null || !parameter.HasValue) && typeElement != null)
            {
                parameter = typeElement.LookupParameter(name);
            }

            if (parameter == null || !parameter.HasValue)
            {
                return string.Empty;
            }

            if (parameter.StorageType == StorageType.String)
            {
                return parameter.AsString() ?? string.Empty;
            }

            return parameter.AsValueString() ?? string.Empty;
        }
    }
}
