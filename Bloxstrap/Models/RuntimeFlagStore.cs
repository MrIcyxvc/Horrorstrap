using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace Bloxstrap.Models
{
    public static class RuntimeFlagStore
    {
        private const string FILE_NAME = "RuntimeFlags.json";

        private static string FilePath => Path.Combine(Paths.Base, FILE_NAME);

        public static Dictionary<string, object> Flags { get; private set; } = new();

        public static void Load()
        {
            try
            {
                if (!File.Exists(FilePath))
                    return;

                string json = File.ReadAllText(FilePath);
                var parsed = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);
                if (parsed is null)
                    return;

                Flags = parsed.ToDictionary(
                    kv => kv.Key,
                    kv =>
                    {
                        if (kv.Value.ValueKind == JsonValueKind.True) return true;
                        if (kv.Value.ValueKind == JsonValueKind.False) return false;
                        if (kv.Value.ValueKind == JsonValueKind.Number)
                        {
                            if (kv.Value.TryGetInt32(out int intValue)) return intValue;
                            if (kv.Value.TryGetInt64(out long longValue)) return longValue;
                        }
                        return (object)kv.Value.GetRawText().Trim('"');
                    });
            }
            catch (Exception ex)
            {
                App.Logger.WriteLine("RuntimeFlagStore", $"Failed to load: {ex.Message}");
            }
        }

        public static void Save()
        {
            try
            {
                Directory.CreateDirectory(Paths.Base);
                string json = JsonSerializer.Serialize(Flags, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(FilePath, json);
            }
            catch (Exception ex)
            {
                App.Logger.WriteLine("RuntimeFlagStore", $"Failed to save: {ex.Message}");
            }
        }

        public static void SetValue(string key, object value)
        {
            Flags[key] = value;
            Save();
        }

        public static void Remove(string key)
        {
            if (Flags.Remove(key))
                Save();
        }

        public static void Import(IEnumerable<KeyValuePair<string, object>> entries)
        {
            foreach (var kv in entries)
                Flags[kv.Key] = kv.Value;
            Save();
        }

        public static void Clear()
        {
            Flags.Clear();
            Save();
        }
    }
}
