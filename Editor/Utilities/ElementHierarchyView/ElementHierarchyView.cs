using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    internal class ElementHierarchyView : VisualElement
    {
        public bool hierarchyHasChanged { get; set; }
        public BuilderExplorer.BuilderElementInfoVisibilityState elementInfoVisibilityState { get; set; }

        VisualTreeAsset m_ClassPillTemplate;

        public IList<ITreeViewItem> treeRootItems
        {
            get
            {
                return m_TreeRootItems;
            }
            set {}
        }

        public IEnumerable<ITreeViewItem> treeItems
        {
            get
            {
                return m_TreeView.items;
            }
        }

        IList<ITreeViewItem> m_TreeRootItems;

        TreeView m_TreeView;
        HighlightOverlayPainter m_TreeViewHoverOverlay;

        VisualElement m_Container;
        ElementHierarchySearchBar m_SearchBar;

        Action<VisualElement> m_SelectElementCallback;

        List<VisualElement> m_SearchResultsHightlights;
        IPanel m_CurrentPanelDebug;

        VisualElement m_DocumentRootElement;
        BuilderSelection m_Selection;
        BuilderClassDragger m_ClassDragger;
        BuilderHierarchyDragger m_HierarchyDragger;
        BuilderExplorerContextMenu m_ContextMenuManipulator;

        StringBuilder m_SelectorStrBuilder;

        public VisualElement container
        {
            get { return m_Container; }
        }

        public ElementHierarchyView(
            VisualElement documentRootElement,
            BuilderSelection selection,
            BuilderClassDragger classDragger,
            BuilderHierarchyDragger hierarchyDragger,
            BuilderExplorerContextMenu contextMenuManipulator,
            Action<VisualElement> selectElementCallback,
            OverlayPainterHelperElement helperElement = null)
        {
            m_DocumentRootElement = documentRootElement;
            m_Selection = selection;
            m_ClassDragger = classDragger;
            m_HierarchyDragger = hierarchyDragger;
            m_ContextMenuManipulator = contextMenuManipulator;

            m_SelectorStrBuilder = new StringBuilder();

            this.focusable = true;

            m_SelectElementCallback = selectElementCallback;
            hierarchyHasChanged = true;

            m_SearchResultsHightlights = new List<VisualElement>();

            this.RegisterCallback<FocusEvent>(e => m_TreeView?.Focus());

            // HACK: ListView/TreeView need to clear their selections when clicking on nothing.
            this.RegisterCallback<MouseDownEvent>(e =>
            {
                var leafTarget = e.leafTarget as VisualElement;
                if (leafTarget.parent is ScrollView)
                    ClearSelection();
            });

            m_TreeViewHoverOverlay = new HighlightOverlayPainter();

            m_Container = new VisualElement();
            m_Container.name = "explorer-container";
            m_Container.style.flexGrow = 1;
            m_ClassDragger.builderHierarchyRoot = m_Container;
            m_HierarchyDragger.builderHierarchyRoot = m_Container;
            Add(m_Container);

            m_SearchBar = new ElementHierarchySearchBar(this);
            Add(m_SearchBar);

            // TODO: Hiding for now since search does not work, especially with style class pills.
            m_SearchBar.style.display = DisplayStyle.None;

            m_ClassPillTemplate = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                BuilderConstants.UIBuilderPackagePath + "/BuilderClassPill.uxml");

            if (helperElement != null)
                helperElement.painter = m_TreeViewHoverOverlay;

            // Create TreeView.
            m_TreeRootItems = new List<ITreeViewItem>();
            m_TreeView = new TreeView(m_TreeRootItems, 20, MakeItem, FillItem);
            m_TreeView.viewDataKey = "unity-builder-explorer-tree";
            m_TreeView.style.flexGrow = 1;
            m_TreeView.onSelectionChanged += OnSelectionChange;

            m_Container.Add(m_TreeView);

            m_ContextMenuManipulator.RegisterCallbacksOnTarget(m_Container);
        }

        public void DrawOverlay()
        {
            m_TreeViewHoverOverlay.Draw();
        }

        void ActivateSearchBar(ExecuteCommandEvent evt)
        {
            Debug.Log(evt.commandName);
            if (evt.commandName == "Find")
                m_SearchBar.Focus();
        }

        void FillItem(VisualElement element, ITreeViewItem item)
        {
            var explorerItem = element as BuilderExplorerItem;
            explorerItem.Clear();

            // Pre-emptive cleanup.
            var row = explorerItem.parent.parent;
            row.RemoveFromClassList(BuilderConstants.ExplorerHeaderRowClassName);
            row.RemoveFromClassList(BuilderConstants.ExplorerItemHiddenClassName);

            // Get target element (in the document).
            var documentElement = (item as TreeViewItem<VisualElement>).data;
            documentElement.SetProperty(BuilderConstants.ElementLinkedExplorerItemVEPropertyName, explorerItem);
            explorerItem.userData = documentElement;
            row.userData = documentElement;

            // If we have a FillItem callback (override), we call it and stop creating the rest of the item.
            var fillItemCallback =
                documentElement.GetProperty(BuilderConstants.ExplorerItemFillItemCallbackVEPropertyName) as Action<VisualElement, ITreeViewItem, BuilderSelection>;
            if (fillItemCallback != null)
            {
                fillItemCallback(explorerItem, item, m_Selection);
                return;
            }

            // Create main label container.
            var labelCont = new VisualElement();
            labelCont.AddToClassList("unity-builder-explorer-tree-item-label-cont");
            explorerItem.Add(labelCont);

            if (BuilderSharedStyles.IsSelectorsContainerElement(documentElement))
            {
                var ssLabel = new Label("StyleSheet");
                ssLabel.AddToClassList("unity-builder-explorer-tree-item-label");
                ssLabel.AddToClassList("unity-debugger-tree-item-type");
                row.AddToClassList(BuilderConstants.ExplorerHeaderRowClassName);
                labelCont.Add(ssLabel);
                return;
            }
            else if (BuilderSharedStyles.IsSelectorElement(documentElement))
            {
                var selectorParts = BuilderSharedStyles.GetSelectorParts(documentElement);

                Action<string> addSimpleLabel = (str) =>
                {
                    var selectorPartLabel = new Label(str);
                    selectorPartLabel.AddToClassList("unity-builder-explorer-tree-item-label");
                    selectorPartLabel.AddToClassList("unity-debugger-tree-item-type");
                    labelCont.Add(selectorPartLabel);
                };

                foreach (var partStr in selectorParts)
                {
                    if (!partStr.StartsWith("."))
                    {
                        m_SelectorStrBuilder.Append(partStr);
                        continue;
                    }

                    if (m_SelectorStrBuilder.Length != 0)
                    {
                        addSimpleLabel(m_SelectorStrBuilder.ToString());
                        m_SelectorStrBuilder.Clear();
                    }

                    m_ClassPillTemplate.CloneTree(labelCont);
                    var pill = labelCont.contentContainer.ElementAt(labelCont.childCount - 1);
                    var pillLabel = pill.Q<Label>("class-name-label");
                    pill.AddToClassList("unity-debugger-tree-item-pill");
                    pill.SetProperty(BuilderConstants.ExplorerStyleClassPillClassNameVEPropertyName, partStr);
                    pill.userData = documentElement;

                    // Add ellipsis if the class name is too long.
                    var partStrShortened = BuilderNameUtilities.CapStringLengthAndAddEllipsis(partStr, BuilderConstants.ClassNameInPillMaxLength);
                    pillLabel.text = partStrShortened;

                    m_ClassDragger.RegisterCallbacksOnTarget(pill);
                }

                if (m_SelectorStrBuilder.Length != 0)
                {
                    addSimpleLabel(m_SelectorStrBuilder.ToString());
                    m_SelectorStrBuilder.Clear();
                }

                // Register right-click events for context menu actions.
                m_ContextMenuManipulator.RegisterCallbacksOnTarget(explorerItem);

                return;
            }
            else if (BuilderSharedStyles.IsDocumentElement(documentElement))
            {
                var ssLabel = new Label("Hierarchy");
                ssLabel.AddToClassList("unity-builder-explorer-tree-item-label");
                ssLabel.AddToClassList("unity-debugger-tree-item-type");
                row.AddToClassList(BuilderConstants.ExplorerHeaderRowClassName);
                labelCont.Add(ssLabel);
                return;
            }

            // Check if element is inside current document.
            if (!documentElement.IsPartOfCurrentDocument())
                row.AddToClassList(BuilderConstants.ExplorerItemHiddenClassName);

            // Register drag-and-drop events for reparenting.
            m_HierarchyDragger.RegisterCallbacksOnTarget(explorerItem);

            // Allow reparenting.
            explorerItem.SetProperty(BuilderConstants.ExplorerItemElementLinkVEPropertyName, documentElement);

            // Element type label.
            if (string.IsNullOrEmpty(documentElement.name) ||
                elementInfoVisibilityState.HasFlag(BuilderExplorer.BuilderElementInfoVisibilityState.TypeName))
            {
                var typeLabel = new Label(documentElement.typeName);
                typeLabel.AddToClassList("unity-builder-explorer-tree-item-label");
                typeLabel.AddToClassList("unity-debugger-tree-item-type");
                typeLabel.AddToClassList(BuilderConstants.ExplorerItemTypeLabelClassName);
                labelCont.Add(typeLabel);
            }

            // Element name label.
            var nameLabel = new Label();
            nameLabel.AddToClassList("unity-builder-explorer-tree-item-label");
            nameLabel.AddToClassList("unity-debugger-tree-item-name");
            nameLabel.AddToClassList("unity-debugger-tree-item-name-label");
            nameLabel.AddToClassList(BuilderConstants.ExplorerItemNameLabelClassName);
            if (!string.IsNullOrEmpty(documentElement.name))
                nameLabel.text = "#" + documentElement.name;
            labelCont.Add(nameLabel);

            // Textfield to rename element in hierarchy.
            var renameTextfield = explorerItem.CreateRenamingTextField(documentElement, nameLabel, m_Selection);
            labelCont.Add(renameTextfield);

            // Add class list.
            if (documentElement.classList.Count > 0 && elementInfoVisibilityState.HasFlag(BuilderExplorer.BuilderElementInfoVisibilityState.ClassList))
            {
                foreach (var ussClass in documentElement.GetClasses())
                {
                    var classLabelCont = new VisualElement();
                    classLabelCont.AddToClassList("unity-builder-explorer-tree-item-label-cont");
                    explorerItem.Add(classLabelCont);

                    var classLabel = new Label("." + ussClass);
                    classLabel.AddToClassList("unity-builder-explorer-tree-item-label");
                    classLabel.AddToClassList("unity-debugger-tree-item-classlist");
                    classLabel.AddToClassList("unity-debugger-tree-item-classlist-label");

                    classLabelCont.Add(classLabel);
                }
            }

            // Show name of uxml file if this element is a TemplateContainer.
            var path = documentElement.GetProperty(BuilderConstants.LibraryItemLinkedTemplateContainerPathVEPropertyName) as string;
            if (documentElement is TemplateContainer && !string.IsNullOrEmpty(path))
            {
                var pathStr = Path.GetFileName(path);
                var label = new Label(pathStr);
                label.AddToClassList("unity-builder-explorer-tree-item-label");
                label.AddToClassList("unity-debugger-tree-item-type");
                label.AddToClassList("unity-builder-explorer-tree-item-template-path"); // Just make it look a bit shaded.
                labelCont.Add(label);
            }

            // Register right-click events for context menu actions.
            m_ContextMenuManipulator.RegisterCallbacksOnTarget(explorerItem);
        }

        void HighlightItemInTargetWindow(VisualElement documentElement)
        {
            m_TreeViewHoverOverlay.AddOverlay(documentElement);
            var panel = documentElement.panel;
            panel?.visualTree.MarkDirtyRepaint();
        }

        public void ClearHighlightOverlay()
        {
            m_TreeViewHoverOverlay.ClearOverlay();
        }

        public void ResetHighlightOverlays()
        {
            m_TreeViewHoverOverlay.ClearOverlay();

            if (m_TreeView != null)
            {
                foreach (TreeViewItem<VisualElement> selectedItem in m_TreeView.currentSelection)
                {
                    var documentElement = selectedItem.data;
                    HighlightAllRelatedDocumentElements(documentElement);
                }
            }

            var panel = this.panel;
            panel?.visualTree.MarkDirtyRepaint();
        }

        public void RebuildTree(VisualElement rootVisualElement)
        {
            if (!hierarchyHasChanged)
                return;

            // Save focus state.
            bool wasTreeFocused = false;
            if (m_TreeView != null)
                wasTreeFocused = m_TreeView.Q<ListView>().IsFocused();

            ResetHighlightOverlays();

            if (rootVisualElement.childCount == 0)
                return;

            m_CurrentPanelDebug = rootVisualElement.panel;

            int nextId = 1;
            m_TreeRootItems = GetTreeItemsFromVisualTree(rootVisualElement, ref nextId);

            // Clear selection which would otherwise persist via view data persistence.
            m_TreeView?.ClearSelection();
            m_TreeView.rootItems = m_TreeRootItems;

            // Restore focus state.
            if (wasTreeFocused)
                m_TreeView.Q<ListView>()?.Focus();

            // Auto-expand all items on load.
            foreach (var item in m_TreeView.rootItems)
                m_TreeView.ExpandItem(item.id);

            hierarchyHasChanged = false;
        }

        void OnSelectionChange(List<ITreeViewItem> items)
        {
            if (m_SelectElementCallback == null)
                return;

            if (items.Count == 0)
            {
                m_SelectElementCallback(null);
                return;
            }

            var item = items.First() as TreeViewItem<VisualElement>;
            var element = item != null ? item.data : null;
            m_SelectElementCallback(element);

            ResetHighlightOverlays();
        }

        void HighlightAllElementsMatchingSelectorElement(VisualElement selectorElement)
        {
            var selector = selectorElement.GetProperty(BuilderConstants.ElementLinkedStyleSelectorVEPropertyName) as StyleComplexSelector;
            if (selector == null)
                return;

            var selectorStr = StyleSheetToUss.ToUssSelector(selector);
            var matchingElements = BuilderSharedStyles.GetMatchingElementsForSelector(m_DocumentRootElement, selectorStr);
            if (matchingElements == null)
                return;

            foreach (var element in matchingElements)
                HighlightItemInTargetWindow(element);
        }

        void HighlightAllRelatedDocumentElements(VisualElement documentElement)
        {
            if (BuilderSharedStyles.IsSelectorElement(documentElement))
            {
                HighlightAllElementsMatchingSelectorElement(documentElement);
            }
            else
            {
                HighlightItemInTargetWindow(documentElement);
            }
        }

        VisualElement MakeItem()
        {
            var element = new BuilderExplorerItem();
            element.name = "unity-treeview-item-content";
            element.RegisterCallback<MouseEnterEvent>((e) =>
            {
                ClearHighlightOverlay();

                var explorerItem = e.target as VisualElement;
                var documentElement = explorerItem.userData as VisualElement;
                HighlightAllRelatedDocumentElements(documentElement);
            });
            element.RegisterCallback<MouseLeaveEvent>((e) =>
            {
                ResetHighlightOverlays();
            });

            return element;
        }

        TreeViewItem<VisualElement> FindElement(IEnumerable<ITreeViewItem> list, VisualElement element)
        {
            if (list == null)
                return null;

            foreach (var item in list)
            {
                var treeItem = item as TreeViewItem<VisualElement>;
                if (treeItem.data == element)
                    return treeItem;

                TreeViewItem<VisualElement> itemFoundInChildren = null;
                if (treeItem.hasChildren)
                    itemFoundInChildren = FindElement(treeItem.children, element);

                if (itemFoundInChildren != null)
                    return itemFoundInChildren;
            }

            return null;
        }

        public void ClearSelection()
        {
            if (m_TreeView == null)
                return;

            m_TreeView.ClearSelection();

            ResetHighlightOverlays();
        }

        public void ClearSearchResults()
        {
            foreach (var hl in m_SearchResultsHightlights)
                hl.RemoveFromHierarchy();

            m_SearchResultsHightlights.Clear();
        }

        public void SelectElement(VisualElement element)
        {
            SelectElement(element, string.Empty);
        }

        public void SelectElement(VisualElement element, string query)
        {
            SelectElement(element, query, SearchHighlight.None);
        }

        public void SelectElement(VisualElement element, string query, SearchHighlight searchHighlight)
        {
            ClearSearchResults();

            var item = FindElement(m_TreeRootItems, element);
            if (item == null)
                return;

            m_TreeView.SelectItem(item.id);

            if (string.IsNullOrEmpty(query))
                return;

            var selected = m_TreeView.Query(classes: "unity-list-view__item--selected").First();
            if (selected == null || searchHighlight == SearchHighlight.None)
                return;

            var content = selected.Q("unity-treeview-item-content");
            var labelContainers = content.Query(classes: "unity-builder-explorer-tree-item-label-cont").ToList();
            foreach (var labelContainer in labelContainers)
            {
                var label = labelContainer.Q<Label>();

                if (label.ClassListContains("unity-debugger-tree-item-type") && searchHighlight != SearchHighlight.Type)
                    continue;

                if (label.ClassListContains("unity-debugger-tree-item-name") && searchHighlight != SearchHighlight.Name)
                    continue;

                if (label.ClassListContains("unity-debugger-tree-item-classlist") && searchHighlight != SearchHighlight.Class)
                    continue;

                var text = label.text;
                var indexOf = text.IndexOf(query, StringComparison.OrdinalIgnoreCase);
                if (indexOf < 0)
                    continue;

                var highlight = new VisualElement();
                m_SearchResultsHightlights.Add(highlight);
                highlight.AddToClassList("unity-debugger-highlight");
                int letterSize = 8;
                highlight.style.width = query.Length * letterSize;
                highlight.style.left = indexOf * letterSize;
                labelContainer.Insert(0, highlight);

                break;
            }
        }

        IList<ITreeViewItem> GetTreeItemsFromVisualTree(VisualElement parent, ref int nextId)
        {
            List<ITreeViewItem> items = null;

            if (parent == null)
                return null;

            int count = parent.hierarchy.childCount;
            if (count == 0)
                return null;

            for (int i = 0; i < count; i++)
            {
                var element = parent.hierarchy[i];

                if (element.name == BuilderConstants.SpecialVisualElementInitialMinSizeName)
                    continue;

                if (items == null)
                    items = new List<ITreeViewItem>();

                var id = 0;
                var linkedAsset = element.GetVisualElementAsset();
                if (linkedAsset != null)
                {
                    id = linkedAsset.id;
                }
                else
                {
                    id = nextId;
                    nextId++;
                }

                var item = new TreeViewItem<VisualElement>(id, element);
                items.Add(item);

                var childItems = GetTreeItemsFromVisualTree(element, ref nextId);
                if (childItems == null)
                    continue;

                item.AddChildren(childItems);
            }

            return items;
        }
    }
}
