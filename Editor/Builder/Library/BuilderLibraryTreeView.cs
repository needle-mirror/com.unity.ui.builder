using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    class BuilderLibraryTreeView : BuilderLibraryView
    {
        const string k_TreeItemRootName = "root";
        const string k_TreeItemLabelName = "item-name-label";
        const string k_TreeItemEditorOnlyLabelName = "item-editor-only-label";
        const string k_TreeViewName = "library-tree-view";
        const string k_OpenButtonName = "item-open-button";

        const string k_TreeViewClassName = "unity-builder-library__tree-view";
        const string k_TreeViewItemWithButtonClassName = "unity-builder-library__tree-item-with-edit-button";

        readonly TreeView m_TreeView;
        readonly VisualTreeAsset m_TreeViewItemTemplate;

        public override VisualElement PrimaryFocusable => m_TreeView.Q<ListView>();

        public BuilderLibraryTreeView(IList<ITreeViewItem> items)
        {
            m_TreeViewItemTemplate = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(Path.Combine(BuilderConstants.LibraryUIPath, "BuilderLibraryTreeViewItem.uxml"));

            style.flexGrow = 1;
            m_TreeView = new TreeView { name = k_TreeViewName };
            m_TreeView.AddToClassList(k_TreeViewClassName);
            Add(m_TreeView);

            m_TreeView.viewDataKey = "samples-tree";
            m_TreeView.itemHeight = 20;
            m_TreeView.rootItems = items;
            m_TreeView.makeItem = MakeItem;
            m_TreeView.bindItem = BindItem;
#if UNITY_2020_1_OR_NEWER
            m_TreeView.onItemsChosen += OnItemsChosen;
#else
            m_TreeView.onItemChosen += (s) => OnItemChosen(s);
#endif
            m_TreeView.Refresh();

            foreach (var item in m_TreeView.rootItems)
                m_TreeView.ExpandItem(item.id);
        }

        void OnContextualMenuPopulateEvent(ContextualMenuPopulateEvent evt)
        {
            var libraryItem = GetLibraryTreeItem((VisualElement) evt.target);

            evt.menu.AppendAction(
                "Add",
                action => { AddItemToTheDocument(libraryItem); },
                action =>
                {
                    if (libraryItem.MakeVisualElementCallback == null)
                        return DropdownMenuAction.Status.Disabled;

                    if (libraryItem.SourceAsset == m_PaneWindow.document.visualTreeAsset)
                        return DropdownMenuAction.Status.Disabled;

                    return DropdownMenuAction.Status.Normal;
                });

            if (libraryItem.SourceAsset != null)
            {
                evt.menu.AppendAction(
                    "Open In UIBuilder",
                    action => { m_PaneWindow.LoadDocument(libraryItem.SourceAsset); },
                    action =>
                    {
                        if (libraryItem.SourceAsset == m_PaneWindow.document.visualTreeAsset)
                            return DropdownMenuAction.Status.Disabled;

                        return DropdownMenuAction.Status.Normal;
                    });

                evt.menu.AppendAction(
                    "Open with IDE",
                    action => {  AssetDatabase.OpenAsset(libraryItem.SourceAsset); },
                    action => DropdownMenuAction.Status.Normal);
            }
        }

        internal static CustomStyleProperty<int> s_DummyProperty = new CustomStyleProperty<int>("--my-dummy");

        VisualElement MakeItem()
        {
            var root = m_TreeViewItemTemplate.CloneTree().Q(k_TreeItemRootName);
            RegisterControlContainer(root);
            root.AddManipulator(new ContextualMenuManipulator(OnContextualMenuPopulateEvent));
            if (!EditorGUIUtility.isProSkin)
            {
                root.RegisterCustomBuilderStyleChangeEvent(builderElementStyle =>
                {
                    var libraryTreeItem = GetLibraryTreeItem(root);
                    if (libraryTreeItem == null)
                        return;

                    var libraryTreeItemIcon = libraryTreeItem.Icon;
                    if (builderElementStyle == BuilderElementStyle.Highlighted)
                        libraryTreeItemIcon = libraryTreeItem.DarkSkinIcon;

                    AssignTreeItemIcon(root, libraryTreeItemIcon);
                });
            }

            // Open button.
            var openButton = root.Q<Button>(k_OpenButtonName);
            openButton.AddToClassList(BuilderConstants.HiddenStyleClassName);
            openButton.clickable.clickedWithEventInfo += OnOpenButtonClick;

            return root;
        }

        void OnOpenButtonClick(EventBase evt)
        {
            var button = evt.target as Button;
            var item = button.userData as BuilderLibraryTreeItem;

            if (item?.SourceAsset == null)
                return;

            HidePreview();
            m_PaneWindow.LoadDocument(item.SourceAsset);
        }

        void BindItem(VisualElement element, ITreeViewItem item)
        {
            var builderItem = item as BuilderLibraryTreeItem;
            Assert.IsNotNull(builderItem);

            // Pre-emptive cleanup.
            var row = element.parent.parent;
            row.RemoveFromClassList(BuilderConstants.ExplorerHeaderRowClassName);
            row.SetEnabled(true);

            var isCurrentDocumentVisualTreeAsset = builderItem.SourceAsset == m_PaneWindow.document.visualTreeAsset;
            row.EnableInClassList(BuilderConstants.LibraryCurrentlyOpenFileItemClassName, isCurrentDocumentVisualTreeAsset);
            element.SetEnabled(!isCurrentDocumentVisualTreeAsset);

            // Header
            if (builderItem.IsHeader)
                row.AddToClassList(BuilderConstants.ExplorerHeaderRowClassName);

            var editorOnlyLabel = element.Q<Label>(k_TreeItemEditorOnlyLabelName);
            editorOnlyLabel.style.display = builderItem.IsEditorOnly ? DisplayStyle.Flex : DisplayStyle.None;

            // Set Icon
            AssignTreeItemIcon(element, builderItem.Icon);

            // Set label.
            var label = element.Q<Label>(k_TreeItemLabelName);
            Assert.IsNotNull(label);
            label.text = builderItem.data;

            // Set open button visibility.
            var openButton = element.Q<Button>(k_OpenButtonName);
            openButton.userData = item;
            var enableTreeViewItemWithButton = builderItem.SourceAsset != null && builderItem.SourceAsset != m_PaneWindow.document.visualTreeAsset;
            element.EnableInClassList(k_TreeViewItemWithButtonClassName, enableTreeViewItemWithButton);

            LinkToTreeViewItem(element, builderItem);
        }

#if UNITY_2020_1_OR_NEWER
        void OnItemsChosen(IEnumerable<ITreeViewItem> selectedItems)
#else
        void OnItemChosen(ITreeViewItem selectedItem)
#endif
        {
#if UNITY_2020_1_OR_NEWER
            var selectedItem = selectedItems.FirstOrDefault();
#endif

            var item = selectedItem as BuilderLibraryTreeItem;
            AddItemToTheDocument(item);
        }

        public override void Refresh() => m_TreeView.Refresh();

        void AssignTreeItemIcon(VisualElement itemRoot, Texture2D icon)
        {
            var iconElement = itemRoot.ElementAt(0);
            if (icon == null)
            {
                iconElement.style.display = DisplayStyle.None;
            }
            else
            {
                iconElement.style.display = DisplayStyle.Flex;
                var styleBackgroundImage = iconElement.style.backgroundImage;
                styleBackgroundImage.value = new Background { texture = icon };
                iconElement.style.backgroundImage = styleBackgroundImage;
            }
        }
    }
}
