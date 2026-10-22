using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace BimTools.SpxExporter.Models
{
    public sealed class ExportContext
    {
        public UIApplication UiApp { get; set; }

        public Document Document { get; set; }

        public ExportJobItem Item { get; set; }

        public ExportSettings Settings { get; set; }

        public string OutputDirectory { get; set; }

        public string BaseName { get; set; }

        public View3D ExportView { get; set; }

        public bool TemporaryViewCreated { get; set; }

        public DateTime StartedUtc { get; set; }

        public bool ContinueRequested { get; set; }

        public List<string> OutputFiles { get; } = new List<string>();
    }
}
