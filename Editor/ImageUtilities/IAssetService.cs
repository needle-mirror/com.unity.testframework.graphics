using System.Collections.Generic;
using System.Threading.Tasks;

namespace UnityEditor.TestTools.Graphics
{
    /// <summary>
    /// Non-generic abstraction over <see cref="AssetDatabase"/> for testability.
    /// </summary>
    interface IAssetService
    {
        T LoadAssetAtPath<T>(string path)
            where T : UnityEngine.Object;
        UnityEngine.Object[] LoadAllAssetsAtPath(string path);
        string GetAssetPath(UnityEngine.Object asset);
        void CreateAsset(UnityEngine.Object asset, string path);
        void AddObjectToAsset(UnityEngine.Object objectToAdd, UnityEngine.Object assetObject);
        void SetDirty(UnityEngine.Object target);
        bool DeleteAsset(string path);
        string MoveAsset(string sourcePath, string targetPath);
        string CreateFolder(string parentFolder, string newFolderName);
        bool IsValidFolder(string path);
        void Refresh();
        void SaveAssets();
        void ImportAsset(string path);
        string[] FindAssets(string filter, string[] searchInFolders);
        string GuidToAssetPath(string guid);
        bool AssetPathExists(string path);
        void StartAssetEditing();
        void StopAssetEditing();
    }

    /// <summary>
    /// Generic extension of <see cref="IAssetService"/> that adds typed loading,
    /// importer access, and async operations for a specific asset type.
    /// </summary>
    interface IAssetService<T> : IAssetService
        where T : UnityEngine.Object
    {
        IAssetImporter GetImporterAtPath(string path);
        bool ContainsAsset(UnityEngine.Object asset);
        bool TryLoadAssetAtPath(string path, out T asset);
        IEnumerable<string> FindAssets(string rootPath, string searchPattern);

        Task RefreshAsync();
        Task<bool> IsValidFolderAsync(string path);
        Task<bool> ContainsAssetAsync(UnityEngine.Object asset);
        Task<IEnumerable<string>> FindAssetsAsync(string rootPath, string searchPattern);
        Task<T> LoadAssetAtPathAsync(string path);
        Task<string> CreateFolderAsync(string parentFolder, string newFolderName);
        Task<string> MoveAssetAsync(string sourcePath, string targetPath);
        Task<bool> DeleteAssetAsync(string path);
    }
}
