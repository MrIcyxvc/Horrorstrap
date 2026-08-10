using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using Microsoft.Win32;
using Wpf.Ui.Mvvm.Contracts;

namespace Bloxstrap.UI.Elements.Settings.Pages
{
    public partial class IxpEditorPage
    {
        private readonly ObservableCollection<IxpEntry> _ixpList = new();

        public IxpEditorPage()
        {
            InitializeComponent();
            App.BubbleRPC?.SetPage("IXP Editor");
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            Models.RuntimeFlagStore.Load();
            ReloadList();
        }

        private async void ReloadList()
        {
            _ixpList.Clear();

            try
            {
                var settings = await RobloxInterfaces.ApplicationSettings.PCDesktopClient.GetAllAsync();
                foreach (var kv in settings.Where(x => IsIxpFlag(x.Key)).OrderBy(x => x.Key))
                {
                    // runtime override takes precedence
                    string? overrideValue = Models.RuntimeFlagStore.Flags.TryGetValue(kv.Key, out var overrideObj)
                        ? overrideObj?.ToString()
                        : null;

                    _ixpList.Add(new IxpEntry
                    {
                        Name = kv.Key,
                        Value = overrideValue ?? kv.Value,
                        IxpVariable = ExtractIxpVariable(kv.Key),
                        IsOverridden = overrideValue is not null
                    });
                }
            }
            catch (Exception ex)
            {
                App.Logger.WriteLine("IxpEditorPage", $"Failed to load IXP list from Roblox: {ex.Message}");
            }

            // also include any manually added runtime flags that aren't IXP variables
            try
            {
                foreach (var kv in Models.RuntimeFlagStore.Flags.OrderBy(x => x.Key))
                {
                    if (_ixpList.Any(x => x.Name == kv.Key))
                        continue;

                    _ixpList.Add(new IxpEntry
                    {
                        Name = kv.Key,
                        Value = kv.Value?.ToString() ?? string.Empty,
                        IxpVariable = ExtractIxpVariable(kv.Key),
                        IsOverridden = true
                    });
                }
            }
            catch (Exception ex)
            {
                App.Logger.WriteLine("IxpEditorPage", $"Failed to load runtime flags: {ex.Message}");
            }

            if (this.FindName("DataGrid") is System.Windows.Controls.DataGrid dg)
            {
                if (dg.ItemsSource is null)
                    dg.ItemsSource = _ixpList;

                ApplySearchFilter(dg);
            }
        }

        private static bool IsIxpFlag(string flagName) =>
            flagName.EndsWith("_IXPValue", StringComparison.OrdinalIgnoreCase)
            || flagName.Contains("Ixp", StringComparison.OrdinalIgnoreCase);

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            if (Window.GetWindow(this) is INavigationWindow window)
                window.Navigate(typeof(FastFlagsPage));
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            if (!Utilities.IsAdmin())
            {
                Frontend.ShowMessageBox("Runtime flag injection can only be configured while Horrorstrap is running as administrator.", MessageBoxImage.Warning);
                return;
            }

            var dlg = new SimpleInputDialog("Add IXP Entry", "Key:", "Value:");
            if (dlg.ShowDialog() != true)
                return;

            try
            {
                var entries = ParseDialogInput(dlg);
                if (entries.Count == 0)
                    return;

                foreach (var kv in entries)
                {
                    Models.RuntimeFlagStore.SetValue(kv.Key, kv.Value);

                    string fastFlagName = kv.Key;
                    if (fastFlagName.EndsWith("_IXPValue", StringComparison.OrdinalIgnoreCase))
                        fastFlagName = fastFlagName[..^"_IXPValue".Length];

                    App.FastFlags.SetValue(fastFlagName, kv.Value.ToString()!);
                }

                App.FastFlags.Save();
                ReloadList();
                Frontend.ShowMessageBox($"Added {entries.Count} runtime flag(s).", MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                App.Logger.WriteLine("IxpEditorPage", $"Failed to add IXP entry: {ex.Message}");
                Frontend.ShowMessageBox($"Failed to add entry: {ex.Message}", MessageBoxImage.Error);
            }
        }

        private Dictionary<string, object> ParseDialogInput(SimpleInputDialog dlg)
        {
            var result = new Dictionary<string, object>();

            if (!string.IsNullOrWhiteSpace(dlg.Value) && dlg.Value.StartsWith("{"))
            {
                var parsed = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(dlg.Value);
                if (parsed is not null)
                {
                    foreach (var kv in parsed)
                    {
                        var v = ConvertJsonElement(kv.Value);
                        if (v is not null)
                            result[kv.Key] = v;
                    }
                    return result;
                }
            }

            if (string.IsNullOrWhiteSpace(dlg.Key))
                return result;

            var singleValue = ConvertValueString(dlg.Value);
            if (singleValue is not null)
                result[dlg.Key] = singleValue;

            return result;
        }

        private object? ConvertJsonElement(JsonElement element)
        {
            if (element.ValueKind == JsonValueKind.True) return true;
            if (element.ValueKind == JsonValueKind.False) return false;
            if (element.ValueKind == JsonValueKind.Number)
            {
                if (element.TryGetInt32(out int i)) return i;
                if (element.TryGetInt64(out long l)) return l;
                if (element.TryGetDouble(out double d)) return d;
            }
            if (element.ValueKind == JsonValueKind.String)
                return ConvertValueString(element.GetString() ?? "");
            return null;
        }

