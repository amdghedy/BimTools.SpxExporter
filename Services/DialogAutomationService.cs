using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows.Automation;
using BimTools.SpxExporter.Models;

namespace BimTools.SpxExporter.Services
{
    /// <summary>
    /// Watches for dialogs opened by another add-in (for example the SYNCHRO exporter) while an export is armed,
    /// fills in the file / folder path, applies configured field rules and presses the configured buttons.
    /// Runs on a worker thread through UI Automation so it still works while a modal dialog blocks Revit.
    /// </summary>
    public sealed class DialogAutomationService : IDisposable
    {
        private const int MaxAttemptsPerWindow = 8;
        private const int MaxDescribedControls = 60;

        private readonly int _processId;
        private readonly int _mainWindowHandle;
        private readonly string[] _buttons;
        private readonly bool _wantShared;
        private readonly List<KeyValuePair<string, string>> _rules;
        private readonly LogService _log;
        private readonly HashSet<int> _excludedHandles = new HashSet<int>();
        private readonly Dictionary<string, int> _attempts = new Dictionary<string, int>();
        private readonly HashSet<string> _described = new HashSet<string>();
        private readonly object _sync = new object();

        private Timer _timer;
        private int _busy;
        private int _errorCount;
        private volatile bool _armed;
        private volatile string _targetPath = string.Empty;

        public DialogAutomationService(int processId, int mainWindowHandle, ExportSettings settings, LogService log)
        {
            _processId = processId;
            _mainWindowHandle = mainWindowHandle;
            _log = log;
            _buttons = Infrastructure.PatternMatcher.Split(settings.AutoClickButtons);
            _rules = ParseRules(settings.DialogFieldOverrides);
            _wantShared = settings.CoordinateSystem == CoordinateSystemOption.Shared;
        }

        public void ExcludeWindow(int handle)
        {
            lock (_sync)
            {
                _excludedHandles.Add(handle);
            }
        }

        public void Start()
        {
            _timer = new Timer(OnTick, null, 500, 400);
        }

        public void Arm(string targetPath)
        {
            _targetPath = targetPath ?? string.Empty;
            lock (_sync)
            {
                _attempts.Clear();
            }

            _armed = true;
        }

        public void Disarm()
        {
            _armed = false;
            _targetPath = string.Empty;
        }

        public void Dispose()
        {
            _armed = false;
            Timer timer = _timer;
            _timer = null;
            if (timer != null)
            {
                timer.Dispose();
            }
        }

