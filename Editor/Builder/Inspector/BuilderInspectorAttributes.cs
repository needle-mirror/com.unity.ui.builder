using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    internal class BuilderInspectorAttributes : IBuilderInspectorSection
    {
        BuilderInspector m_Inspector;
        BuilderSelection m_Selection;
        PersistedFoldout m_AttributesSection;

        VisualElement currentVisualElement => m_Inspector.currentVisualElement;

        public VisualElement root => m_AttributesSection;

        public BuilderInspectorAttributes(BuilderInspector inspector)
        {
            m_Inspector = inspector;
            m_Selection = inspector.selection;

            m_AttributesSection = m_Inspector.Q<PersistedFoldout>("inspector-attributes-foldout");
        }

        public void Refresh()
        {
            m_AttributesSection.Clear();

            if (currentVisualElement == null)
                return;

            m_AttributesSection.text = currentVisualElement.typeName;

            if (m_Selection.selectionType != BuilderSelectionType.Element &&
                m_Selection.selectionType != BuilderSelectionType.ElementInTemplateInstance)
                return;

            GenerateAttributeFields();

            // Forward focus to the panel header.
            m_AttributesSection
                .Query()
                .Where(e => e.focusable)
                .ForEach((e) => m_Inspector.AddFocusable(e));
        }

        public void Enable()
        {
            m_AttributesSection.contentContainer.SetEnabled(true);
        }

        public void Disable()
        {
            m_AttributesSection.contentContainer.SetEnabled(false);
        }

        void GenerateAttributeFields()
        {
            var attributeList = currentVisualElement.GetAttributeDescriptions();

            foreach (var attribute in attributeList)
            {
                if (attribute == null || attribute.name == null)
                    continue;

                var styleRow = CreateAttributeRow(attribute);
                m_AttributesSection.Add(styleRow);
            }
        }

        BuilderStyleRow CreateAttributeRow(UxmlAttributeDescription attribute)
        {
            var attributeType = attribute.GetType();
            var vea = currentVisualElement.GetVisualElementAsset();

            // Generate field label.
            var fieldLabel = BuilderNameUtilities.ConvertDashToHuman(attribute.name);

            BindableElement fieldElement = null;
            if (attribute is UxmlStringAttributeDescription)
            {
                var uiField = new TextField(fieldLabel);
                uiField.RegisterValueChangedCallback(OnAttributeValueChange);
                fieldElement = uiField;
            }
            else if (attribute is UxmlFloatAttributeDescription)
            {
                var uiField = new FloatField(fieldLabel);
                uiField.RegisterValueChangedCallback(OnAttributeValueChange);
                fieldElement = uiField;
            }
            else if (attribute is UxmlDoubleAttributeDescription)
            {
                var uiField = new DoubleField(fieldLabel);
                uiField.RegisterValueChangedCallback(OnAttributeValueChange);
                fieldElement = uiField;
            }
            else if (attribute is UxmlIntAttributeDescription)
            {
                var uiField = new IntegerField(fieldLabel);
                uiField.RegisterValueChangedCallback(OnAttributeValueChange);
                fieldElement = uiField;
            }
            else if (attribute is UxmlLongAttributeDescription)
            {
                var uiField = new LongField(fieldLabel);
                uiField.RegisterValueChangedCallback(OnAttributeValueChange);
                fieldElement = uiField;
            }
            else if (attribute is UxmlBoolAttributeDescription)
            {
                var uiField = new Toggle(fieldLabel);
                uiField.RegisterValueChangedCallback(OnAttributeValueChange);
                fieldElement = uiField;
            }
            else if (attribute is UxmlColorAttributeDescription)
            {
                var uiField = new ColorField(fieldLabel);
                uiField.RegisterValueChangedCallback(OnAttributeValueChange);
                fieldElement = uiField;
            }
            else if (attributeType.IsGenericType && attributeType.GetGenericArguments()[0].IsEnum)
            {
                var propInfo = attributeType.GetProperty("defaultValue");
                var enumValue = propInfo.GetValue(attribute, null) as Enum;

                // Create and initialize the EnumField.
                var uiField = new EnumField(fieldLabel);
                uiField.Init(enumValue);

                uiField.RegisterValueChangedCallback(OnAttributeValueChange);
                fieldElement = uiField;
            }
            else
            {
                var uiField = new TextField(fieldLabel);
                uiField.RegisterValueChangedCallback(OnAttributeValueChange);
                fieldElement = uiField;
            }

            // Create row.
            var styleRow = new BuilderStyleRow();
            styleRow.Add(fieldElement);

            // Link the field.
            fieldElement.SetProperty(BuilderConstants.InspectorLinkedStyleRowVEPropertyName, styleRow);
            fieldElement.SetProperty(BuilderConstants.InspectorLinkedAttributeDescriptionVEPropertyName, attribute);

            // Set initial value.
            RefreshAttributeField(fieldElement);

            // Setup field binding path.
            fieldElement.bindingPath = attribute.name;

            // Tooltip.
            var label = fieldElement.Q<Label>();
            if (label != null)
                label.tooltip = attribute.name;
            else
                fieldElement.tooltip = attribute.name;

            // Context menu.
            fieldElement.AddManipulator(new ContextualMenuManipulator(BuildAttributeFieldContextualMenu));

            return styleRow;
        }

        object GetCustomValueAbstract(string attributeName)
        {
            if (currentVisualElement is ScrollView)
            {
                var scrollView = currentVisualElement as ScrollView;
                if (attributeName == "mode")
                {
                    if (scrollView.ClassListContains(ScrollView.verticalVariantUssClassName))
                        return ScrollViewMode.Vertical;
                    else if (scrollView.ClassListContains(ScrollView.horizontalVariantUssClassName))
                        return ScrollViewMode.Horizontal;
                    else if (scrollView.ClassListContains(ScrollView.verticalHorizontalVariantUssClassName))
                        return ScrollViewMode.VerticalAndHorizontal;
                }
                else if (attributeName == "show-horizontal-scroller")
                {
                    return scrollView.showHorizontal;
                }
                else if (attributeName == "show-vertical-scroller")
                {
                    return scrollView.showVertical;
                }
            }

            return null;
        }

        void RefreshAttributeField(BindableElement fieldElement)
        {
            var styleRow = fieldElement.GetProperty(BuilderConstants.InspectorLinkedStyleRowVEPropertyName) as VisualElement;
            var attribute = fieldElement.GetProperty(BuilderConstants.InspectorLinkedAttributeDescriptionVEPropertyName) as UxmlAttributeDescription;

            var veType = currentVisualElement.GetType();
            var camel = BuilderNameUtilities.ConvertDashToCamel(attribute.name);

            var fieldInfo = veType.GetProperty(camel, BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);

            object veValueAbstract = null;
            if (fieldInfo == null)
            {
                veValueAbstract = GetCustomValueAbstract(attribute.name);
            }
            else
            {
                veValueAbstract = fieldInfo.GetValue(currentVisualElement);
            }
            if (veValueAbstract == null)
                return;

            var attributeType = attribute.GetType();
            var vea = currentVisualElement.GetVisualElementAsset();

            if (attribute is UxmlStringAttributeDescription && fieldElement is TextField)
            {
                string value;
                if (veValueAbstract is Enum)
                    value = (veValueAbstract as Enum).ToString();
                else
                    value = (string)veValueAbstract;

                (fieldElement as TextField).SetValueWithoutNotify(value);
            }
            else if (attribute is UxmlFloatAttributeDescription && fieldElement is FloatField)
            {
                (fieldElement as FloatField).SetValueWithoutNotify((float)veValueAbstract);
            }
            else if (attribute is UxmlDoubleAttributeDescription && fieldElement is DoubleField)
            {
                (fieldElement as DoubleField).SetValueWithoutNotify((double)veValueAbstract);
            }
            else if (attribute is UxmlIntAttributeDescription && fieldElement is IntegerField)
            {
                (fieldElement as IntegerField).SetValueWithoutNotify((int)veValueAbstract);
            }
            else if (attribute is UxmlLongAttributeDescription && fieldElement is LongField)
            {
                (fieldElement as LongField).SetValueWithoutNotify((long)veValueAbstract);
            }
            else if (attribute is UxmlBoolAttributeDescription && fieldElement is Toggle)
            {
                (fieldElement as Toggle).SetValueWithoutNotify((bool)veValueAbstract);
            }
            else if (attribute is UxmlColorAttributeDescription && fieldElement is ColorField)
            {
                (fieldElement as ColorField).SetValueWithoutNotify((Color)veValueAbstract);
            }
            else if (attributeType.IsGenericType &&
                attributeType.GetGenericArguments()[0].IsEnum &&
                fieldElement is EnumField)
            {
                var propInfo = attributeType.GetProperty("defaultValue");
                var enumValue = propInfo.GetValue(attribute, null) as Enum;

                // Create and initialize the EnumField.
                var uiField = fieldElement as EnumField;

                // Set the value from the UXML attribute.
                var enumAttributeValueStr = vea?.GetAttributeValue(attribute.name);
                if (!string.IsNullOrEmpty(enumAttributeValueStr))
                {
                    var parsedValue = Enum.Parse(enumValue.GetType(), enumAttributeValueStr, true) as Enum;
                    uiField.SetValueWithoutNotify(parsedValue);
                }
            }
            else if (fieldElement is TextField)
            {
                (fieldElement as TextField).SetValueWithoutNotify(veValueAbstract.ToString());
            }

            // Determine if overridden.
            styleRow.RemoveFromClassList(BuilderConstants.InspectorLocalStyleOverrideClassName);
            if (vea != null && attribute.name == "picking-mode")
            {
                var veaAttributeValue = vea.GetAttributeValue(attribute.name);
                if (veaAttributeValue != null && veaAttributeValue.ToLower() != attribute.defaultValueAsString.ToLower())
                    styleRow.AddToClassList(BuilderConstants.InspectorLocalStyleOverrideClassName);
            }
            else if (attribute.name == "name")
            {
                if (!string.IsNullOrEmpty(currentVisualElement.name))
                    styleRow.AddToClassList(BuilderConstants.InspectorLocalStyleOverrideClassName);
            }
            else if (vea != null && vea.HasAttribute(attribute.name))
                styleRow.AddToClassList(BuilderConstants.InspectorLocalStyleOverrideClassName);
        }

        void ResetAttributeFieldToDefault(BindableElement fieldElement)
        {
            var styleRow = fieldElement.GetProperty(BuilderConstants.InspectorLinkedStyleRowVEPropertyName) as VisualElement;
            var attribute = fieldElement.GetProperty(BuilderConstants.InspectorLinkedAttributeDescriptionVEPropertyName) as UxmlAttributeDescription;

            var attributeType = attribute.GetType();
            var vea = currentVisualElement.GetVisualElementAsset();

            if (attribute is UxmlStringAttributeDescription && fieldElement is TextField)
            {
                var a = attribute as UxmlStringAttributeDescription;
                var f = fieldElement as TextField;
                f.SetValueWithoutNotify(a.defaultValue);
            }
            else if (attribute is UxmlFloatAttributeDescription && fieldElement is FloatField)
            {
                var a = attribute as UxmlFloatAttributeDescription;
                var f = fieldElement as FloatField;
                f.SetValueWithoutNotify(a.defaultValue);
            }
            else if (attribute is UxmlDoubleAttributeDescription && fieldElement is DoubleField)
            {
                var a = attribute as UxmlDoubleAttributeDescription;
                var f = fieldElement as DoubleField;
                f.SetValueWithoutNotify(a.defaultValue);
            }
            else if (attribute is UxmlIntAttributeDescription && fieldElement is IntegerField)
            {
                var a = attribute as UxmlIntAttributeDescription;
                var f = fieldElement as IntegerField;
                f.SetValueWithoutNotify(a.defaultValue);
            }
            else if (attribute is UxmlLongAttributeDescription && fieldElement is LongField)
            {
                var a = attribute as UxmlLongAttributeDescription;
                var f = fieldElement as LongField;
                f.SetValueWithoutNotify(a.defaultValue);
            }
            else if (attribute is UxmlBoolAttributeDescription && fieldElement is Toggle)
            {
                var a = attribute as UxmlBoolAttributeDescription;
                var f = fieldElement as Toggle;
                f.SetValueWithoutNotify(a.defaultValue);
            }
            else if (attribute is UxmlColorAttributeDescription && fieldElement is ColorField)
            {
                var a = attribute as UxmlColorAttributeDescription;
                var f = fieldElement as ColorField;
                f.SetValueWithoutNotify(a.defaultValue);
            }
            else if (attributeType.IsGenericType &&
                attributeType.GetGenericArguments()[0].IsEnum &&
                fieldElement is EnumField)
            {
                var propInfo = attributeType.GetProperty("defaultValue");
                var enumValue = propInfo.GetValue(attribute, null) as Enum;

                var uiField = fieldElement as EnumField;
                uiField.SetValueWithoutNotify(enumValue);
            }
            else if (fieldElement is TextField)
            {
                (fieldElement as TextField).SetValueWithoutNotify(string.Empty);
            }

            // Clear override.
            styleRow.RemoveFromClassList(BuilderConstants.InspectorLocalStyleOverrideClassName);
            var styleFields = styleRow.Query<BindableElement>().ToList();
            foreach (var styleField in styleFields)
            {
                styleField.RemoveFromClassList(BuilderConstants.InspectorLocalStyleResetClassName);
                styleField.RemoveFromClassList(BuilderConstants.InspectorLocalStyleOverrideClassName);
            }
        }

        void BuildAttributeFieldContextualMenu(ContextualMenuPopulateEvent evt)
        {
            evt.menu.AppendAction(
                BuilderConstants.ContextMenuUnsetMessage,
                UnsetAttributeProperty,
                DropdownMenuAction.AlwaysEnabled,
                evt.target);
            
            evt.menu.AppendAction(
                BuilderConstants.ContextMenuUnsetAllMessage,
                UnsetAllAttributes,
                DropdownMenuAction.AlwaysEnabled,
                evt.target);
        }

        void UnsetAllAttributes(DropdownMenuAction action)
        {
            var attributeList = currentVisualElement.GetAttributeDescriptions();

            // Undo/Redo
            Undo.RegisterCompleteObjectUndo(m_Inspector.visualTreeAsset, BuilderConstants.ChangeAttributeValueUndoMessage);
            
            foreach (var attribute in attributeList)
            {
                if (attribute?.name == null)
                    continue;
                
                // Unset value in asset.
                var vea = currentVisualElement.GetVisualElementAsset();
                vea.RemoveAttribute(attribute.name);
            }
            
            var fields = m_AttributesSection.Query<BindableElement>().Where(e => !string.IsNullOrEmpty(e.bindingPath)).ToList();
            foreach (var fieldElement in fields)
            {
                // Reset UI value.
                ResetAttributeFieldToDefault(fieldElement);
            }

            // Call Init();
            CallInitOnElement();

            // Notify of changes.
            m_Selection.NotifyOfHierarchyChange(m_Inspector);
        }

        void UnsetAttributeProperty(DropdownMenuAction action)
        {
            var fieldElement = action.userData as BindableElement;
            var attributeName = fieldElement.bindingPath;

            // Undo/Redo
            Undo.RegisterCompleteObjectUndo(m_Inspector.visualTreeAsset, BuilderConstants.ChangeAttributeValueUndoMessage);

            // Unset value in asset.
            var vea = currentVisualElement.GetVisualElementAsset();
            vea.RemoveAttribute(attributeName);

            // Reset UI value.
            ResetAttributeFieldToDefault(fieldElement);

            // Call Init();
            CallInitOnElement();

            // Notify of changes.
            m_Selection.NotifyOfHierarchyChange(m_Inspector);
        }

        void OnAttributeValueChange(ChangeEvent<string> evt)
        {
            var field = evt.target as TextField;
            PostAttributeValueChange(field, evt.newValue);
        }

        void OnAttributeValueChange(ChangeEvent<float> evt)
        {
            var field = evt.target as FloatField;
            PostAttributeValueChange(field, evt.newValue.ToString());
        }

        void OnAttributeValueChange(ChangeEvent<double> evt)
        {
            var field = evt.target as DoubleField;
            PostAttributeValueChange(field, evt.newValue.ToString());
        }

        void OnAttributeValueChange(ChangeEvent<int> evt)
        {
            var field = evt.target as IntegerField;
            PostAttributeValueChange(field, evt.newValue.ToString());
        }

        void OnAttributeValueChange(ChangeEvent<long> evt)
        {
            var field = evt.target as LongField;
            PostAttributeValueChange(field, evt.newValue.ToString());
        }

        void OnAttributeValueChange(ChangeEvent<bool> evt)
        {
            var field = evt.target as Toggle;
            PostAttributeValueChange(field, evt.newValue.ToString().ToLower());
        }

        void OnAttributeValueChange(ChangeEvent<Color> evt)
        {
            var field = evt.target as ColorField;
            PostAttributeValueChange(field, "#" + ColorUtility.ToHtmlStringRGBA(evt.newValue));
        }

        void OnAttributeValueChange(ChangeEvent<Enum> evt)
        {
            var field = evt.target as EnumField;
            PostAttributeValueChange(field, evt.newValue.ToString());
        }

        void PostAttributeValueChange(BindableElement field, string value)
        {
            // Undo/Redo
            Undo.RegisterCompleteObjectUndo(m_Inspector.visualTreeAsset, BuilderConstants.ChangeAttributeValueUndoMessage);

            // Set value in asset.
            var vea = currentVisualElement.GetVisualElementAsset();
            vea.SetAttributeValue(field.bindingPath, value);

            // Mark field as overridden.
            var styleRow = field.GetProperty(BuilderConstants.InspectorLinkedStyleRowVEPropertyName) as BuilderStyleRow;
            styleRow.AddToClassList(BuilderConstants.InspectorLocalStyleOverrideClassName);

            var styleFields = styleRow.Query<BindableElement>().ToList();
            
            foreach (var styleField in styleFields)
            {
                styleField.RemoveFromClassList(BuilderConstants.InspectorLocalStyleResetClassName);
                if (field.bindingPath == styleField.bindingPath)
                {
                    styleField.AddToClassList(BuilderConstants.InspectorLocalStyleOverrideClassName);
                }
                else if (!string.IsNullOrEmpty(styleField.bindingPath) &&
                    field.bindingPath != styleField.bindingPath &&
                    !styleField.ClassListContains(BuilderConstants.InspectorLocalStyleOverrideClassName))
                {
                    styleField.AddToClassList(BuilderConstants.InspectorLocalStyleResetClassName);
                }
            }

            // Call Init();
            CallInitOnElement();

            // Notify of changes.
            m_Selection.NotifyOfHierarchyChange(m_Inspector);
        }

        void CallInitOnElement()
        {
            var attributeList = new List<UxmlAttributeDescription>();
            var fullTypeName = currentVisualElement.GetType().ToString();
            List<IUxmlFactory> factoryList;
            if (VisualElementFactoryRegistry.TryGetValue(fullTypeName, out factoryList))
            {
                var factory = factoryList[0];
                var traitsField = factory.GetType().GetField("m_Traits", BindingFlags.Instance | BindingFlags.NonPublic);
                if (traitsField == null)
                {
                    Debug.LogError("UI Builder: IUxmlFactory.m_Traits field has not been found! Update the reflection code!");
                    return;
                }

                var traitObj = traitsField.GetValue(factory);
                var trait = traitObj as UxmlTraits;

                var context = new CreationContext();
                var vea = currentVisualElement.GetVisualElementAsset();

                try
                {
                    trait.Init(currentVisualElement, vea, context);
                }
                catch
                {
                    // HACK: This throws in 2019.3.0a4 because usageHints property throws when set after the element has already been added to the panel.
                }
            }
        }
    }
}
