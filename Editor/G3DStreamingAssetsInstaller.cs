using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace G3D.Editor
{
    [InitializeOnLoad]
    public static class G3DStreamingAssetsInstaller
    {
        private const string SourceSuffix = "/Runtime/HeadTrackingLib/G3DHTService";
        private const string DestinationRelativePath = "Assets/StreamingAssets/G3DHTService";
        private const string VersionMarkerFileName = "g3d_plugin_version.txt";

        private static bool s_IsCopyInProgress;

        static G3DStreamingAssetsInstaller()
        {
            EditorApplication.delayCall += EnsureInstalled;
        }

        [MenuItem("Tools/G3D/Install HeadTracking Service To StreamingAssets")]
        public static void EnsureInstalledWithLog()
        {
            Debug.Log("[G3D] Ensuring G3DHTService is installed to StreamingAssets...");
            EnsureInstalled();
        }

        public static void EnsureInstalled()
        {
            if (s_IsCopyInProgress)
                return;

            string sourceAssetPath = FindSourceAssetPath();
            if (string.IsNullOrEmpty(sourceAssetPath))
                return;

            try
            {
                s_IsCopyInProgress = true;

                EnsureFolderExists("Assets/StreamingAssets");

                string projectRoot = GetProjectRoot();
                string sourceAbsolutePath = AssetPathToAbsolutePath(sourceAssetPath, projectRoot);
                string destinationAbsolutePath = AssetPathToAbsolutePath(
                    DestinationRelativePath,
                    projectRoot
                );

                if (!Directory.Exists(sourceAbsolutePath))
                {
                    Debug.LogWarning(
                        "[G3D] Head tracking source directory does not exist: " + sourceAbsolutePath
                    );
                    return;
                }

                string pluginVersion = GetPluginVersionFromPackageJson(
                    sourceAssetPath,
                    projectRoot
                );
                string installedVersion = GetInstalledVersion(destinationAbsolutePath);
                bool overwriteExistingFiles =
                    !string.IsNullOrEmpty(pluginVersion)
                    && !string.Equals(pluginVersion, installedVersion, StringComparison.Ordinal);

                int copiedFileCount = CopyDirectoryRecursive(
                    sourceAbsolutePath,
                    destinationAbsolutePath,
                    overwriteExistingFiles
                );
                bool versionMarkerUpdated = WriteInstalledVersion(
                    destinationAbsolutePath,
                    pluginVersion
                );

                if (copiedFileCount > 0)
                {
                    AssetDatabase.Refresh();
                    Debug.Log("[G3D] Installed G3DHTService to " + DestinationRelativePath);
                }
                else if (versionMarkerUpdated)
                {
                    AssetDatabase.Refresh();
                }
            }
            catch (Exception ex)
            {
                Debug.LogError("[G3D] Failed to install G3DHTService to StreamingAssets: " + ex);
            }
            finally
            {
                s_IsCopyInProgress = false;
            }
        }

        private static string FindSourceAssetPath()
        {
            string[] roots = { "Assets", "Packages" };

            foreach (string root in roots)
            {
                if (!Directory.Exists(AssetPathToAbsolutePath(root, GetProjectRoot())))
                    continue;

                string[] guids = AssetDatabase.FindAssets("G3DHTService", new[] { root });
                for (int i = 0; i < guids.Length; i++)
                {
                    string assetPath = AssetDatabase.GUIDToAssetPath(guids[i]).Replace('\\', '/');
                    if (!assetPath.EndsWith(SourceSuffix, StringComparison.OrdinalIgnoreCase))
                        continue;

                    string candidateExe = AssetPathToAbsolutePath(
                        assetPath + "/G3DHTService.exe",
                        GetProjectRoot()
                    );
                    if (File.Exists(candidateExe))
                        return assetPath;
                }
            }

            return string.Empty;
        }

        private static string GetProjectRoot()
        {
            return Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        }

        private static string AssetPathToAbsolutePath(string assetPath, string projectRoot)
        {
            string normalizedAssetPath = assetPath.Replace('\\', '/');
            return Path.GetFullPath(Path.Combine(projectRoot, normalizedAssetPath));
        }

        private static void EnsureFolderExists(string assetFolderPath)
        {
            if (AssetDatabase.IsValidFolder(assetFolderPath))
                return;

            string[] parts = assetFolderPath.Split('/');
            if (parts.Length == 0 || parts[0] != "Assets")
                throw new InvalidOperationException(
                    "Folder path must start with Assets: " + assetFolderPath
                );

            string current = "Assets";
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }

        private static int CopyDirectoryRecursive(
            string sourceDir,
            string destinationDir,
            bool overwriteExistingFiles
        )
        {
            Directory.CreateDirectory(destinationDir);

            int copiedFiles = 0;

            foreach (string sourceFile in Directory.GetFiles(sourceDir))
            {
                if (
                    string.Equals(
                        Path.GetExtension(sourceFile),
                        ".meta",
                        StringComparison.OrdinalIgnoreCase
                    )
                )
                    continue;

                string fileName = Path.GetFileName(sourceFile);
                string destinationFile = Path.Combine(destinationDir, fileName);
                bool destinationExists = File.Exists(destinationFile);
                if (destinationExists && !overwriteExistingFiles)
                    continue;

                File.Copy(sourceFile, destinationFile, overwriteExistingFiles && destinationExists);

                copiedFiles++;
            }

            foreach (string sourceSubdirectory in Directory.GetDirectories(sourceDir))
            {
                string directoryName = Path.GetFileName(sourceSubdirectory);
                string destinationSubdirectory = Path.Combine(destinationDir, directoryName);
                copiedFiles += CopyDirectoryRecursive(
                    sourceSubdirectory,
                    destinationSubdirectory,
                    overwriteExistingFiles
                );
            }

            return copiedFiles;
        }

        private static string GetInstalledVersion(string destinationAbsolutePath)
        {
            string versionMarkerPath = Path.Combine(destinationAbsolutePath, VersionMarkerFileName);
            if (!File.Exists(versionMarkerPath))
                return string.Empty;

            return File.ReadAllText(versionMarkerPath).Trim();
        }

        private static bool WriteInstalledVersion(
            string destinationAbsolutePath,
            string pluginVersion
        )
        {
            if (string.IsNullOrEmpty(pluginVersion))
                return false;

            Directory.CreateDirectory(destinationAbsolutePath);

            string versionMarkerPath = Path.Combine(destinationAbsolutePath, VersionMarkerFileName);
            string currentValue = File.Exists(versionMarkerPath)
                ? File.ReadAllText(versionMarkerPath).Trim()
                : string.Empty;

            if (string.Equals(currentValue, pluginVersion, StringComparison.Ordinal))
                return false;

            File.WriteAllText(versionMarkerPath, pluginVersion + Environment.NewLine);
            return true;
        }

        private static string GetPluginVersionFromPackageJson(
            string sourceAssetPath,
            string projectRoot
        )
        {
            int sourceSuffixStart = sourceAssetPath.LastIndexOf(
                SourceSuffix,
                StringComparison.OrdinalIgnoreCase
            );
            if (sourceSuffixStart < 0)
                return string.Empty;

            string pluginRootAssetPath = sourceAssetPath.Substring(0, sourceSuffixStart);
            if (string.IsNullOrEmpty(pluginRootAssetPath))
                return string.Empty;

            string packageJsonAbsolutePath = AssetPathToAbsolutePath(
                pluginRootAssetPath + "/package.json",
                projectRoot
            );
            if (!File.Exists(packageJsonAbsolutePath))
                return string.Empty;

            string json = File.ReadAllText(packageJsonAbsolutePath);
            PackageInfo packageInfo = JsonUtility.FromJson<PackageInfo>(json);
            return packageInfo == null || string.IsNullOrEmpty(packageInfo.version)
                ? string.Empty
                : packageInfo.version.Trim();
        }

        [Serializable]
        private sealed class PackageInfo
        {
            public string version;
        }

        private sealed class ImportWatcher : AssetPostprocessor
        {
            private static void OnPostprocessAllAssets(
                string[] importedAssets,
                string[] deletedAssets,
                string[] movedAssets,
                string[] movedFromAssetPaths
            )
            {
                if (s_IsCopyInProgress)
                    return;

                if (
                    ContainsServiceSourceChange(importedAssets)
                    || ContainsServiceSourceChange(movedAssets)
                )
                    EditorApplication.delayCall += EnsureInstalled;
            }

            private static bool ContainsServiceSourceChange(IEnumerable<string> paths)
            {
                foreach (string path in paths)
                {
                    string normalized = path.Replace('\\', '/');
                    if (
                        normalized.IndexOf(
                            "/Runtime/HeadTrackingLib/G3DHTService",
                            StringComparison.OrdinalIgnoreCase
                        ) >= 0
                    )
                        return true;
                }
                return false;
            }
        }
    }
}
