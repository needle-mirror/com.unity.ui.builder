using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Toolbar = UnityEditor.UIElements.Toolbar;

namespace Unity.UI.Builder
{
    internal class BuilderLibrary : BuilderPaneContent, IBuilderAssetModificationProcessor
    {
        static readonly string s_UssClassName = "unity-builder-library";

        readonly string k_TreeViewName = "library-tree-view";
        readonly string k_TreeViewClassName = "unity-builder-library__tree-view";

        readonly string k_TreeItemClassName = "unity-builder-library__tree-item";
        readonly string k_TreeItemLabelClassName = "unity-builder-library__tree-item-label";

        readonly string k_OpenButtonClassName = "unity-builder-library__tree-item-open-button";

        static List<string> s_NameSpacesToAvoid = new List<string>() { "Unity", "UnityEngine", "UnityEditor" };

        BuilderPaneWindow m_PaneWindow;
        VisualElement m_DocumentElement;
        BuilderSelection m_Selection;
        BuilderLibraryDragger m_Dragger;

        int m_ProjectUxmlPathsHash;

        BuilderTooltipPreview m_TooltipPreview;

        public class LibraryTreeItem : TreeViewItem<string>
        {
            static int m_NextId = 0;

            public string name => data;

            public bool isHeader { get; set; }

            public VisualTreeAsset sourceAsset { get; set; }

            public Func<VisualElement> makeVisualElement { get; set; }

            public Func<VisualTreeAsset, VisualElementAsset, VisualElementAsset> makeElementAsset { get; set; }

            public LibraryTreeItem(
                string name, Func<VisualElement> makeVisualElement,
                Func<VisualTreeAsset, VisualElementAsset, VisualElementAsset> makeElementAsset = null,
                List<TreeViewItem<string>> children = null)
                : base(m_NextId++, name, children)
            {
                this.makeVisualElement = makeVisualElement;
                this.makeElementAsset = makeElementAsset;
            }

            static public void ResetNextId()
            {
                m_NextId = 0;
            }
        }

        class FactoryProcessingHelper
        {
            public class AttributeRecord
            {
                public XmlQualifiedName name { get; set; }
                public UxmlAttributeDescription desc { get; set; }
            }

            public Dictionary<string, AttributeRecord> attributeTypeNames;

            public SortedDictionary<string, IUxmlFactory> knownTypes;

            public FactoryProcessingHelper()
            {
                attributeTypeNames = new Dictionary<string, AttributeRecord>();
                knownTypes = new SortedDictionary<string, IUxmlFactory>();
            }

            public void RegisterElementType(IUxmlFactory factory)
            {
                knownTypes.Add(XmlQualifiedName.ToString(factory.uxmlName, factory.uxmlNamespace), factory);
            }

            public bool IsKnownElementType(string elementName, string elementNameSpace)
            {
                return knownTypes.ContainsKey(XmlQualifiedName.ToString(elementName, elementNameSpace));
            }
        }

        bool ProcessFactory(IUxmlFactory factory, FactoryProcessingHelper processingData)
        {
            if (!string.IsNullOrEmpty(factory.substituteForTypeName))
            {
                if (!processingData.IsKnownElementType(factory.substituteForTypeName, factory.substituteForTypeNamespace))
                {
                    // substituteForTypeName is not yet known. Defer processing to later.
                    return false;
                }
            }

            processingData.RegisterElementType(factory);

            return true;
        }

        static void AddCategoriesToStack(LibraryTreeItem sourceCategory, List<LibraryTreeItem> categoryStack, string[] split)
        {
            if (categoryStack.Count > split.Length)
            {
                categoryStack.RemoveRange(split.Length, categoryStack.Count - split.Length);
            }

            for (int i = 0; i < split.Length; ++i)
            {
                var part = split[i];

                if (categoryStack.Count > i)
                {
                    if (categoryStack[i].name == part)
                    {
                        continue;
                    }
                    else if (categoryStack[i].name != part)
                    {
                        categoryStack.RemoveRange(i, categoryStack.Count - i);
                    }
                }

                if (categoryStack.Count <= i)
                {
                    var newCategory = new LibraryTreeItem(part, () => null, null, new List<TreeViewItem<string>>());

                    if (categoryStack.Count == 0)
                        sourceCategory.AddChild(newCategory);
                    else
                        categoryStack[i - 1].AddChild(newCategory);

                    categoryStack.Add(newCategory);
                    continue;
                }
            }
        }

