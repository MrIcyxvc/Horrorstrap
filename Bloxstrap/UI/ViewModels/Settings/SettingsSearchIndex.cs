using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Windows;
using System.Xml;

namespace Bloxstrap.UI.ViewModels.Settings
{
    public static class SettingsSearchIndex
    {
        private static readonly Dictionary<Type, List<SearchResult>> _index = new();
        private static readonly List<SearchResult> _allResults = new();

        public static IReadOnlyList<SearchResult> Results => _allResults;
        public static IReadOnlyList<string> Suggestions => _allResults.Select(r => r.Text).ToList();

        public static void Build()
        {
            _index.Clear();
            _allResults.Clear();

            var pages = new (Type Type, string FileName)[]
            {
                (typeof(Elements.Settings.Pages.IntegrationsPage), "IntegrationsPage.xaml"),
                (typeof(Elements.Settings.Pages.BehaviourPage), "BootstrapperPage.xaml"),
                (typeof(Elements.Settings.Pages.ChannelPage), "ChannelPage.xaml"),
                (typeof(Elements.Settings.Pages.ModsPage), "ModsPage.xaml"),
                (typeof(Elements.Settings.Pages.FastFlagsPage), "FastFlagsPage.xaml"),
                (typeof(Elements.Settings.Pages.GlobalSettingsPage), "GlobalSettingsPage.xaml"),
                (typeof(Elements.Settings.Pages.AppearancePage), "AppearancePage.xaml"),
                (typeof(Elements.Settings.Pages.ShortcutsPage), "ShortcutsPage.xaml"),
            };

            string pagesDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "UI", "Elements", "Settings", "Pages");
            if (!Directory.Exists(pagesDir))
                pagesDir = Path.Combine(AppContext.BaseDirectory, "UI", "Elements", "Settings", "Pages");

            foreach (var (pageType, fileName) in pages)
            {
                var pageResults = new List<SearchResult>();
                string filePath = Path.Combine(pagesDir, fileName);

                if (File.Exists(filePath))
                    ExtractStrings(filePath, pageType, pageResults);

                _index[pageType] = pageResults;
                _allResults.AddRange(pageResults);
            }
        }

        private static void ExtractStrings(string filePath, Type pageType, List<SearchResult> results)
        {
            string xaml = File.ReadAllText(filePath);
            var matches = Regex.Matches(xaml, @"\{x:Static resources:Strings\.(\w+)\}");

            foreach (Match match in matches)
            {
                string resourceName = match.Groups[1].Value;
                string? text = GetResourceString(resourceName);
                if (string.IsNullOrWhiteSpace(text))
                    continue;

                if (!results.Any(r => r.Text == text))
                    results.Add(new SearchResult(text, pageType));
            }
        }

        private static string? GetResourceString(string resourceName)
        {
            try
            {
                var property = typeof(Bloxstrap.Resources.Strings).GetProperty(resourceName, BindingFlags.Public | BindingFlags.Static);
                return property?.GetValue(null)?.ToString();
            }
            catch
            {
                return null;
            }
        }

        public static Type? FindPage(string query)
        {
            string normalized = query.Trim().ToLowerInvariant();
            var match = _allResults.FirstOrDefault(r => r.Text.ToLowerInvariant() == normalized)
                ?? _allResults.FirstOrDefault(r => r.Text.ToLowerInvariant().Contains(normalized));
            return match?.PageType;
        }

        public class SearchResult
        {
            public string Text { get; }
            public Type PageType { get; }

            public SearchResult(string text, Type pageType)
            {
                Text = text;
                PageType = pageType;
            }

            public override string ToString() => Text;
        }
    }
}
