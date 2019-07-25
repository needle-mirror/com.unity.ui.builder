using System;
using UnityEditor;
using UnityEngine;

namespace Unity.UI.Builder
{
    [InitializeOnLoad]
    internal class BuilderAssetModificationProcessor : UnityEditor.AssetModificationProcessor
    {
        public static Action OnAssetChange { get; set; }

        static bool IsUxml(string assetPath)
        {
            if (assetPath.EndsWith("uxml") || assetPath.EndsWith("uxml.meta"))
                return true;

            return false;
        }

        static void OnWillCreateAsset(string assetPath)
        {
            if (IsUxml(assetPath))
                OnAssetChange?.Invoke();
        }

        static AssetDeleteResult OnWillDeleteAsset(string assetPath, RemoveAssetOptions option)
        {
            if (IsUxml(assetPath))
                OnAssetChange?.Invoke();

            return AssetDeleteResult.DidNotDelete;
        }

        private static AssetMoveResult OnWillMoveAsset(string sourcePath, string destinationPath)
        {
            if (IsUxml(sourcePath) || IsUxml(destinationPath))
                OnAssetChange?.Invoke();

            return AssetMoveResult.DidNotMove;
        }

        static string[] OnWillSaveAssets(string[] paths)
        {
            // On a duplication, this function is called with ZERO
            // paths. Because, of course.
            OnAssetChange?.Invoke();

            return paths;
        }
    }
}