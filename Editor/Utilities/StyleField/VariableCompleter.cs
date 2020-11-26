using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.StyleSheets;

namespace Unity.UI.Builder
{
    internal class VariableCompleter : FieldSearchCompleter<VariableInfo>
    {
        public static readonly string s_ItemUssClassName = "unity-field-search-completer-popup__item";
        public static readonly string s_ItemNameLabelName = "nameLabel";
        public static readonly string s_ItemNameLabelUssClassName = "unity-field-search-completer-popup__item__name-label";
        public static readonly string s_ItemEditorOnlyLabelName = "editorOnlyLabel";
        public static readonly string s_ItemEditorOnlyLabelUssClassName = "unity-field-search-completer-popup__item__editor-only-label";

        VariableEditingHandler m_Handler;
        VariableInfoView m_DetailsView;

        public VariableCompleter(VariableEditingHandler handler)
            : base(handler.variableField != null ? handler.variableField.textField : null)
        {
            m_Handler = handler;
            getFilterFromTextCallback = (text) => text != null ? text.TrimStart('-') : null;
            dataSourceCallback = () =>
            {
                return StyleVariableUtilities.GetAllAvailableVariables(handler.inspector.currentVisualElement, GetCompatibleStyleValueTypes(handler), handler.inspector.document.fileSettings.editorExtensionMode);
            };
            makeItem = () =>
            {
                var item = new VisualElement();

                item.AddToClassList(s_ItemUssClassName);
                var nameLabel = new Label();
                var editorOnlyLabel = new Label(BuilderConstants.EditorOnlyTag);
                nameLabel.AddToClassList(s_ItemNameLabelUssClassName);
                nameLabel.name = s_ItemNameLabelName;
#if !UNITY_2019_4
                // Cannot use USS because no way to do version checks in USS.
                // This is not available in 2019.4.
                nameLabel.style.textOverflow = TextOverflow.Ellipsis;
#endif
                editorOnlyLabel.AddToClassList(s_ItemEditorOnlyLabelUssClassName);
                editorOnlyLabel.AddToClassList("unity-builder-tag-pill");
                editorOnlyLabel.name = s_ItemEditorOnlyLabelName;
                item.Add(nameLabel);
                item.Add(editorOnlyLabel);
                return item;
            };
            bindItem = (e, i) =>
            {
                var res = results[i];

                e.Q<Label>(s_ItemNameLabelName).text = res.name;
                e.Q<Label>(s_ItemEditorOnlyLabelName).EnableInClassList(BuilderConstants.HiddenStyleClassName, !res.isEditorVar);
            };

            m_DetailsView = new VariableInfoView();
            m_DetailsView.AddToClassList(BuilderConstants.HiddenStyleClassName);
            detailsContent = m_DetailsView;
            onSelectionChange += data =>
            {
                m_DetailsView.SetInfo(data);
                if (data != null)
                {
                    m_DetailsView.RemoveFromClassList(BuilderConstants.HiddenStyleClassName);
                }
                else
                {
                    m_DetailsView.AddToClassList(BuilderConstants.HiddenStyleClassName);
                }
            };

            matcherCallback = Matcher;
            getTextFromDataCallback = GetVarName;
        }

        static string GetVarName(VariableInfo data)
        {
            return data.name;
        }

        public static StyleValueType[] GetCompatibleStyleValueTypes(VariableEditingHandler handler)
        {
            var field = handler.inspector.styleFields.FindStylePropertyInfo(handler.styleName);
            if (field == null)
                return new[] { StyleValueType.Invalid };

            var val = field.GetValue(handler.inspector.currentVisualElement.computedStyle, null);
            var valType = val == null ? typeof(object) : val.GetType();

            if (BuilderInspectorStyleFields.IsComputedStyleFloat(val) || BuilderInspectorStyleFields.IsComputedStyleInt(val)
                || BuilderInspectorStyleFields.IsComputedStyleLength(val))
            {
                return new[] { StyleValueType.Float, StyleValueType.Dimension };
            }
            else if (BuilderInspectorStyleFields.IsComputedStyleColor(val))
            {
                return new[] { StyleValueType.Color };
            }
            else if (BuilderInspectorStyleFields.IsComputedStyleFont(val, handler.styleName))
            {
                return new[] { StyleValueType.AssetReference, StyleValueType.ResourcePath };
            }
            else if (BuilderInspectorStyleFields.IsComputedStyleBackground(val))
            {
                return new[] { StyleValueType.ScalableImage, StyleValueType.AssetReference, StyleValueType.ResourcePath };
            }
            else if (BuilderInspectorStyleFields.IsComputedStyleCursor(val))
            {
                return new[] { StyleValueType.Enum, StyleValueType.ScalableImage, StyleValueType.AssetReference, StyleValueType.ResourcePath };
            }
            else if (BuilderInspectorStyleFields.IsComputedStyleEnum(val, valType))
            {
                return new[] { StyleValueType.Enum };
            }
            return new[] { StyleValueType.Invalid };
        }

        bool Matcher(string filter, VariableInfo data)
        {
            var text = data.name;
            return string.IsNullOrEmpty(text) ? false : text.Contains(filter);
        }

        protected override bool IsValidText(string text)
        {
            if (m_Handler.variableField != null && m_Handler.variableField.textField == textField)
            {
                return true;
            }
            else
            {
                return text.StartsWith(BuilderConstants.UssVariablePrefix);
            }
        }
    }
}