        void ImportFactoriesFromSource(LibraryTreeItem sourceCategory)
        {
            List<IUxmlFactory> deferredFactories = new List<IUxmlFactory>();
            FactoryProcessingHelper processingData = new FactoryProcessingHelper();

            foreach (var factories in VisualElementFactoryRegistry.factories)
            {
                if (factories.Value.Count == 0)
                    continue;

                var factory = factories.Value[0];
                if (!ProcessFactory(factory, processingData))
                {
                    // Could not process the factory now, because it depends on a yet unprocessed factory.
                    // Defer its processing.
                    deferredFactories.Add(factory);
                }
            }

            List<IUxmlFactory> deferredFactoriesCopy;
            do
            {
                deferredFactoriesCopy = new List<IUxmlFactory>(deferredFactories);
                foreach (var factory in deferredFactoriesCopy)
                {
                    deferredFactories.Remove(factory);
                    if (!ProcessFactory(factory, processingData))
                    {
                        // Could not process the factory now, because it depends on a yet unprocessed factory.
                        // Defer its processing again.
                        deferredFactories.Add(factory);
                    }
                }
            }
            while (deferredFactoriesCopy.Count > deferredFactories.Count);

            if (deferredFactories.Count > 0)
            {
                Debug.Log("Some factories could not be processed because their base type is missing.");
            }

            var categoryStack = new List<LibraryTreeItem>();
            foreach (var known in processingData.knownTypes.Values)
            {
                var split = known.uxmlNamespace.Split('.');

                // Avoid adding our own internal factories (like Package Manager templates).
                if (!Unsupported.IsDeveloperMode() && split.Count() > 0 && s_NameSpacesToAvoid.Contains(split[0]))
                    continue;

                // Avoid adding UI Builder's own types, even in internal mode.
                if (split.Count() >= 3 && split[0] == "Unity" && split[1] == "UI" && split[2] == "Builder")
                    continue;

                AddCategoriesToStack(sourceCategory, categoryStack, split);

                var asset = new VisualElementAsset(known.uxmlQualifiedName);

                var slots = new Dictionary<string, VisualElement>();
                var overrides = new List<TemplateAsset.AttributeOverride>();
                var vta = ScriptableObject.CreateInstance<VisualTreeAsset>();
                var context = new CreationContext(slots, overrides, vta, null);

                var newItem = new LibraryTreeItem(
                    known.uxmlName, () => known.Create(asset, context));
                if (categoryStack.Count == 0)
                    sourceCategory.AddChild(newItem);
                else
                    categoryStack.Last().AddChild(newItem);
            }
        }

        public void OnAfterBuilderDeserialize()
        {
            RefreshTreeView();
        }

        int GetAllProjectUxmlFilePathsHash(IEnumerable<HierarchyProperty> assets = null)
        {
            if (assets == null)
            {
                var filter = new SearchFilter();
                filter.classNames = new string[] { "VisualTreeAsset" };
                assets = AssetDatabase.FindAllAssets(filter);
            }

            var sb = new StringBuilder();

            foreach (var asset in assets)
            {
                var assetPath = AssetDatabase.GetAssetPath(asset.instanceID);
                sb.Append(assetPath);
            }

            var pathsStr = sb.ToString();
            return pathsStr.GetHashCode();
        }

