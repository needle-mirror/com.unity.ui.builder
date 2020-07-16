using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    class VariableInfoTooltip : BuilderTooltipPreview
    {
        static readonly string s_UssClassName = "unity-builder-inspector__varinfo-tooltip";

        VariableEditingHandler m_CurrentHandler;
        Label m_VarNameLabel;
        Label m_SelectorSourceLabel;
#if false // TODO: Will need to bring this back once we can also do the dragger at the same time.
        Button m_EditButton;
#endif
        bool m_Showing = true;

        public VariableEditingHandler currentHandler => m_CurrentHandler;

        public VariableInfoTooltip()
        {
            AddToClassList(s_UssClassName);

            VisualElement content = new VisualElement();

            // Load Template
            var template = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                BuilderConstants.UIBuilderPackagePath + "/Inspector/VariableInfo.uxml");
            template.CloneTree(content);

            Add(content);

            m_VarNameLabel = this.Q<Label>("inspector-varinfo-name-label");

            m_SelectorSourceLabel = this.Q<Label>("inspector-varinfo-source");

#if false // TODO: Will need to bring this back once we can also do the dragger at the same time.
            m_EditButton = this.Q<Button>("inspector-varinfo-edit-button");

            /*
            var shorcutLabel = this.Q<Label>("inspector-varinfo-edit-shortcut");
            shorcutLabel.text = "(CTRL+Q)";*/
#if UNITY_2020_1_OR_NEWER
            m_EditButton.RegisterCallback<ClickEvent>(e =>
            {
                m_CurrentHandler.ShowVariableField();
            });
#else
            m_EditButton.clickable.clicked += () => {
                m_CurrentHandler.ShowVariableField();
            };
#endif
#endif // false

// TODO: Will need to bring this back once we can also do the dragger at the same time.
#if false
            RegisterCallback<FocusOutEvent>(e => {
                if (m_CurrentHandler != null)
                {
                    Hide();
                    m_CurrentHandler = null;
                }
            });
#endif

            this.RegisterCallback<GeometryChangedEvent>(e => {
                if (m_Showing)
                {
                    m_Showing = false;
                    AdjustXPosition();
                }
            });

            focusable = true;
        }

        public void Show(VariableEditingHandler handler, string variableName, StyleSheet varStyleSheetOrigin, StyleComplexSelector varSelectorOrigin)
        {
            m_CurrentHandler = handler;
            m_VarNameLabel.text = !string.IsNullOrEmpty(variableName) ? variableName : "None";

            if (!string.IsNullOrEmpty(variableName) && varSelectorOrigin != null)
            {
                var fullPath = AssetDatabase.GetAssetPath(varStyleSheetOrigin);
                string displayPath = null;

                if (string.IsNullOrEmpty(fullPath))
                {
                    displayPath = varStyleSheetOrigin.name;
                }
                else
                {
                    if (fullPath == "Library/unity editor resources")
                        displayPath = varStyleSheetOrigin.name;
                    else
                        displayPath = Path.GetFileName(fullPath);
                }

                m_SelectorSourceLabel.text = $"{StyleSheetToUss.ToUssSelector(varSelectorOrigin)} ({displayPath})";
            }
            else
            {
                m_SelectorSourceLabel.text = "None";
            }

            var pos = handler.labelElement.ChangeCoordinatesTo(parent, Vector2.zero);

            pos.x = parent.layout.width - (style.minWidth.value.value + handler.inspector.resolvedStyle.width + BuilderConstants.TooltipPreviewYOffset);
            style.left = pos.x;
            style.top = pos.y;
            m_Showing = true;
            Show();
            // TODO: Will need to bring this back once we can also do the dragger at the same time.
            //Focus();
        }

        void AdjustXPosition()
        {
            if (m_CurrentHandler == null || parent == null)
                return;

            style.left = parent.layout.width - (resolvedStyle.width + m_CurrentHandler.inspector.pane.resolvedStyle.width + BuilderConstants.TooltipPreviewYOffset);
        }
    }
}

