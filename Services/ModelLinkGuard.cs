using System;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.DB;

namespace BimTools.SpxExporter.Services
{
    /// <summary>
    /// A model that is loaded as a Revit link inside another open model cannot be opened as a primary model
    /// ("Document is a linked file"). This guard detects that situation and releases links of models we opened.
    /// </summary>
    public static class ModelLinkGuard
    {
        public static bool IsOpenAsLink(Application application, string path)
        {
            foreach (Document document in application.Documents)
            {
                if (document.IsLinked && string.Equals(document.PathName, path, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        public static void UnloadLinks(Document document)
        {
            if (document == null || !document.IsValidObject)
            {
                return;
            }

            using (FilteredElementCollector collector = new FilteredElementCollector(document))
            {
                collector.OfClass(typeof(RevitLinkType));
                foreach (Element element in collector)
                {
                    RevitLinkType linkType = element as RevitLinkType;
                    if (linkType == null || !RevitLinkType.IsLoaded(document, linkType.Id))
                    {
                        continue;
                    }

                    try
                    {
                        linkType.Unload(null);
                    }
                    catch (Autodesk.Revit.Exceptions.InvalidOperationException)
                    {
                        continue;
                    }
                }
            }
        }

        public static string LinkedMessage(string fileName)
        {
            return fileName + " is currently loaded as a link inside another open Revit model, so it cannot be opened on its own. "
                + "Close that model (or unload the link) and run the batch again.";
        }
    }
}
