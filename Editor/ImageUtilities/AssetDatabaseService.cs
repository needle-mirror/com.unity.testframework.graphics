using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine.TestTools.Graphics;

namespace UnityEditor.TestTools.Graphics
{
    /// <summary>
    /// Production implementation of <see cref="IAssetService"/> that delegates to <see cref="AssetDatabase"/>.
    /// </summary>
    class AssetDatabaseService : IAssetService
    {
        public T LoadAssetAtPath<T>(string path)
            where T : UnityEngine.Object => AssetDatabase.LoadAssetAtPath<T>(path);

        public UnityEngine.Object[] LoadAllAssetsAtPath(string path) =>
            AssetDatabase.LoadAllAssetsAtPath(path);

        public string GetAssetPath(UnityEngine.Object asset) => AssetDatabase.GetAssetPath(asset);

        public void CreateAsset(UnityEngine.Object asset, string path) =>
            AssetDatabase.CreateAsset(asset, path);

        public void AddObjectToAsset(UnityEngine.Object objectToAdd, UnityEngine.Object assetObject) =>
            AssetDatabase.AddObjectToAsset(objectToAdd, assetObject);

        public void SetDirty(UnityEngine.Object target) => EditorUtility.SetDirty(target);

        public bool DeleteAsset(string path) => AssetDatabase.DeleteAsset(path);

        public string MoveAsset(string sourcePath, string targetPath) =>
            AssetDatabase.MoveAsset(sourcePath, targetPath);

        public string CreateFolder(string parentFolder, string newFolderName) =>
            AssetDatabase.CreateFolder(parentFolder, newFolderName);

        public bool IsValidFolder(string path) => AssetDatabase.IsValidFolder(path);

        public void Refresh() => AssetDatabase.Refresh();

        public void SaveAssets() => AssetDatabase.SaveAssets();

        public void SaveAssetIfDirty(UnityEngine.Object asset) => AssetDatabase.SaveAssetIfDirty(asset);

        public void ImportAsset(string path) => AssetDatabase.ImportAsset(path);

        public string[] FindAssets(string filter, string[] searchInFolders) =>
            AssetDatabase.FindAssets(filter, searchInFolders);

        public string GuidToAssetPath(string guid) => AssetDatabase.GUIDToAssetPath(guid);

        public bool AssetPathExists(string path) => AssetDatabase.AssetPathExists(path);

        public void StartAssetEditing() => AssetDatabase.StartAssetEditing();

        public void StopAssetEditing() => AssetDatabase.StopAssetEditing();
    }

    /// <summary>
    /// Generic extension of <see cref="AssetDatabaseService"/> that adds typed loading,
    /// importer access, and async operations for a specific asset type.
    /// </summary>
    class AssetDatabaseAssetService<T> : AssetDatabaseService, IAssetService<T>
        where T : UnityEngine.Object
    {
        public IAssetImporter GetImporterAtPath(string path) =>
            AssetImporterWrapperFactory.GetImporterAtPath(AssetImporter.GetAtPath(path));

        public bool ContainsAsset(UnityEngine.Object asset) => AssetDatabase.Contains(asset);

        public bool TryLoadAssetAtPath(string path, out T asset)
        {
            asset = AssetDatabase.LoadAssetAtPath<T>(path);
            return asset != null;
        }

        public IEnumerable<string> FindAssets(string rootPath, string searchPattern)
        {
            var filter = $"{searchPattern} t:{typeof(T).Name}";
            var guids = AssetDatabase.FindAssets(filter, new[] { rootPath });
            var paths = new string[guids.Length];
            for (var i = 0; i < guids.Length; i++)
                paths[i] = AssetDatabase.GUIDToAssetPath(guids[i]);
            return paths;
        }

        public async Task RefreshAsync()
        {
            await MainThreadDispatcher.RunOnMainThread(Refresh);
        }

        public async Task<bool> IsValidFolderAsync(string path)
        {
            var isValidFolder = false;
            await MainThreadDispatcher.RunOnMainThread(() =>
            {
                isValidFolder = IsValidFolder(path);
            });

            return isValidFolder;
        }

        public async Task<bool> ContainsAssetAsync(UnityEngine.Object asset)
        {
            var containsAsset = false;
            await MainThreadDispatcher.RunOnMainThread(() =>
            {
                containsAsset = ContainsAsset(asset);
            });

            return containsAsset;
        }

        public async Task<IEnumerable<string>> FindAssetsAsync(string rootPath, string searchPattern)
        {
            IEnumerable<string> assets = null;
            await MainThreadDispatcher.RunOnMainThread(() =>
            {
                assets = FindAssets(rootPath, searchPattern);
            });

            return assets;
        }

        public async Task<T> LoadAssetAtPathAsync(string path)
        {
            T loadedAsset = null;
            await MainThreadDispatcher.RunOnMainThread(() =>
            {
                TryLoadAssetAtPath(path, out loadedAsset);
            });

            return loadedAsset;
        }

        public async Task<string> CreateFolderAsync(string parentFolder, string newFolderName)
        {
            if (Directory.Exists(Path.Combine(parentFolder, newFolderName)))
                return string.Empty;

            var guid = string.Empty;
            await MainThreadDispatcher.RunOnMainThread(() =>
            {
                guid = CreateFolder(parentFolder, newFolderName);
            });

            return guid;
        }

        public async Task<string> MoveAssetAsync(string sourcePath, string targetPath)
        {
            var error = string.Empty;
            await MainThreadDispatcher.RunOnMainThread(() =>
            {
                error = MoveAsset(sourcePath, targetPath);
            });

            return error;
        }

        public async Task<bool> DeleteAssetAsync(string path)
        {
            var success = false;
            await MainThreadDispatcher.RunOnMainThread(() =>
            {
                success = DeleteAsset(path);
            });

            return success;
        }
    }
}