        private static List<KeyValuePair<string, string>> ParseRules(string text)
        {
            List<KeyValuePair<string, string>> rules = new List<KeyValuePair<string, string>>();
            if (string.IsNullOrWhiteSpace(text))
            {
                return rules;
            }

            foreach (string part in text.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
            {
                int separator = part.IndexOf('=');
                if (separator < 0)
                {
                    rules.Add(new KeyValuePair<string, string>(Normalize(part), null));
                }
                else
                {
                    rules.Add(new KeyValuePair<string, string>(Normalize(part.Substring(0, separator)), part.Substring(separator + 1).Trim()));
                }
            }

            return rules;
        }

        private static string Normalize(string text)
        {
            return (text ?? string.Empty).Replace("&", string.Empty).Replace(":", string.Empty).Trim();
        }

        private void OnTick(object state)
        {
            if (!_armed || Interlocked.Exchange(ref _busy, 1) == 1)
            {
                return;
            }

            try
            {
                ScanWindows();
            }
            catch (Exception ex)
            {
                // A background thread must never let an exception reach Revit.
                if (Interlocked.Increment(ref _errorCount) <= 5)
                {
                    _log.Warn("Dialog automation: " + ex.Message);
                }
            }
            finally
            {
                Interlocked.Exchange(ref _busy, 0);
            }
        }

        private void ScanWindows()
        {
            Condition condition = new PropertyCondition(AutomationElement.ProcessIdProperty, _processId);
            AutomationElementCollection windows = AutomationElement.RootElement.FindAll(TreeScope.Children, condition);
            foreach (AutomationElement window in windows)
            {
                int handle = window.Current.NativeWindowHandle;
                bool excluded;
                lock (_sync)
                {
                    excluded = _excludedHandles.Contains(handle);
                }

                if (handle == _mainWindowHandle || excluded || window.Current.ControlType != ControlType.Window)
                {
                    continue;
                }

                if (!IsDialog(window))
                {
                    continue;
                }

                HandleDialog(window);
            }
        }

        private static bool IsDialog(AutomationElement window)
        {
            if (string.Equals(window.Current.ClassName, "#32770", StringComparison.Ordinal))
            {
                return true;
            }

            object pattern;
            if (window.TryGetCurrentPattern(WindowPattern.Pattern, out pattern))
            {
                return ((WindowPattern)pattern).Current.IsModal;
            }

            return false;
        }

        private void HandleDialog(AutomationElement window)
        {
            string key = string.Join(".", window.GetRuntimeId());
            int attempts;
            lock (_sync)
            {
                _attempts.TryGetValue(key, out attempts);
                if (attempts >= MaxAttemptsPerWindow)
                {
                    return;
                }

                _attempts[key] = attempts + 1;
            }

            AutomationElementCollection everything = window.FindAll(TreeScope.Descendants, Condition.TrueCondition);
            Describe(window, key, everything);
            ApplyRules(everything);
            ApplyCoordinateChoice(key, everything);
            SetPathFields(everything);
            PressButton(window, everything);
        }

        private void Describe(AutomationElement window, string key, AutomationElementCollection everything)
        {
            lock (_sync)
            {
                if (!_described.Add(key))
                {
                    return;
                }
            }

            StringBuilder builder = new StringBuilder();
            builder.Append("Dialog detected: '").Append(window.Current.Name).Append("' [");
            int count = 0;
            foreach (AutomationElement element in everything)
            {
                string name = Normalize(element.Current.Name);
                if (name.Length == 0)
                {
                    continue;
                }

                string type = element.Current.ControlType.ProgrammaticName.Replace("ControlType.", string.Empty);
                if (type == "Text" || type == "Pane" || type == "Group")
                {
                    continue;
                }

                if (count++ > 0)
                {
                    builder.Append(", ");
                }

                builder.Append(type).Append(':').Append(name);
                if (count >= MaxDescribedControls)
                {
                    break;
                }
            }

            builder.Append(']');
            _log.Info(builder.ToString());
        }

        private void ApplyCoordinateChoice(string windowKey, AutomationElementCollection everything)
        {
            string[] keywords = _wantShared ? new[] { "shared" } : new[] { "project", "internal", "base point" };
            foreach (AutomationElement element in everything)
            {
                ControlType type = element.Current.ControlType;
                if (type != ControlType.RadioButton && type != ControlType.CheckBox && type != ControlType.ListItem)
                {
                    continue;
                }

                string name = Normalize(element.Current.Name);
                bool matches = false;
                foreach (string keyword in keywords)
                {
                    if (name.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0 && (name.IndexOf("coord", StringComparison.OrdinalIgnoreCase) >= 0 || name.Length <= 24))
                    {
                        matches = true;
                    }
                }

                if (!matches)
                {
                    continue;
                }

                object pattern;
                if (element.TryGetCurrentPattern(SelectionItemPattern.Pattern, out pattern))
                {
                    SelectionItemPattern item = (SelectionItemPattern)pattern;
                    if (!item.Current.IsSelected)
                    {
                        item.Select();
                        LogOnce(windowKey + "|" + name, "Dialog automation: coordinate option '" + name + "' selected.");
                    }
                }
                else if (type == ControlType.CheckBox && element.TryGetCurrentPattern(TogglePattern.Pattern, out pattern))
                {
                    TogglePattern toggle = (TogglePattern)pattern;
                    if (toggle.Current.ToggleState != ToggleState.On)
                    {
                        toggle.Toggle();
                        LogOnce(windowKey + "|" + name, "Dialog automation: coordinate option '" + name + "' enabled.");
                    }
                }
            }
        }

        private void LogOnce(string key, string message)
        {
            lock (_sync)
            {
                if (!_described.Add(key))
                {
                    return;
                }
            }

            _log.Info(message);
        }

        private void SetPathFields(AutomationElementCollection everything)
        {
            string target = _targetPath;
            if (string.IsNullOrEmpty(target))
            {
                return;
            }

            foreach (AutomationElement element in everything)
            {
                if (element.Current.ControlType != ControlType.Edit)
                {
                    continue;
                }

                string name = Normalize(element.Current.Name).ToUpperInvariant();
                string id = element.Current.AutomationId;
                string value = null;
                if (name == "FILE NAME" || name == "FILENAME" || id == "1001")
                {
                    value = target;
                }
                else if (name == "FOLDER")
                {
                    value = Path.GetDirectoryName(target);
                }

                if (value == null)
                {
                    continue;
                }

                object pattern;
                if (element.TryGetCurrentPattern(ValuePattern.Pattern, out pattern))
                {
                    ValuePattern valuePattern = (ValuePattern)pattern;
                    if (!valuePattern.Current.IsReadOnly && !string.Equals(valuePattern.Current.Value, value, StringComparison.OrdinalIgnoreCase))
                    {
                        valuePattern.SetValue(value);
                        _log.Info("Dialog automation: path set to " + value);
                    }
                }
            }
        }

        private void ApplyRules(AutomationElementCollection everything)
        {
            foreach (KeyValuePair<string, string> rule in _rules)
            {
                foreach (AutomationElement element in everything)
                {
                    if (!string.Equals(Normalize(element.Current.Name), rule.Key, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    ApplyRule(element, rule.Value);
                    break;
                }
            }
        }

        private void ApplyRule(AutomationElement element, string value)
        {
            ControlType type = element.Current.ControlType;
            object pattern;

            if (type == ControlType.CheckBox)
            {
                bool desired = value == null
                    || value.Equals("true", StringComparison.OrdinalIgnoreCase)
                    || value.Equals("on", StringComparison.OrdinalIgnoreCase)
                    || value.Equals("yes", StringComparison.OrdinalIgnoreCase)
                    || value == "1";
                if (element.TryGetCurrentPattern(TogglePattern.Pattern, out pattern))
                {
                    TogglePattern toggle = (TogglePattern)pattern;
                    bool isOn = toggle.Current.ToggleState == ToggleState.On;
                    if (isOn != desired)
                    {
                        toggle.Toggle();
                    }
                }

                return;
            }

            if (value == null)
            {
                if (element.TryGetCurrentPattern(SelectionItemPattern.Pattern, out pattern))
                {
                    SelectionItemPattern item = (SelectionItemPattern)pattern;
                    if (!item.Current.IsSelected)
                    {
                        item.Select();
                    }
                }
                else if (element.TryGetCurrentPattern(InvokePattern.Pattern, out pattern))
                {
                    QueueInvoke((InvokePattern)pattern, element.Current.Name);
                }

                return;
            }

            if (element.TryGetCurrentPattern(ValuePattern.Pattern, out pattern) && !((ValuePattern)pattern).Current.IsReadOnly)
            {
                ValuePattern valuePattern = (ValuePattern)pattern;
                if (!string.Equals(valuePattern.Current.Value, value, StringComparison.OrdinalIgnoreCase))
                {
                    valuePattern.SetValue(value);
                }

                return;
            }

            if (element.TryGetCurrentPattern(ExpandCollapsePattern.Pattern, out pattern))
            {
                ExpandCollapsePattern expander = (ExpandCollapsePattern)pattern;
                expander.Expand();
                AutomationElementCollection items = element.FindAll(
                    TreeScope.Descendants,
                    new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.ListItem));
                foreach (AutomationElement item in items)
                {
                    if (string.Equals(Normalize(item.Current.Name), value, StringComparison.OrdinalIgnoreCase)
                        && item.TryGetCurrentPattern(SelectionItemPattern.Pattern, out pattern))
                    {
                        ((SelectionItemPattern)pattern).Select();
                        break;
                    }
                }

                expander.Collapse();
            }
        }

        private void PressButton(AutomationElement window, AutomationElementCollection everything)
        {
            foreach (string wanted in _buttons)
            {
                foreach (AutomationElement element in everything)
                {
                    if (element.Current.ControlType != ControlType.Button || !element.Current.IsEnabled)
                    {
                        continue;
                    }

                    if (!string.Equals(Normalize(element.Current.Name), wanted, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    object pattern;
                    if (element.TryGetCurrentPattern(InvokePattern.Pattern, out pattern))
                    {
                        _log.Info("Dialog automation: pressing '" + wanted + "' on '" + window.Current.Name + "'.");
                        QueueInvoke((InvokePattern)pattern, wanted);
                        return;
                    }
                }
            }
        }

        private void QueueInvoke(InvokePattern pattern, string description)
        {
            ThreadPool.QueueUserWorkItem(delegate
            {
                try
                {
                    pattern.Invoke();
                }
                catch (Exception ex)
                {
                    // Invoke may fail if the dialog closed first; this runs on a pool thread and must not throw.
                    _log.Warn("Dialog automation could not press '" + description + "': " + ex.Message);
                }
            });
        }
    }
}
