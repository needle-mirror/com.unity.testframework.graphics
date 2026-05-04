using UnityEngine;

namespace UnityEditor.TestTools.Graphics.Filtering
{
    [System.Serializable]
#if GRAPHICS_TEST_FRAMEWORK_DEBUG
    [CreateAssetMenu(fileName = "TestFilters", menuName = "Graphics Test Framework/Test Filters")]
#endif
    class TestFilters : ScriptableObject
    {
        internal IAssetService AssetService { get; set; } = new AssetDatabaseService();

        [SerializeField]
        internal TestFilterConfig[] filters;

        void OnEnable()
        {
            Debug.LogWarning(
                $"The Test Filters Asset workflow is deprecated. Please use the IgnoreGraphicsTest attribute instead.\nYou can convert the asset into test filter by clicking the button in the <a href=\"{AssetService.GetAssetPath(this)}\">Test Filters Asset Editor</a>. "
            );
        }
    }
}