        private object? ConvertValueString(string value)
        {
            string v = value.Trim();
            if (string.IsNullOrEmpty(v))
                return null;

            if (bool.TryParse(v, out bool b))
                return b;

            if (int.TryParse(v, out int i))
                return i;

            if (long.TryParse(v, out long l))
                return l;

            if (double.TryParse(v, out double d))
                return d;

            return null;
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (!Utilities.IsAdmin())
            {
                Frontend.ShowMessageBox("Runtime flag injection can only be configured while Horrorstrap is running as administrator.", MessageBoxImage.Warning);
                return;
            }

            if (this.FindName("DataGrid") is not System.Windows.Controls.DataGrid dg)
                return;

            var selected = dg.SelectedItems.Cast<IxpEntry>().ToList();
            if (selected.Count == 0)
                return;

            var confirmMsg = string.Format("Are you sure you want to delete {0} selected entries?", selected.Count);
            if (Frontend.ShowMessageBox(confirmMsg, MessageBoxImage.Warning, System.Windows.MessageBoxButton.YesNo) != System.Windows.MessageBoxResult.Yes)
                return;

            foreach (var item in selected)
            {
                Models.RuntimeFlagStore.Remove(item.Name);

                string fastFlagName = item.Name;
                if (fastFlagName.EndsWith("_IXPValue", StringComparison.OrdinalIgnoreCase))
                    fastFlagName = fastFlagName[..^"_IXPValue".Length];

                App.FastFlags.SetValue(fastFlagName, null);
            }

            App.FastFlags.Save();
            ReloadList();
        }

        private void ExportJSONButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dlg = new SaveFileDialog { Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*", FileName = "runtime-flags.json" };
                if (dlg.ShowDialog() != true)
                    return;

                string json = JsonSerializer.Serialize(Models.RuntimeFlagStore.Flags, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(dlg.FileName, json);
                Frontend.ShowMessageBox($"Exported {Models.RuntimeFlagStore.Flags.Count} runtime flag(s).", MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                App.Logger.WriteLine("IxpEditorPage", $"Export failed: {ex.Message}");
                Frontend.ShowMessageBox($"Export failed: {ex.Message}", MessageBoxImage.Error);
            }
        }

        private void SearchTextBox_TextChanged(object sender, RoutedEventArgs e)
        {
            if (this.FindName("DataGrid") is System.Windows.Controls.DataGrid dg)
                ApplySearchFilter(dg);
        }

        private void ApplySearchFilter(System.Windows.Controls.DataGrid dg)
        {
            if (this.FindName("SearchTextBox") is not System.Windows.Controls.TextBox searchBox)
                return;

            string filter = searchBox.Text.Trim().ToLowerInvariant();
            var filtered = string.IsNullOrEmpty(filter)
                ? _ixpList
                : new ObservableCollection<IxpEntry>(_ixpList.Where(x =>
                    x.Name.ToLowerInvariant().Contains(filter) ||
                    x.Value.ToLowerInvariant().Contains(filter) ||
                    x.IxpVariable.ToLowerInvariant().Contains(filter)));

            dg.ItemsSource = filtered;
        }

        private static string ExtractIxpVariable(string flagName)
        {
            const string suffix = "_IXPValue";
            if (flagName.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                flagName = flagName[..^suffix.Length];

            // Common prefixes to strip
            string[] prefixes = { "FFlag", "DFFlag", "FInt", "DFInt", "FString", "DFString", "FLog" };
            foreach (var prefix in prefixes)
            {
                if (flagName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    flagName = flagName.Substring(prefix.Length);
                    break;
                }
            }

            return flagName;
        }

        private void DataGrid_CellEditEnding(object sender, System.Windows.Controls.DataGridCellEditEndingEventArgs e)
        {
            if (!Utilities.IsAdmin())
            {
                Frontend.ShowMessageBox("Runtime flag injection can only be configured while Horrorstrap is running as administrator.", MessageBoxImage.Warning);
                e.Cancel = true;
                return;
            }

            try
            {
                if (e.Row.Item is not IxpEntry entry)
                    return;

                if (e.EditingElement is not System.Windows.Controls.TextBox tb)
                    return;

                var newValue = ConvertValueString(tb.Text);
                if (newValue is null)
                {
                    Frontend.ShowMessageBox("Only bool or numeric values are supported for runtime injection.", MessageBoxImage.Warning);
                    e.Cancel = true;
                    return;
                }

                Models.RuntimeFlagStore.SetValue(entry.Name, newValue);

                // sync IXP override to ClientAppSettings.json so it bypasses server-provided values on launch
                string fastFlagName = entry.Name;
                if (fastFlagName.EndsWith("_IXPValue", StringComparison.OrdinalIgnoreCase))
                    fastFlagName = fastFlagName[..^"_IXPValue".Length];

                App.FastFlags.SetValue(fastFlagName, newValue.ToString()!);
                App.FastFlags.Save();

                entry.IsOverridden = true;
                entry.Value = newValue.ToString()!;
            }
            catch (Exception ex)
            {
                App.Logger.WriteLine("IxpEditorPage", $"Failed to commit edit: {ex.Message}");
                Frontend.ShowMessageBox($"Failed to update value: {ex.Message}", MessageBoxImage.Error);
            }
        }
    }

    public class IxpEntry
    {
        public string Name { get; set; } = "";
        public string Value { get; set; } = "";
        public string IxpVariable { get; set; } = "";
        public bool IsOverridden { get; set; } = false;
    }
}