        void ImportUxmlFromProject(LibraryTreeItem projectCategory)
        {
            var filter = new SearchFilter();
            filter.searchArea = SearchFilter.SearchArea.AllAssets;
            filter.classNames = new string[] { "VisualTreeAsset" };
            var assets = AssetDatabase.FindAllAssets(filter);

            m_ProjectUxmlPathsHash = GetAllProjectUxmlFilePathsHash(assets);

            var categoryStack = new List<LibraryTreeItem>();
            foreach (var asset in assets)
            {
                var assetPath = AssetDatabase.GetAssetPath(asset.instanceID);
                var prettyPath = assetPath;
                prettyPath = Path.GetDirectoryName(prettyPath);
                prettyPath = prettyPath.Replace('\\', '/');
                if (prettyPath.StartsWith(BuilderConstants.UIBuilderPackageRootPath))
                    continue;

                var split = prettyPath.Split('/');
                AddCategoriesToStack(projectCategory, categoryStack, split);

                var vta = asset.pptrValue as VisualTreeAsset;
                var newItem = new LibraryTreeItem(asset.name + ".uxml",
                    () =>
                    {
                        var tree = vta.CloneTree();
                        tree.SetProperty(BuilderConstants.LibraryItemLinkedTemplateContainerPathVEPropertyName, assetPath);
                        return tree;
                    },
                    (inVta, inParent) =>
                    {
                        return inVta.AddTemplateInstance(inParent, assetPath) as VisualElementAsset;
                    });
                newItem.sourceAsset = vta;

                if (categoryStack.Count == 0)
                    projectCategory.AddChild(newItem);
                else
                    categoryStack.Last().AddChild(newItem);
            }
        }

        void OnItemMouseEnter(MouseEnterEvent evt)
        {
            var box = evt.target as VisualElement;
            var item = box.GetProperty(BuilderConstants.LibraryItemLinkedManipulatorVEPropertyName) as LibraryTreeItem;

            if (item.makeVisualElement == null)
                return;

            var sample = item.makeVisualElement();
            if (sample == null)
                return;

            m_TooltipPreview.Add(sample);

            m_TooltipPreview.Show();

            m_TooltipPreview.style.left = this.pane.resolvedStyle.width + BuilderConstants.TooltipPreviewYOffset;
            m_TooltipPreview.style.top = this.pane.resolvedStyle.top;
        }

        void OnItemMouseLeave(MouseLeaveEvent evt)
        {
            m_TooltipPreview.Clear();
            m_TooltipPreview.Hide();
        }

        public BuilderLibrary(
            BuilderPaneWindow paneWindow, BuilderViewport viewport,
            BuilderSelection selection, BuilderLibraryDragger dragger,
            BuilderTooltipPreview tooltipPreview)
        {
            m_PaneWindow = paneWindow;
            m_DocumentElement = viewport.documentElement;
            m_Selection = selection;
            m_Dragger = dragger;
            m_TooltipPreview = tooltipPreview;

            AddToClassList(s_UssClassName);

            BuilderAssetModificationProcessor.Register(this);

            RefreshTreeView();
        }

        public void OnAssetChange()
        {
            // AssetDatabase.FindAllAssets(filter) will return outdated assets if
            // we refresh immediately.
            this.schedule.Execute(OnAfterAssetChange);
        }

        public AssetMoveResult OnWillMoveAsset(string sourcePath, string destinationPath) => AssetMoveResult.DidNotMove;
        
        public AssetDeleteResult OnWillDeleteAsset(string assetPath, RemoveAssetOptions option) =>
            AssetDeleteResult.DidNotDelete;

        void OnAfterAssetChange()
        {
            var newHash = GetAllProjectUxmlFilePathsHash();

            if (newHash == m_ProjectUxmlPathsHash)
                return;

            RefreshTreeView();
        }

