using System;
using System.IO;
using Celeste.Mod;
using TAS.Module;
using TAS.Utils;

#if DEBUG
namespace TAS.EverestInterop;

public static class HotReloadHelper {
    private static FileSystemWatcher watcher;

    [Load]
    private static void Load() {
        EverestModuleMetadata meta = CelesteTasModule.Instance.Metadata;
        if (meta.PathDirectory.IsNullOrEmpty()) {
            return;
        }

        try {
            watcher = new FileSystemWatcher {
                Path = Path.GetDirectoryName(meta.DLL),
                NotifyFilter = NotifyFilters.LastWrite,
            };

            watcher.Changed += (s, e) => {
                if (e.FullPath == meta.DLL && Manager.Running) {
                    Manager.DisableRun();
                }
            };

            watcher.EnableRaisingEvents = true;
        } catch (Exception e) {
            e.LogException($"Failed watching folder: {Path.GetDirectoryName(meta.DLL)}");
            Unload();
        }
    }

    [Unload]
    private static void Unload() {
        watcher?.Dispose();
        watcher = null;
    }
}
#endif