using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Bloxstrap.Integrations
{
    public static class RuntimeFlagInjector
    {
        private const string LOG_IDENT = "RuntimeFlagInjector::Inject";
        private const string OFFSETS_BASE_URL = "https://offsets.imtheo.lol";
        private const string FALLBACK_VERSION_GUID = "version-d584fb6c717a43d9";
        private const int PROCESS_OPEN_RETRIES = 3;
        private const int PROCESS_OPEN_RETRY_DELAY_MS = 100;
        private const int INJECTION_TIMEOUT_SECONDS = 15;

        #region Native Methods

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr OpenProcess(ProcessAccessFlags dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr hObject);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, [Out] byte[] lpBuffer, int dwSize, out int lpNumberOfBytesRead);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, int dwSize, out int lpNumberOfBytesWritten);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool VirtualProtectEx(IntPtr hProcess, IntPtr lpAddress, int dwSize, uint flNewProtect, out uint lpflOldProtect);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern int GetLastError();

        [DllImport("ntdll.dll", SetLastError = true)]
        private static extern int NtWriteVirtualMemory(IntPtr processHandle, IntPtr baseAddress, byte[] buffer, int bufferSize, out int bytesWritten);

        [Flags]
        private enum ProcessAccessFlags : uint
        {
            VirtualMemoryOperation = 0x00000008,
            VirtualMemoryRead = 0x00000010,
            VirtualMemoryWrite = 0x00000020,
            QueryInformation = 0x00000400
        }

        private const uint PAGE_EXECUTE_READWRITE = 0x40;

        #endregion

        private class FlagOffset
        {
            public string Name { get; set; } = "";
            public IntPtr Offset { get; set; }
        }

        private class PatchResult
        {
            public int TotalFlags { get; set; }
            public int PatchedCount { get; set; }
            public int SkippedCount { get; set; }
            public int FailedCount { get; set; }
            public int UnsupportedCount { get; set; }
            public int ProtectedCount { get; set; }
        }

        public static void Inject(int processId)
        {
            if (!App.Settings.Prop.EnableRuntimeFlagInjector)
            {
                App.Logger.WriteLine(LOG_IDENT, "Runtime flag injector is disabled in settings.");
                return;
            }

            if (!Utilities.IsAdmin())
            {
                App.Logger.WriteLine(LOG_IDENT, "Runtime flag injection skipped: Horrorstrap is not running as administrator.");
                return;
            }

            // Run on a thread pool thread with a timeout so the launch flow is never blocked.
            _ = Task.Run(async () =>
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(INJECTION_TIMEOUT_SECONDS));
                var cancellationToken = cts.Token;

                try
                {
                    App.Logger.WriteLine(LOG_IDENT, $"Runtime flag injection is enabled for process {processId} (administrator).");

                    // Get flag offsets
                    var flagOffsets = await FetchFlagOffsetsAsync(cancellationToken);

                    if (flagOffsets == null || flagOffsets.Count == 0)
                    {
                        App.Logger.WriteLine(LOG_IDENT, "No flag offsets available for injection.");
                        return;
                    }

                    App.Logger.WriteLine(LOG_IDENT, $"Parsed {flagOffsets.Count} flag offsets.");

                    // Open process with retry
                    IntPtr processHandle = OpenProcessWithRetry(processId);
                    if (processHandle == IntPtr.Zero)
                    {
                        int err = GetLastError();
                        App.Logger.WriteLine(LOG_IDENT, $"Failed to open Roblox process {processId} after retries. Win32 error: {err}");
                        return;
                    }

                    try
                    {
                        // Get process module information
                        Process process;
                        try
                        {
                            process = Process.GetProcessById(processId);
                        }
                        catch (ArgumentException)
                        {
                            App.Logger.WriteLine(LOG_IDENT, $"Roblox process {processId} has already exited.");
                            return;
                        }

                        ProcessModule? mainModule = process.MainModule;
                        if (mainModule == null)
                        {
                            App.Logger.WriteLine(LOG_IDENT, "Could not get Roblox main module.");
                            return;
                        }

                        IntPtr baseAddress = mainModule.BaseAddress;
                        App.Logger.WriteLine(LOG_IDENT, $"Roblox base address: 0x{baseAddress.ToInt64():X}");

                        var result = await Task.Run(() => InjectRuntimeFlags(
                            processHandle,
                            baseAddress,
                            mainModule.ModuleMemorySize,
                            flagOffsets),
                            cancellationToken);

                        App.Logger.WriteLine(LOG_IDENT,
                            $"Patch results: {result.PatchedCount}/{result.TotalFlags} patched, " +
                            $"{result.SkippedCount} skipped (out of range), " +
                            $"{result.UnsupportedCount} unsupported (string/etc), " +
                            $"{result.FailedCount} failed, " +
                            $"{result.ProtectedCount} memory pages protected.");
                    }
                    finally
                    {
                        CloseHandle(processHandle);
                    }
                }
                catch (OperationCanceledException)
                {
                    App.Logger.WriteLine(LOG_IDENT, $"Runtime flag injection timed out after {INJECTION_TIMEOUT_SECONDS} seconds.");
                }
                catch (Exception ex)
                {
                    App.Logger.WriteLine(LOG_IDENT, "Runtime flag injection failed.");
                    App.Logger.WriteException(LOG_IDENT, ex);
                }
            });
        }

        private static async Task<List<FlagOffset>> FetchFlagOffsetsAsync(CancellationToken cancellationToken)
        {
            try
            {
                string offsetsUrl = GetOffsetsUrl();
                App.Logger.WriteLine(LOG_IDENT, $"Fetching offsets from {offsetsUrl}");

                string offsetsContent = await App.HttpClient
                    .GetStringAsync(offsetsUrl, cancellationToken)
                    .ConfigureAwait(false);

                return ParseFlagOffsets(offsetsContent);
            }
            catch (HttpRequestException ex)
            {
                App.Logger.WriteLine(LOG_IDENT, $"HTTP request failed: {ex.Message}");
                App.Logger.WriteException(LOG_IDENT, ex);
                return new List<FlagOffset>();
            }
            catch (OperationCanceledException)
            {
                throw; // Re-throw to be handled by caller
            }
            catch (Exception ex)
            {
                App.Logger.WriteLine(LOG_IDENT, $"Failed to fetch or parse offsets: {ex.Message}");
                App.Logger.WriteException(LOG_IDENT, ex);
                return new List<FlagOffset>();
            }
        }

        private static string GetOffsetsUrl()
        {
            // Get the version GUID from Roblox state
            string versionGuid = App.RobloxState.Prop.Player.VersionGuid;

            // Use fallback if no version is available
            if (string.IsNullOrEmpty(versionGuid))
            {
                App.Logger.WriteLine(LOG_IDENT, "No version GUID found, using fallback.");
                versionGuid = FALLBACK_VERSION_GUID;
            }

            // Construct URL: https://offsets.imtheo.lol/[version-guid]/fflags.hpp
            return $"{OFFSETS_BASE_URL}/{versionGuid}/fflags.hpp";
        }

        private static List<FlagOffset> ParseFlagOffsets(string content)
        {
            var offsets = new List<FlagOffset>();

            var regex = new Regex(
                @"inline constexpr uintptr_t\s+([A-Za-z_][A-Za-z0-9_]*)\s*=\s*(0x[0-9A-Fa-f]+)",
                RegexOptions.Compiled | RegexOptions.IgnoreCase,
                TimeSpan.FromSeconds(1));

            try
            {
                var matches = regex.Matches(content);
                App.Logger.WriteLine(LOG_IDENT, $"Found {matches.Count} potential flag definitions.");

                foreach (Match match in matches)
                {
                    if (match.Groups.Count < 3)
                        continue;

                    string name = match.Groups[1].Value;
                    string hexValue = match.Groups[2].Value;

                    if (ulong.TryParse(hexValue.AsSpan(2),
                        System.Globalization.NumberStyles.HexNumber,
                        null,
                        out ulong offsetValue))
                    {
                        offsets.Add(new FlagOffset
                        {
                            Name = name,
                            Offset = (IntPtr)offsetValue
                        });
                    }
                    else
                    {
                        App.Logger.WriteLine(LOG_IDENT, $"Failed to parse hex value for flag '{name}': {hexValue}");
                    }
                }
            }
            catch (RegexMatchTimeoutException)
            {
                App.Logger.WriteLine(LOG_IDENT, "Regex timeout while parsing flag offsets.");
            }
            catch (Exception ex)
            {
                App.Logger.WriteLine(LOG_IDENT, $"Error parsing flag offsets: {ex.Message}");
                App.Logger.WriteException(LOG_IDENT, ex);
            }

            return offsets;
        }

        private static string CleanFlagName(string raw)
        {
            string[] prefixes = { "FFlag", "DFFlag", "FInt", "DFInt", "FString", "DFString", "FLog" };
            foreach (var prefix in prefixes)
            {
                if (raw.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    return raw.Substring(prefix.Length);
            }
            return raw;
        }

        private static IntPtr OpenProcessWithRetry(int processId)
        {
            ProcessAccessFlags desiredAccess =
                ProcessAccessFlags.VirtualMemoryOperation |
                ProcessAccessFlags.VirtualMemoryRead |
                ProcessAccessFlags.VirtualMemoryWrite |
                ProcessAccessFlags.QueryInformation;

            for (int i = 0; i < PROCESS_OPEN_RETRIES; i++)
            {
                IntPtr handle = OpenProcess(desiredAccess, false, processId);
                if (handle != IntPtr.Zero)
                {
                    if (i > 0)
                    {
                        App.Logger.WriteLine(LOG_IDENT, $"Successfully opened process after {i + 1} attempts.");
                    }
                    return handle;
                }

                if (i < PROCESS_OPEN_RETRIES - 1)
                {
                    App.Logger.WriteLine(LOG_IDENT, $"Failed to open process (attempt {i + 1}/{PROCESS_OPEN_RETRIES}), retrying...");
                    Thread.Sleep(PROCESS_OPEN_RETRY_DELAY_MS);
                }
            }

            return IntPtr.Zero;
        }

        /// <summary>
        /// Batched injection method - groups consecutive offsets for better performance
        /// </summary>
        private static PatchResult InjectRuntimeFlags(
            IntPtr processHandle,
            IntPtr baseAddress,
            int moduleSize,
            List<FlagOffset> flagOffsets)
        {
            var result = new PatchResult
            {
                TotalFlags = Models.RuntimeFlagStore.Flags.Count
            };

            var offsetLookup = flagOffsets
                .ToDictionary(f => CleanFlagName(f.Name), f => f.Offset, StringComparer.OrdinalIgnoreCase);

            foreach (var kv in Models.RuntimeFlagStore.Flags)
            {
                string cleanName = CleanFlagName(kv.Key);
                if (!offsetLookup.TryGetValue(cleanName, out IntPtr offset))
                {
                    App.Logger.WriteLine(LOG_IDENT, $"No offset found for flag '{kv.Key}'");
                    result.FailedCount++;
                    continue;
                }

                long relativeOffset = offset.ToInt64() - baseAddress.ToInt64();
                if (relativeOffset < 0 || relativeOffset > moduleSize)
                {
                    App.Logger.WriteLine(LOG_IDENT, $"Flag '{kv.Key}' offset is outside module range.");
                    result.SkippedCount++;
                    continue;
                }

                if (!TryConvertToInt32(kv.Value, out int value))
                {
                    App.Logger.WriteLine(LOG_IDENT, $"Flag '{kv.Key}' has unsupported value type '{kv.Value?.GetType().Name}'. Only bool/int values can be injected.");
                    result.UnsupportedCount++;
                    continue;
                }

                IntPtr targetAddress = IntPtr.Add(baseAddress, (int)relativeOffset);
                byte[] bytes = BitConverter.GetBytes(value);

                try
                {
                    if (!VirtualProtectEx(processHandle, targetAddress, bytes.Length, PAGE_EXECUTE_READWRITE, out uint oldProtect))
                    {
                        int err = GetLastError();
                        App.Logger.WriteLine(LOG_IDENT, $"Failed to change protection for flag '{kv.Key}'. Error: {err}");
                        result.FailedCount++;
                        continue;
                    }

                    result.ProtectedCount++;

                    int status = NtWriteVirtualMemory(processHandle, targetAddress, bytes, bytes.Length, out int written);
                    if (status == 0 && written == bytes.Length)
                    {
                        result.PatchedCount++;
                        App.Logger.WriteLine(LOG_IDENT, $"Injected flag '{kv.Key}' = {value} at 0x{relativeOffset:X}");
                    }
                    else
                    {
                        App.Logger.WriteLine(LOG_IDENT, $"NtWriteVirtualMemory failed for flag '{kv.Key}' (status={status}, written={written}).");
                        result.FailedCount++;
                    }

                    if (!VirtualProtectEx(processHandle, targetAddress, bytes.Length, oldProtect, out _))
                    {
                        App.Logger.WriteLine(LOG_IDENT, $"Warning: Failed to restore protection for flag '{kv.Key}'.");
                    }
                }
                catch (Exception ex)
                {
                    App.Logger.WriteLine(LOG_IDENT, $"Exception while injecting flag '{kv.Key}': {ex.Message}");
                    result.FailedCount++;
                }
            }

            return result;
        }

        private static bool TryConvertToInt32(object? value, out int result)
        {
            result = 0;
            if (value is null)
                return false;

            switch (value)
            {
                case bool b:
                    result = b ? 1 : 0;
                    return true;
                case int i:
                    result = i;
                    return true;
                case long l:
                    if (l > int.MaxValue || l < int.MinValue)
                        return false;
                    result = (int)l;
                    return true;
                case double d:
                    result = (int)d;
                    return true;
                case string s:
                    if (bool.TryParse(s, out bool sb))
                    {
                        result = sb ? 1 : 0;
                        return true;
                    }
                    if (int.TryParse(s, out int si))
                    {
                        result = si;
                        return true;
                    }
                    return false;
                default:
                    return false;
            }
        }

            }
        }