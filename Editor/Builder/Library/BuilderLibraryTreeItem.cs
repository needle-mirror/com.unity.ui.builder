using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    class BuilderLibraryTreeItem : TreeViewItem<string>
    {
        public string name => data;
        public Type type { get; }
        public bool isHeader { get; set; }
        public bool hasPreview { get; set; }
        public VisualTreeAsset sourceAsset { get; }
        public string sourceAssetPath { get; }
        public Func<VisualElement> makeVisualElementCallback { get; }
        public Func<VisualTreeAsset, VisualElementAsset, VisualElement, VisualElementAsset> makeElementAssetCallback { get; }
        public Texture2D icon { get; private set; }
        public Texture2D largeIcon { get; private set; }
        public bool isEditorOnly { get; set; }
        public Texture2D darkSkinIcon { get; private set; }
        public Texture2D lightSkinIcon { get; private set; }
        public Texture2D darkSkinLargeIcon { get; private set; }
        public Texture2D lightSkinLargeIcon { get; private set; }

        public BuilderLibraryTreeItem(
            string name, string iconName, Type type, Func<VisualElement> makeVisualElementCallback,
            Func<VisualTreeAsset, VisualElementAsset, VisualElement, VisualElementAsset> makeElementAssetCallback = null,
            List<TreeViewItem<string>> children = null, VisualTreeAsset asset = null, int id = default)
            : base(GetItemId(name, type, asset, id) , name, children)
        {
            this.makeVisualElementCallback = makeVisualElementCallback;
            this.makeElementAssetCallback = makeElementAssetCallback;
            sourceAsset = asset;
            if (sourceAsset != null)
                sourceAssetPath = AssetDatabase.GetAssetPath(sourceAsset);

            this.type = type;
            if (!string.IsNullOrEmpty(iconName))
            {
                AssignIcon(iconName);
                if (icon == null)
                    AssignIcon("VisualElement");
            }
        }

        static int GetItemId(string name, Type type, VisualTreeAsset asset, int id)
        {
            if (id != default)
                return id;

            if (asset != null)
                return AssetDatabase.GetAssetPath(asset).GetHashCode();

            return (name + type?.FullName).GetHashCode();
        }

        void AssignIcon(string iconName)
        {
            var darkSkinResourceBasePath = $"{BuilderConstants.IconsResourcesPath}/Dark/Library/";
            var lightSkinResourceBasePath = $"{BuilderConstants.IconsResourcesPath}/Light/Library/";

            darkSkinLargeIcon = LoadLargeIcon(darkSkinResourceBasePath, iconName);
            lightSkinLargeIcon = LoadLargeIcon(lightSkinResourceBasePath, iconName);

            darkSkinIcon = LoadIcon(darkSkinResourceBasePath, iconName);
            lightSkinIcon = LoadIcon(lightSkinResourceBasePath, iconName);

            if (EditorGUIUtility.isProSkin)
            {
                icon = darkSkinIcon;
                largeIcon = darkSkinLargeIcon;
            }
            else
            {
                icon = lightSkinIcon;
                largeIcon = lightSkinLargeIcon;
            }
        }

        Texture2D LoadIcon(string resourceBasePath, string iconName)
        {
#if UI_BUILDER_PACKAGE
            return Resources.Load<Texture2D>(EditorGUIUtility.pixelsPerPoint > 1
                ? $"{resourceBasePath}{iconName}@2x"
                : $"{resourceBasePath}{iconName}");
#else
            return EditorGUIUtility.Load(EditorGUIUtility.pixelsPerPoint > 1
                ? $"{resourceBasePath}{iconName}@2x.png"
                : $"{resourceBasePath}{iconName}.png") as Texture2D;
#endif
        }

        Texture2D LoadLargeIcon(string resourceBasePath, string iconName)
        {
#if UI_BUILDER_PACKAGE
            return Resources.Load<Texture2D>(EditorGUIUtility.pixelsPerPoint > 1
                ? $"{resourceBasePath}{iconName}@8x"
                : $"{resourceBasePath}{iconName}@4x");
#else
            return EditorGUIUtility.Load(EditorGUIUtility.pixelsPerPoint > 1
                ? $"{resourceBasePath}{iconName}@8x.png"
                : $"{resourceBasePath}{iconName}@4x.png") as Texture2D;
#endif
        }

        public void SetIcon(Texture2D icon)
        {
            this.icon = icon;
            largeIcon = icon;
            darkSkinIcon = icon;
            lightSkinIcon = icon;
        }
    }
}
