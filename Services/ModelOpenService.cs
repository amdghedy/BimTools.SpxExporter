using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using BimTools.SpxExporter.Infrastructure;
using BimTools.SpxExporter.Models;

namespace BimTools.SpxExporter.Services
{
    public sealed class ModelOpenService
    {
        public Document OpenBackground(Autodesk.Revit.ApplicationServices.Application application, ExportJobItem item, ExportSettings settings)
        {
            ModelPath modelPath = ModelPathUtils.ConvertUserVisiblePathToModelPath(item.FilePath);
            OpenOptions options = BuildOptions(modelPath, item, settings);
            try
            {
                return application.OpenDocumentFile(modelPath, options);
            }
            finally
            {
                Disposer.Release(options);
            }
        }

        public UIDocument OpenAndActivate(UIApplication uiApplication, ExportJobItem item, ExportSettings settings)
        {
            ModelPath modelPath = ModelPathUtils.ConvertUserVisiblePathToModelPath(item.FilePath);
            OpenOptions options = BuildOptions(modelPath, item, settings);
            try
            {
                return uiApplication.OpenAndActivateDocument(modelPath, options, false);
            }
            finally
            {
                Disposer.Release(options);
            }
        }

        private static OpenOptions BuildOptions(ModelPath modelPath, ExportJobItem item, ExportSettings settings)
        {
            OpenOptions options = new OpenOptions();
            options.Audit = settings.AuditOnOpen;
            options.AllowOpeningLocalByWrongUser = true;

            if (item.IsWorkshared)
            {
                switch (settings.DetachMode)
                {
                    case DetachMode.DetachAndPreserveWorksets:
                        options.DetachFromCentralOption = DetachFromCentralOption.DetachAndPreserveWorksets;
                        break;
                    case DetachMode.DetachAndDiscardWorksets:
                        options.DetachFromCentralOption = DetachFromCentralOption.DetachAndDiscardWorksets;
                        break;
                    default:
                        options.DetachFromCentralOption = DetachFromCentralOption.DoNotDetach;
                        break;
                }

                if (settings.DetachMode != DetachMode.DetachAndDiscardWorksets)
                {
                    options.SetOpenWorksetsConfiguration(BuildWorksetConfiguration(modelPath, settings));
                }
            }

            return options;
        }

        private static WorksetConfiguration BuildWorksetConfiguration(ModelPath modelPath, ExportSettings settings)
        {
            if (settings.WorksetMode == WorksetMode.OpenAll)
            {
                return new WorksetConfiguration(WorksetConfigurationOption.OpenAllWorksets);
            }

            if (settings.WorksetMode == WorksetMode.OpenLastViewed)
            {
                return new WorksetConfiguration(WorksetConfigurationOption.OpenLastViewed);
            }

            string[] includes = PatternMatcher.Split(settings.WorksetIncludePatterns);
            string[] excludes = PatternMatcher.Split(settings.WorksetExcludePatterns);

            WorksetConfiguration configuration = new WorksetConfiguration(WorksetConfigurationOption.CloseAllWorksets);
            List<WorksetId> toOpen = new List<WorksetId>();
            foreach (WorksetPreview preview in WorksharingUtils.GetUserWorksetInfo(modelPath))
            {
                if (includes.Length > 0 && !PatternMatcher.MatchesAny(preview.Name, includes))
                {
                    continue;
                }

                if (PatternMatcher.MatchesAny(preview.Name, excludes))
                {
                    continue;
                }

                toOpen.Add(preview.Id);
            }

            configuration.Open(toOpen);
            return configuration;
        }
    }
}
