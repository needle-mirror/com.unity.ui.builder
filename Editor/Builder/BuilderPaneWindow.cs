using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    internal class BuilderPaneWindow : EditorWindow
    {
        BuilderDocument m_Document;

        BuilderCommandHandler m_CommandHandler;

        public BuilderDocument document
        {
            get
            {
                // Find or create document.
                if (m_Document == null)
                {
                    var allDocuments = Resources.FindObjectsOfTypeAll(typeof(BuilderDocument));
                    if (allDocuments.Length > 1)
                        Debug.LogError("UIBuilder: More than one BuilderDocument was somehow created!");
                    if (allDocuments.Length == 0)
                        m_Document = BuilderDocument.CreateInstance();
                    else
                        m_Document = allDocuments[0] as BuilderDocument;
                }

                return m_Document;
            }
        }

        public BuilderCommandHandler commandHandler
        {
            get
            {
                if (m_CommandHandler == null)
                {
                    var selection = primarySelection;
                    if (selection == null)
                        return null;

                    m_CommandHandler = new BuilderCommandHandler(this, selection);
                }
                return m_CommandHandler;
            }
        }

        public BuilderSelection primarySelection
        {
            get
            {
                if (this is IBuilderViewportWindow)
                    return (this as IBuilderViewportWindow).selection;

                return document.primaryViewportWindow.selection;
            }
        }

        protected static T GetWindowAndInit<T>(string title) where T : BuilderPaneWindow
        {
            var window = GetWindow<T>();
            window.titleContent = new GUIContent(title);
            window.Show();
            return window;
        }

        protected void OnEnable()
        {
            var root = rootVisualElement;

            // Load assets.
            var mainSS = AssetDatabase.LoadAssetAtPath<StyleSheet>(BuilderConstants.UIBuilderPackagePath + "/Builder.uss");
            var themeSS = EditorGUIUtility.isProSkin
                ? AssetDatabase.LoadAssetAtPath<StyleSheet>(BuilderConstants.UIBuilderPackagePath + "/BuilderDark.uss")
                : AssetDatabase.LoadAssetAtPath<StyleSheet>(BuilderConstants.UIBuilderPackagePath + "/BuilderLight.uss");

            // HACK: Check for null assets.
            // See: https://fogbugz.unity3d.com/f/cases/1180330/
            if (mainSS == null || themeSS == null)
            {
                EditorApplication.delayCall += () =>
                {
                    this.m_Parent.Reload(this);
                };
                return;
            }

            // Load styles.
            root.styleSheets.Add(mainSS);
            root.styleSheets.Add(themeSS);

            // Handle viewport window.
            if (this is IBuilderViewportWindow || document.primaryViewportWindow != null)
                CreateUIInternal();

            // Register window.
            document.RegisterWindow(this);
        }

        void CreateUIInternal()
        {
            CreateUI();

            commandHandler.OnEnable();
        }

        public virtual void CreateUI()
        {
            // Nothing to do by default.
        }

        public virtual void ClearUI()
        {
            // Nothing to do by default.
        }

        protected void OnDisable()
        {
            // Unregister window.
            document.UnregisterWindow(this);

            // Commands
            if (m_CommandHandler != null)
                m_CommandHandler.OnDisable();
        }

        public virtual void OnEnableAfterAllSerialization()
        {
            // Nothing to do by default.
        }

        public virtual void LoadDocument(VisualTreeAsset asset)
        {
            // Nothing to do by default.
        }

        public virtual void PrimaryViewportWindowChanged()
        {
            if (this is IBuilderViewportWindow)
                return;

            ClearUI();
            rootVisualElement.Clear();

            m_CommandHandler = null;

            var viewportWindow = document.primaryViewportWindow;
            if (viewportWindow == null)
                AddMessageForNoViewportOpen();
            else
                CreateUIInternal();
        }

        void AddMessageForNoViewportOpen()
        {
            rootVisualElement.Add(new Label("No viewport window open."));
        }
    }
}