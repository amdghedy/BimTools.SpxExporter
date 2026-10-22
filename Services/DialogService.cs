using System.Collections.Generic;
using Microsoft.Win32;

namespace BimTools.SpxExporter.Services
{
    public sealed class DialogService
    {
        public IList<string> PickModels()
        {
            OpenFileDialog dialog = new OpenFileDialog
            {
                Title = "Select Revit models",
                Filter = "Revit models (*.rvt)|*.rvt",
                Multiselect = true,
                CheckFileExists = true
            };

            return dialog.ShowDialog() == true ? dialog.FileNames : new string[0];
        }

        public string PickFolder(string description, string initialFolder)
        {
            using (System.Windows.Forms.FolderBrowserDialog dialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                dialog.Description = description;
                dialog.ShowNewFolderButton = true;
                if (!string.IsNullOrEmpty(initialFolder) && System.IO.Directory.Exists(initialFolder))
                {
                    dialog.SelectedPath = initialFolder;
                }

                return dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK ? dialog.SelectedPath : null;
            }
        }

        public string PickExecutable()
        {
            OpenFileDialog dialog = new OpenFileDialog
            {
                Title = "Select converter executable",
                Filter = "Executables (*.exe;*.bat;*.cmd)|*.exe;*.bat;*.cmd|All files (*.*)|*.*",
                CheckFileExists = true
            };

            return dialog.ShowDialog() == true ? dialog.FileName : null;
        }

        public string PickSettingsToOpen()
        {
            OpenFileDialog dialog = new OpenFileDialog
            {
                Title = "Import export preset",
                Filter = "Export preset (*.json)|*.json",
                CheckFileExists = true
            };

            return dialog.ShowDialog() == true ? dialog.FileName : null;
        }

        public string PickSettingsToSave(string initialDirectory)
        {
            SaveFileDialog dialog = new SaveFileDialog
            {
                Title = "Save export preset",
                Filter = "Export preset (*.json)|*.json",
                FileName = "SpxExportPreset.json",
                InitialDirectory = initialDirectory
            };

            return dialog.ShowDialog() == true ? dialog.FileName : null;
        }

        public string PickListToOpen()
        {
            OpenFileDialog dialog = new OpenFileDialog
            {
                Title = "Import a list of model paths",
                Filter = "Text or CSV (*.txt;*.csv)|*.txt;*.csv",
                CheckFileExists = true
            };

            return dialog.ShowDialog() == true ? dialog.FileName : null;
        }

        public string PickListToSave()
        {
            SaveFileDialog dialog = new SaveFileDialog
            {
                Title = "Save the model list",
                Filter = "Text file (*.txt)|*.txt",
                FileName = "ModelList.txt"
            };

            return dialog.ShowDialog() == true ? dialog.FileName : null;
        }
    }
}
