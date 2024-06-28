﻿using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Celeste.Mod;
using Ionic.Zip;
using TAS.Module;
using TAS.Utils;

namespace TAS.EverestInterop;

public static class StudioHelper {
    private const string StudioName = "Celeste Studio";
    private static EverestModuleMetadata Metadata => CelesteTasModule.Instance.Metadata;
    private static string StudioNameWithExe => StudioName + ".exe";
    private static string ExtractedStudioExePath => Path.Combine(Everest.PathGame, StudioNameWithExe);
    private static string TempExtractPath => Path.Combine(Everest.PathGame, "TAS Files");
    private static string TempExtractStudio => Path.Combine(TempExtractPath, StudioNameWithExe);


    [Initialize]
    private static void Initialize() {
        try {
            // delete the useless file that makes Studio require.net472
            File.Delete(Path.Combine(Everest.PathGame, "Celeste Studio.exe.config"));
        } catch {
            // ignored
        }

        ExtractStudio(out bool studioProcessWasKilled);
        if (Environment.OSVersion.Platform == PlatformID.Win32NT) {
            LaunchStudioAtBoot(studioProcessWasKilled);
        }
    }

    private static void ExtractStudio(out bool studioProcessWasKilled) {
        studioProcessWasKilled = false;
        if (File.Exists(ExtractedStudioExePath) && !CheckNewerStudio()) {
            return;
        }

        try {
            if (Environment.OSVersion.Platform == PlatformID.Win32NT) {
                Process studioProcess = Process.GetProcesses().FirstOrDefault(process => process.ProcessName == StudioName);

                if (studioProcess != null) {
                    studioProcess.Kill();
                    studioProcess.WaitForExit(50000);
                }

                if (studioProcess?.HasExited == false) {
                    return;
                }

                if (studioProcess?.HasExited == true) {
                    studioProcessWasKilled = true;
                }
            }

            if (!string.IsNullOrEmpty(Metadata.PathArchive)) {
                using ZipFile zip = ZipFile.Read(Metadata.PathArchive);
                if (zip.EntryFileNames.Contains(StudioNameWithExe)) {
                    foreach (ZipEntry entry in zip.Entries) {
                        if (entry.FileName.StartsWith(StudioName)) {
                            entry.Extract(Everest.PathGame, ExtractExistingFileAction.OverwriteSilently);
                        }
                    }
                }
            } else if (!string.IsNullOrEmpty(Metadata.PathDirectory)) {
                string[] files = Directory.GetFiles(Metadata.PathDirectory);

                if (files.Any(filePath => filePath.EndsWith(StudioNameWithExe))) {
                    foreach (string sourceFile in files) {
                        string fileName = Path.GetFileName(sourceFile);
                        if (fileName.StartsWith(StudioName)) {
                            string destFile = Path.Combine(Everest.PathGame, fileName);
                            File.Copy(sourceFile, destFile, true);
                        }
                    }
                }
            }
        } catch (Exception e) {
            e.LogException("Failed to extract studio.");
            try {
                foreach (string file in Directory.GetFiles(Everest.PathGame, "*.PendingOverwrite")) {
                    File.Delete(file);
                }
            } catch {
                // ignore
            }
        }
    }

    private static bool CheckNewerStudio() {
        try {
            string modStudioVersion = null;

            if (!string.IsNullOrEmpty(Metadata.PathArchive)) {
                using ZipFile zip = ZipFile.Read(Metadata.PathArchive);
                if (zip.Entries.FirstOrDefault(zipEntry => zipEntry.FileName == StudioNameWithExe) is { } studioZipEntry) {
                    studioZipEntry.Extract(TempExtractPath, ExtractExistingFileAction.OverwriteSilently);
                    modStudioVersion = FileVersionInfo.GetVersionInfo(TempExtractStudio).FileVersion;
                    File.Delete(TempExtractStudio);
                }
            } else if (!string.IsNullOrEmpty(Metadata.PathDirectory)) {
                string[] files = Directory.GetFiles(Metadata.PathDirectory);
                if (files.FirstOrDefault(filePath => filePath.EndsWith(StudioNameWithExe)) is { } studioFilePath) {
                    modStudioVersion = FileVersionInfo.GetVersionInfo(studioFilePath).FileVersion;
                }
            }

            string existStudioVersion = FileVersionInfo.GetVersionInfo(ExtractedStudioExePath).FileVersion;
            return existStudioVersion != modStudioVersion;
        } catch (Exception e) {
            e.LogException("Failed to check studio version.");
            try {
                foreach (string file in Directory.GetFiles(TempExtractPath, "*.PendingOverwrite")) {
                    File.Delete(file);
                }
            } catch {
                // ignore
            }

            return false;
        }
    }

    private static void LaunchStudioAtBoot(bool studioProcessWasKilled) {
        if (TasSettings.Enabled && TasSettings.LaunchStudioAtBoot || studioProcessWasKilled) {
            try {
                Process[] processes = Process.GetProcesses();
                if (processes.Any(process => process.ProcessName == StudioName)) {
                    return;
                }

                // detach studio from steam
                if (File.Exists(ExtractedStudioExePath)) {
                    Process.Start("Explorer", ExtractedStudioExePath);
                }
            } catch (Exception e) {
                e.LogException("Failed to launch studio at boot.");
            }
        }
    }
}