        void RefreshTreeView()
        {
            Clear();
            LibraryTreeItem.ResetNextId();
            var items = new List<ITreeViewItem>();

            var unityItem = new LibraryTreeItem("Unity", () => null);
            unityItem.isHeader = true;
            IList<ITreeViewItem> unityItemList = new List<ITreeViewItem>()
            {
                new LibraryTreeItem("VisualElement",
                    () =>
                    {
                        var ve = new VisualElement();
                        var veMinSizeChild = new VisualElement();
                        veMinSizeChild.name = BuilderConstants.SpecialVisualElementInitialMinSizeName;
                        veMinSizeChild.AddToClassList(BuilderConstants.SpecialVisualElementInitialMinSizeClassName);
                        ve.Add(veMinSizeChild);
                        return ve;
                    },
                    (inVta, inParent) =>
                    {
                        var vea = new VisualElementAsset(typeof(VisualElement).ToString());
                        VisualTreeAssetUtilities.InitializeElement(vea);
                        inVta.AddElement(inParent, vea);
                        return vea;
                    }),
                new LibraryTreeItem("Button", () => new Button() { text = "Button" }),
                new LibraryTreeItem("Scroller", () => new Scroller(0, 100, (v) => { }, SliderDirection.Horizontal) { value = 42 }),
                new LibraryTreeItem("Toggle", () => new Toggle("Toggle")),
                new LibraryTreeItem("Label", () => new Label("Label")),
                new LibraryTreeItem("Text Field", () => new TextField("Text Field") { value = "filler text" }),
                new LibraryTreeItem("Object Field", () => new ObjectField("Object Field") { value = new Texture2D(10, 10) { name = "new_texture" } }),
                new LibraryTreeItem("Foldout", () => new Foldout() { text = "Foldout"}),
                new LibraryTreeItem("Numeric Fields", () => null, null, new List<TreeViewItem<string>>()
                {
                    new LibraryTreeItem("Integer", () => new IntegerField("Int Field") { value = 42 }),
                    new LibraryTreeItem("Float", () => new FloatField("Float Field") { value = 42.2f }),
                    new LibraryTreeItem("Long", () => new LongField("Long Field") { value = 42 }),
                    new LibraryTreeItem("Min-Max Slider", () => new MinMaxSlider("Min/Max Slider", 0, 20, -10, 40) { value = new Vector2(10, 12) }),
                    new LibraryTreeItem("Slider", () => new Slider("Slider", 0, 100) { value = 42 }),
                    new LibraryTreeItem("Progress Bar", () => new ProgressBar() { title = "my-progress", value = 22 } ),
                    new LibraryTreeItem("Vector2", () => new Vector2Field("Vec2 Field")),
                    new LibraryTreeItem("Vector3", () => new Vector3Field("Vec3 Field")),
                    new LibraryTreeItem("Vector4", () => new Vector4Field("Vec4 Field")),
                    new LibraryTreeItem("Rect", () => new RectField("Rect")),
                    new LibraryTreeItem("Bounds", () => new BoundsField("Bounds")),
                    new LibraryTreeItem("Slider (Int)", () => new SliderInt("SliderInt", 0, 100) { value = 42 }),
                    new LibraryTreeItem("Vector2 (Int)", () => new Vector2IntField("Vector2Int")),
                    new LibraryTreeItem("Vector3 (Int)", () => new Vector3IntField("Vector3Int")),
                    new LibraryTreeItem("Rect (Int)", () => new RectIntField("RectInt")),
                    new LibraryTreeItem("Bounds (Int)", () => new BoundsIntField("BoundsInt"))
                }),
                new LibraryTreeItem("Value Fields", () => null, null, new List<TreeViewItem<string>>()
                {
                    new LibraryTreeItem("Color", () => new ColorField("Color") { value = Color.cyan }),
                    new LibraryTreeItem("Curve", () => new CurveField("Curve") {
                        value = new AnimationCurve(new Keyframe[] { new Keyframe(0, 0), new Keyframe(5, 8), new Keyframe(10, 4) })
                    }),
                    new LibraryTreeItem("Gradient", () => new GradientField("Gradient") {
                        value = new Gradient()
                        {
                            colorKeys = new GradientColorKey[]
                            {
                                new GradientColorKey(Color.red, 0),
                                new GradientColorKey(Color.blue, 10),
                                new GradientColorKey(Color.green, 20)
                            }
                        }
                    })
                }),
                new LibraryTreeItem("Choice Fields", () => null, null, new List<TreeViewItem<string>>()
                {
                    new LibraryTreeItem("Enum", () => new EnumField("Enum", TextAlignment.Center)),

                    // No UXML support for PopupField.
                    //new LibraryTreeItem("Popup", () => new PopupField<string>("Normal Field", choices, 0)),

                    new LibraryTreeItem("Tag", () => new TagField("Tag", "Player")),
                    new LibraryTreeItem("Mask", () => new MaskField("Mask")),
                    new LibraryTreeItem("Layer", () => new LayerField("Layer")),
                    new LibraryTreeItem("LayerMask", () => new LayerMaskField("LayerMask"))
                }),
                new LibraryTreeItem("Containers", () => null, null, new List<TreeViewItem<string>>()
                {
                    new LibraryTreeItem("ScrollView", () => new ScrollView()),
                    new LibraryTreeItem("ListView", () => new ListView()),
                    new LibraryTreeItem("IMGUIContainer", () => new IMGUIContainer()),
                }),
                new LibraryTreeItem("Toolbar", () => null, null, new List<TreeViewItem<string>>()
                {
                    new LibraryTreeItem("Toolbar", () => new Toolbar()),
                    new LibraryTreeItem("Toolbar Menu", () => new ToolbarMenu()),
                    new LibraryTreeItem("Toolbar Button", () => new ToolbarButton { text = "Button" }),
                    new LibraryTreeItem("Toolbar Spacer", () => new ToolbarSpacer()),
                    new LibraryTreeItem("Toolbar Toggle", () => new ToolbarToggle { label = "Toggle" }),
#if UNITY_2019_3_OR_NEWER
                    new LibraryTreeItem("Toolbar Breadcrumbs", () => new ToolbarBreadcrumbs()),
#endif
                    new LibraryTreeItem("Toolbar Search Field", () => new ToolbarSearchField ()),
                    new LibraryTreeItem("Toolbar Popup Search Field", () => new ToolbarPopupSearchField ()),
                }),
                new LibraryTreeItem("Inspectors", () => null, null, new List<TreeViewItem<string>>()
                {
                    new LibraryTreeItem("PropertyField", () => new PropertyField())
                }),
            };
            unityItem.AddChildren(unityItemList);
            items.Add(unityItem);

            // From Project
            var fromProjectCategory = new LibraryTreeItem("Project", () => null);
            fromProjectCategory.isHeader = true;
            items.Add(fromProjectCategory);
            ImportUxmlFromProject(fromProjectCategory);
            ImportFactoriesFromSource(fromProjectCategory);

            var treeView = new TreeView() { name = k_TreeViewName };
            treeView.AddToClassList(k_TreeViewClassName);
            Add(treeView);

            treeView.viewDataKey = "samples-tree";
            treeView.itemHeight = 20;
            treeView.rootItems = items;
            treeView.makeItem = () => MakeItem(); // This is apparently more optimal than "= MakeItem;".
            treeView.bindItem = (e, i) => BindItem(e, i);
#if UNITY_2020_1_OR_NEWER
            treeView.onItemsChosen += (s) => OnItemsChosen(s);
#else
            treeView.onItemChosen += (s) => OnItemChosen(s);
#endif
            treeView.Refresh();

            // Make sure the Hierarchy View gets focus when the pane gets focused.
            primaryFocusable = treeView.Q<ListView>();

            // Auto-expand all items on load.
            foreach (var item in treeView.rootItems)
                treeView.ExpandItem(item.id);
        }

