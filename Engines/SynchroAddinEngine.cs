using System;
using System.Collections.Generic;
using System.IO;
using Autodesk.Revit.UI;
using BimTools.SpxExporter.Models;

namespace BimTools.SpxExporter.Engines
{
    public sealed class SynchroAddinEngine : IExportEngine
    {
        private static readonly TimeSpan SettleTime = TimeSpan.FromSeconds(3);

        public string DisplayName
        {
            get { return "SYNCHRO 4D exporter add-in (direct SPX)"; }
        }

        public string OutputExtension
        {
            get { return ".spx"; }
        }

        public bool RequiresActiveDocument
        {
            get { return true; }
        }

        public bool WritesPerModelFile
        {
            get { return true; }
        }

        public bool ProducesIntermediateFile
        {
            get { return false; }
        }

        public bool NeedsView(ExportSettings settings)
        {
            return true;
        }

        public string Validate(ExportSettings settings)
        {
            if (string.IsNullOrWhiteSpace(settings.SynchroCommandId))
            {
                return "Enter the command id of the installed SYNCHRO exporter button (use 'Scan journals' on the SYNCHRO (SPX) tab).";
            }

            if (RevitCommandId.LookupCommandId(settings.SynchroCommandId) == null)
            {
                return "Revit does not know the command id '" + settings.SynchroCommandId + "'. Check the SYNCHRO add-in is loaded and the id is correct.";
            }

            if (settings.SynchroTimeoutMinutes < 1)
            {
                return "The SYNCHRO timeout must be at least one minute.";
            }

            return null;
        }

        public ExportStepResult Begin(ExportContext context)
        {
            RevitCommandId commandId = RevitCommandId.LookupCommandId(context.Settings.SynchroCommandId);
            if (commandId == null)
            {
                return ExportStepResult.Failed("The SYNCHRO command id could not be resolved.");
            }

            if (!context.UiApp.CanPostCommand(commandId))
            {
                return ExportStepResult.Failed("Revit cannot run the SYNCHRO exporter right now.");
            }

            context.StartedUtc = DateTime.UtcNow;
            context.UiApp.PostCommand(commandId);
            return ExportStepResult.Pending("SYNCHRO exporter launched. Complete its dialog; the batch continues when the SPX file appears.");
        }

        public ExportStepResult Poll(ExportContext context)
        {
            if (context.ContinueRequested)
            {
                return ExportStepResult.Completed("Advanced manually.", FindNewFiles(context));
            }

            IList<string> files = FindNewFiles(context);
            if (files.Count > 0)
            {
                return ExportStepResult.Completed("SPX file detected.", files);
            }

            TimeSpan timeout = TimeSpan.FromMinutes(Math.Max(1, context.Settings.SynchroTimeoutMinutes));
            if (DateTime.UtcNow - context.StartedUtc > timeout)
            {
                return ExportStepResult.Failed("Timed out waiting for an SPX file from the SYNCHRO exporter.");
            }

            return ExportStepResult.Pending("Waiting for the SYNCHRO exporter to finish.");
        }

        private static IList<string> FindNewFiles(ExportContext context)
        {
            List<string> results = new List<string>();
            string folder = string.IsNullOrWhiteSpace(context.Settings.SynchroWatchFolder)
                ? context.OutputDirectory
                : context.Settings.SynchroWatchFolder;

            if (!Directory.Exists(folder))
            {
                return results;
            }

            foreach (string file in Directory.EnumerateFiles(folder, "*.spx", SearchOption.AllDirectories))
            {
                DateTime written = File.GetLastWriteTimeUtc(file);
                if (written < context.StartedUtc.AddSeconds(-2))
                {
                    continue;
                }

                if (DateTime.UtcNow - written < SettleTime)
                {
                    return new List<string>();
                }

                results.Add(file);
            }

            return results;
        }
    }
}