        VisualElement MakeItem()
        {
            var box = new VisualElement();
            box.AddToClassList(k_TreeItemClassName);
            m_Dragger?.RegisterCallbacksOnTarget(box);

            if (m_TooltipPreview != null)
            {
                box.RegisterCallback<MouseEnterEvent>(OnItemMouseEnter);
                box.RegisterCallback<MouseLeaveEvent>(OnItemMouseLeave);
            }

            var label = new Label();
            label.AddToClassList(k_TreeItemLabelClassName);

            box.Add(label);

            // Open button.
            var openButton = new Button() { name = k_OpenButtonClassName, text = "Open" };
            openButton.AddToClassList(BuilderConstants.HiddenStyleClassName);
            openButton.clickable.clickedWithEventInfo += OnOpenButtonClick;
            box.Add(openButton);

            return box;
        }

        void BindItem(VisualElement element, ITreeViewItem item)
        {
            var builderItem = item as LibraryTreeItem;

            // Pre-emptive cleanup.
            var row = element.parent.parent;
            row.RemoveFromClassList(BuilderConstants.ExplorerHeaderRowClassName);
            row.SetEnabled(true);

            // If this is the uxml file entry of the currently open file, don't allow
            // the user to instantiate it (infinite recursion) or re-open it.
            if (builderItem.sourceAsset == m_PaneWindow.document.visualTreeAsset)
            {
                row.SetEnabled(false);
                row.AddToClassList(BuilderConstants.LibraryCurrentlyOpenFileItemClassName);
            }

            // Header
            if (builderItem.isHeader)
                row.AddToClassList(BuilderConstants.ExplorerHeaderRowClassName);

            // Set label.
            var label = element.ElementAt(0) as Label;
            label.text = builderItem.data;

            // Set open button visibility.
            var openButton = element.Q<Button>(k_OpenButtonClassName);
            openButton.userData = item;
            if (builderItem.sourceAsset == null)
                openButton.AddToClassList(BuilderConstants.HiddenStyleClassName);
            else
                openButton.RemoveFromClassList(BuilderConstants.HiddenStyleClassName);

            element.userData = item;
            element.SetProperty(BuilderConstants.LibraryItemLinkedManipulatorVEPropertyName, builderItem);
        }

#if UNITY_2020_1_OR_NEWER
        void OnItemsChosen(IEnumerable<ITreeViewItem> selectedItems)
#else
        void OnItemChosen(ITreeViewItem selectedItem)
#endif
        {
#if UNITY_2020_1_OR_NEWER
            if (selectedItems == null || selectedItems.Count() == 0)
                return;

            var selectedItem = selectedItems.First();
#else
            if (selectedItem == null)
                return;
#endif

            var item = selectedItem as LibraryTreeItem;
            if (item.makeVisualElement == null)
                return;

            // If this is the uxml file entry of the currently open file, don't allow
            // the user to instantiate it (infinite recursion) or re-open it.
            if (item.name == m_PaneWindow.document.uxmlFileName)
                return;

            var newElement = item.makeVisualElement();
            if (newElement == null)
                return;

            m_DocumentElement.Add(newElement);

            if (item.makeElementAsset == null)
                BuilderAssetUtilities.AddElementToAsset(m_PaneWindow.document, newElement);
            else
                BuilderAssetUtilities.AddElementToAsset(
                    m_PaneWindow.document, newElement, item.makeElementAsset);

            // TODO: ListView bug. Does not refresh selection pseudo states after a
            // call to Refresh().
            m_Selection.NotifyOfHierarchyChange(null);
            this.schedule.Execute(() =>
            {
                m_Selection.Select(null, newElement);
            }).ExecuteLater(200);
        }

        public void ResetCurrentlyLoadedUxmlStyles()
        {
            var previouslyOpenFileItem = this.Q(className: BuilderConstants.LibraryCurrentlyOpenFileItemClassName);
            if (previouslyOpenFileItem != null)
            {
                previouslyOpenFileItem.SetEnabled(true);
                previouslyOpenFileItem.RemoveFromClassList(BuilderConstants.LibraryCurrentlyOpenFileItemClassName);
            }

            var vta = m_PaneWindow.document.visualTreeAsset;
            this.Query(className: k_TreeItemClassName).ForEach((e) =>
            {
                var item =
                    e.GetProperty(BuilderConstants.LibraryItemLinkedManipulatorVEPropertyName)
                    as LibraryTreeItem;

                if (item == null || item.sourceAsset != vta)
                    return;

                var row = e.GetFirstAncestorWithClass(ListView.itemUssClassName);
                row.SetEnabled(false);
                row.AddToClassList(BuilderConstants.LibraryCurrentlyOpenFileItemClassName);
            });
        }

        void OnOpenButtonClick(EventBase evt)
        {
            var button = evt.target as Button;
            var item = button.userData as LibraryTreeItem;

            if (item.sourceAsset == null)
                return;

            m_PaneWindow.LoadDocument(item.sourceAsset);
        }
    }
}
