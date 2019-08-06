using UnityEngine;
using UnityEngine.UIElements;
using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.UIElements;
using Object = UnityEngine.Object;

#if UNITY_2019_3_OR_NEWER
using UnityEngine.UIElements.StyleSheets;
#endif

namespace Unity.UI.Builder
{
    internal partial class BuilderInspector
    {
        private StyleProperty GetStyleProperty(StyleRule rule, string styleName)
        {
            foreach (var property in rule.properties)
            {
                if (property.name == styleName)
                    return property;
            }

            return null;
        }

        private PropertyInfo FindStylePropertyInfo(string styleName)
        {
            foreach (PropertyInfo field in StyleSheetUtilities.ComputedStylesFieldInfos)
            {
                var styleNameFrom = BuilderNameUtilities.ConverStyleCSharpNameToUssName(field.Name);
                if (styleNameFrom == styleName)
                    return field;
            }
            return null;
        }

        public void BindStyleField(BuilderStyleRow styleRow, string styleName, VisualElement fieldElement)
        {
#if UNITY_2019_2
            if (styleRow.ClassListContains(BuilderConstants.Version_2019_3_OrNewer))
            {
                styleRow.AddToClassList(BuilderConstants.HiddenStyleClassName);
                return;
            }
#else
            if (styleRow.ClassListContains(BuilderConstants.Version_2019_2))
            {
                styleRow.AddToClassList(BuilderConstants.HiddenStyleClassName);
                return;
            }
#endif

            var field = FindStylePropertyInfo(styleName);
            if (field == null)
                return;

            // Link the row.
            fieldElement.SetProperty(BuilderConstants.InspectorLinkedStyleRowVEPropertyName, styleRow);

            // We don't care which element we get the value for here as we're only interested
            // in the type of Enum it might be (and for validation), but not it's actual value.
            var val = field.GetValue(fieldElement.computedStyle, null);
            var valType = val.GetType();

            if (val is StyleFloat && fieldElement is FloatField)
            {
                var uiField = fieldElement as FloatField;
                uiField.RegisterValueChangedCallback(e => OnFieldValueChange(e, styleName));
            }
            else if (val is StyleFloat && fieldElement is IntegerField)
            {
                var uiField = fieldElement as IntegerField;

#if UNITY_2019_3_OR_NEWER
                if (BuilderConstants.SpecialSnowflakeLengthSytles.Contains(styleName))
                    uiField.RegisterValueChangedCallback(e => OnFieldDimensionChange(e, styleName));
                else
#endif
                    uiField.RegisterValueChangedCallback(e => OnFieldValueChangeIntToFloat(e, styleName));
            }
            else if (val is StyleInt && fieldElement is IntegerField)
            {
                var uiField = fieldElement as IntegerField;
                uiField.RegisterValueChangedCallback(e => OnFieldValueChange(e, styleName));
            }
            else if (val is StyleLength && fieldElement is IntegerField)
            {
                var uiField = fieldElement as IntegerField;
#if UNITY_2019_3_OR_NEWER
                uiField.RegisterValueChangedCallback(e => OnFieldDimensionChange(e, styleName));
#else
                uiField.RegisterValueChangedCallback(e => OnFieldValueChange(e, styleName));
#endif
            }
            else if (val is StyleColor && fieldElement is ColorField)
            {
                var uiField = fieldElement as ColorField;
                uiField.RegisterValueChangedCallback(e => OnFieldValueChange(e, styleName));
            }
            else if (val is StyleFont && fieldElement is ObjectField)
            {
                var uiField = fieldElement as ObjectField;
                uiField.objectType = typeof(Font);
                uiField.RegisterValueChangedCallback(e => OnFieldValueChange(e, styleName));
            }
            else if (val is StyleBackground && fieldElement is ObjectField)
            {
                var uiField = fieldElement as ObjectField;
                uiField.objectType = typeof(Texture2D);
                uiField.RegisterValueChangedCallback(e => OnFieldValueChange(e, styleName));
            }
            else if (val is StyleCursor && fieldElement is ObjectField)
            {
                var uiField = fieldElement as ObjectField;
                uiField.objectType = typeof(Texture2D);
                uiField.RegisterValueChangedCallback(e => OnFieldValueChange(e, styleName));
            }
            else if (valType.IsGenericType && valType.GetGenericArguments()[0].IsEnum && fieldElement is EnumField)
            {
                var propInfo = valType.GetProperty("value");
                var enumValue = propInfo.GetValue(val, null) as Enum;
                var uiField = fieldElement as EnumField;

                uiField.Init(enumValue);
                uiField.RegisterValueChangedCallback(e => OnFieldValueChange(e, styleName));
            }
            else
            {
                // Unsupported style value type.
                return;
            }

            fieldElement.SetProperty(BuilderConstants.InspectorStylePropertyNameVEPropertyName, styleName);
            fieldElement.SetProperty(BuilderConstants.InspectorComputedStylePropertyInfoVEPropertyName, field);
            fieldElement.AddManipulator(new ContextualMenuManipulator(BuildStyleFieldContextualMenu));

            // Add to styleName to field map.
            if (!m_StyleFields.ContainsKey(styleName))
            {
                var newList = new List<VisualElement>();
                newList.Add(fieldElement);
                m_StyleFields.Add(styleName, newList);
            }
            else
            {
                m_StyleFields[styleName].Add(fieldElement);
            }
        }

        public void BindDoubleFieldRow(BuilderStyleRow styleRow)
        {
            var styleFields = styleRow.Query<BindableElement>().ToList()
                .Where(element => !string.IsNullOrEmpty(element.bindingPath)).ToList();
            if (styleFields.Count > 0)
            {
                var headerLabel = styleRow.Q<Label>(classes: "unity-builder-double-field-label");
                headerLabel.AddManipulator(new ContextualMenuManipulator(action =>
                {
                    (action.target as VisualElement).userData = styleFields;
                    BuildStyleFieldContextualMenu(action);
                }));
            }
        }

        public void BindStyleField(BuilderStyleRow styleRow, PersistedFoldoutWithField foldoutElement)
        {
            var intFields = foldoutElement.Query<IntegerField>().ToList();

            foreach (var field in intFields)
            {
                field.SetProperty(BuilderConstants.PersistedFoldoutWithFieldPropertyName, foldoutElement);
                field.RegisterValueChangedCallback((evt) =>
                {
                    var row = field.GetProperty(BuilderConstants.InspectorLinkedStyleRowVEPropertyName) as BuilderStyleRow;
                    if (row != null && !string.IsNullOrEmpty(row.bindingPath))
                        foldoutElement.UpdateFromChildField(row.bindingPath, field.value.ToString());

                    foldoutElement.header.AddToClassList(s_LocalStyleOverrideClassName);
                });

                BuilderStyleRow.ReAssignTooltipToChild(field);
            }
            foldoutElement.header.SetProperty(BuilderConstants.PersistedFoldoutWithFieldPropertyName, foldoutElement);
            foldoutElement.headerInputField.RegisterValueChangedCallback(PersistedFoldoutWithFieldOnValueChange);

            foldoutElement.headerInputField.AddManipulator(
                new ContextualMenuManipulator((e) =>
                {
                    e.target = foldoutElement.header;
                    BuildStyleFieldContextualMenu(e);
                }));

            foldoutElement.header.AddManipulator(
                new ContextualMenuManipulator(BuildStyleFieldContextualMenu));
        }

        void PersistedFoldoutWithFieldOnValueChange(ChangeEvent<string> evt)
        {
            var target = evt.target as TextField;
            var foldoutElement = target.GetFirstAncestorOfType<PersistedFoldoutWithField>();

            if (!string.IsNullOrEmpty(evt.newValue))
            {
                if (foldoutElement.IsValidInput(evt.newValue))
                {
                    foldoutElement.header.AddToClassList(s_LocalStyleOverrideClassName);
                    foldoutElement.headerInputField.value = foldoutElement.GetFormattedInputString();
                    for (int i = 0; i < foldoutElement.bindingPathArray.Length; i++)
                    {
                        var styleProperty = GetStylePropertyByStyleName(foldoutElement.bindingPathArray[i]);

#if UNITY_2019_3_OR_NEWER
                            if (BuilderConstants.SpecialSnowflakeLengthSytles.Contains(foldoutElement.bindingPathArray[i]))
                            {
                                var newValue = 0;
                                // TryParse to check if fieldValue is not a style string like "auto"
                                if (Int32.TryParse(foldoutElement.fieldValues[i], out newValue))
                                {
                                    var dimension = new Dimension();
                                    dimension.unit = Dimension.Unit.Pixel;
                                    dimension.value = newValue;

                                    if (styleProperty.values.Length == 0)
                                        styleSheet.AddValue(styleProperty, dimension);
                                    else // TODO: Assume only one value.
                                        styleSheet.SetValue(styleProperty.values[0], dimension);
                                }
                            }
                            else
#endif
                        {
                            var newValue = 0;
                            // TryParse to check if fieldValue is not a style string like "auto"
                            if (Int32.TryParse(foldoutElement.fieldValues[i], out newValue))
                            {
                                var convertedFloat = (float)newValue;

                                if (styleProperty.values.Length == 0)
                                    styleSheet.AddValue(styleProperty, convertedFloat);
                                else // TODO: Assume only one value.
                                    styleSheet.SetValue(styleProperty.values[0], convertedFloat);
                            }
                        }

                        NotifyStyleChanges();
                    }

                    StylingChanged(foldoutElement.bindingPathArray.ToList());
                }
                else
                    foldoutElement.headerInputField.SetValueWithoutNotify(foldoutElement.lastValidInput);
            }

            evt.StopPropagation();
        }

        public void RefreshStyleField(string styleName, VisualElement fieldElement)
        {
            var field = FindStylePropertyInfo(styleName);
            if (field == null)
                return;

            var val = field.GetValue(currentVisualElement.computedStyle, null);
            var valType = val.GetType();
            var styleProperty = GetStyleProperty(currentRule, styleName);

            if (val is StyleFloat && fieldElement is FloatField)
            {
                var style = (StyleFloat)val;
                var uiField = fieldElement as FloatField;

                var value = style.value;
                if (styleProperty != null)
                    value = styleSheet.GetFloat(styleProperty.values[0]);

                uiField.SetValueWithoutNotify(value);
            }
            else if (val is StyleFloat && fieldElement is IntegerField)
            {
                var style = (StyleFloat)val;
                var uiField = fieldElement as IntegerField;

                var value = (int)style.value;
                if (styleProperty != null)
                    value = (int)styleSheet.GetFloat(styleProperty.values[0]);

                uiField.SetValueWithoutNotify(value);
            }
            else if (val is StyleInt && fieldElement is IntegerField)
            {
                var style = (StyleInt)val;
                var uiField = fieldElement as IntegerField;

                var value = style.value;
                if (styleProperty != null)
                    value = styleSheet.GetInt(styleProperty.values[0]);

                uiField.SetValueWithoutNotify(value);
            }
            else if (val is StyleLength && fieldElement is IntegerField)
            {
                var style = (StyleLength)val;
                var uiField = fieldElement as IntegerField;

                var value = (int)style.value.value;
                if (styleProperty != null)
#if UNITY_2019_3_OR_NEWER
                    value = (int)styleSheet.GetDimension(styleProperty.values[0]).value;
#else
                    value = styleSheet.GetInt(styleProperty.values[0]);
#endif

                uiField.SetValueWithoutNotify(value);
            }
            else if (val is StyleColor && fieldElement is ColorField)
            {
                var style = (StyleColor)val;
                var uiField = fieldElement as ColorField;

                var value = style.value;
                if (styleProperty != null)
                    value = styleSheet.GetColor(styleProperty.values[0]);

                // We keep falling into the alpha==0 trap. This patches the issue a little.
                if (value.a < 0.1f && style.specificity == 0)
                    value.a = 255.0f;

                uiField.SetValueWithoutNotify(value);
            }
            else if (val is StyleFont && fieldElement is ObjectField)
            {
                var style = (StyleFont)val;
                var uiField = fieldElement as ObjectField;

                var value = style.value;
                if (styleProperty != null)
                    value = styleSheet.GetAsset(styleProperty.values[0]) as Font;

                uiField.SetValueWithoutNotify(value);
            }
            else if (val is StyleBackground && fieldElement is ObjectField)
            {
                var style = (StyleBackground)val;
                var uiField = fieldElement as ObjectField;

                var value = style.value;
                if (styleProperty != null)
                    value.texture = styleSheet.GetAsset(styleProperty.values[0]) as Texture2D;

                uiField.SetValueWithoutNotify(value.texture);
            }
            else if (val is StyleCursor && fieldElement is ObjectField)
            {
                var style = (StyleCursor)val;
                var uiField = fieldElement as ObjectField;

                var value = style.value;
                if (styleProperty != null)
                    value.texture = styleSheet.GetAsset(styleProperty.values[0]) as Texture2D;

                uiField.SetValueWithoutNotify(value.texture);
            }
            else if (valType.IsGenericType && valType.GetGenericArguments()[0].IsEnum && fieldElement is EnumField)
            {
                var propInfo = valType.GetProperty("value");
                var enumValue = propInfo.GetValue(val, null) as Enum;
                var uiField = fieldElement as EnumField;

                if (styleProperty != null)
                {
                    var enumStr = styleSheet.GetEnum(styleProperty.values[0]);
                    enumStr = BuilderNameUtilities.ConvertDashToHungarian(enumStr);
                    var enumObj = Enum.Parse(enumValue.GetType(), enumStr);
                    enumValue = enumObj as Enum;
                }

                uiField.SetValueWithoutNotify(enumValue);
            }
            else
            {
                // Unsupported style value type.
                return;
            }

            // Add override style to field if it is overwritten.
            var styleRow = fieldElement.GetProperty(BuilderConstants.InspectorLinkedStyleRowVEPropertyName) as BuilderStyleRow;
            var styleFields = styleRow.Query<BindableElement>().ToList();

            bool isRowOverride = false;
            foreach (var styleField in styleFields)
            {
                if (GetStyleProperty(currentRule, styleField.bindingPath) != null)
                {
                    isRowOverride = true;
                    styleField.RemoveFromClassList(s_LocalStyleResetClassName);
                    styleField.AddToClassList(s_LocalStyleOverrideClassName);
                }
                else if (!string.IsNullOrEmpty(styleField.bindingPath))
                {
                    styleField.AddToClassList(s_LocalStyleResetClassName);
                }
            }

            if (styleProperty != null || isRowOverride)
                styleRow.AddToClassList(s_LocalStyleOverrideClassName);
            else
            {
                styleRow.RemoveFromClassList(s_LocalStyleOverrideClassName);
                foreach (var styleField in styleFields)
                {
                    styleField.RemoveFromClassList(s_LocalStyleOverrideClassName);
                    styleField.RemoveFromClassList(s_LocalStyleResetClassName);
                }
            }
        }

        public void RefreshStyleField(PersistedFoldoutWithField foldoutElement)
        {
            var isDirty = false;

            foreach (var path in foldoutElement.bindingPathArray)
            {
                var styleProperty = GetStyleProperty(currentRule, path);

                var field = FindStylePropertyInfo(path);
                if (field == null)
                    continue;

                var val = field.GetValue(currentVisualElement.computedStyle, null);
                if (val is StyleFloat)
                {
                    var style = (StyleFloat)val;
                    var value = style.value;

                    if (styleProperty != null)
                    {
                        isDirty = true;
                        value = styleSheet.GetFloat(styleProperty.values[0]);
                    }

                    foldoutElement.UpdateFromChildField(path, value.ToString());
                }
                else if (val is StyleLength)
                {
                    var style = (StyleLength)val;
                    var value = (int)style.value.value;

                    if (styleProperty != null)
                    {
                        isDirty = true;

#if UNITY_2019_3_OR_NEWER
                        value = (int)styleSheet.GetDimension(styleProperty.values[0]).value;
#else
                        value = styleSheet.GetInt(styleProperty.values[0]);
#endif
                    }

                    foldoutElement.UpdateFromChildField(path, value.ToString());
                }
            }

            if (isDirty)
                foldoutElement.header.AddToClassList(s_LocalStyleOverrideClassName);
            else
                foldoutElement.header.RemoveFromClassList(s_LocalStyleOverrideClassName);
        }

        private void BuildStyleFieldContextualMenu(ContextualMenuPopulateEvent evt)
        {
            evt.menu.AppendAction(
                BuilderConstants.ContextMenuUnsetMessage,
                UnsetStyleProperty,
                DropdownMenuAction.AlwaysEnabled,
                evt.target);
        }

        private void UnsetStyleProperty(DropdownMenuAction action)
        {
            var listToUnset = (action.userData as VisualElement).userData;
            if (listToUnset != null && listToUnset is List<BindableElement>)
            {
                foreach (var bindableElement in listToUnset as List<BindableElement>)
                {
                    var beStyleName = bindableElement.GetProperty(BuilderConstants.InspectorStylePropertyNameVEPropertyName) as string;
                    schedule.Execute(() => RefreshStyleField(beStyleName, bindableElement));
                    styleSheet.RemoveProperty(currentRule, beStyleName);
                }

                NotifyStyleChanges();
                return;
            }

            var fieldElement = action.userData as VisualElement;
            var styleName = fieldElement.GetProperty(BuilderConstants.InspectorStylePropertyNameVEPropertyName) as string;

            // TODO: The computed style still has the old (set) value at this point.
            // We need to reset the field with the value after styling has been
            // recomputed. Just execute next frame for now.
            schedule.Execute(() => RefreshStyleField(styleName, fieldElement));

            styleSheet.RemoveProperty(currentRule, styleName);

            var foldout = fieldElement.GetProperty(BuilderConstants.PersistedFoldoutWithFieldPropertyName) as PersistedFoldoutWithField;
            if (foldout != null)
            {
                // Check if the unset element was the header field.
                if (fieldElement.ClassListContains(BuilderConstants.PersistedFoldoutWithFieldHeaderClassName))
                {
                    foreach (var path in foldout.bindingPathArray)
                    {
                        foreach (var linkedField in m_StyleFields[path])
                            schedule.Execute(() => RefreshStyleField(path, linkedField));
                        styleSheet.RemoveProperty(currentRule, path);
                    }

                    schedule.Execute(() => RefreshStyleField(foldout));
                }
                else
                    // The unset element was a child of a PersistedFoldoutWithField
                    schedule.Execute(() => RefreshStyleField(foldout));
            }

            NotifyStyleChanges();
        }

        private void NotifyStyleChanges()
        {
            if (BuilderSharedStyles.IsSelectorElement(currentVisualElement))
                m_Selection.NotifyOfStylingChange(this);
            else
                m_Selection.NotifyOfHierarchyChange(this, currentVisualElement, BuilderHierarchyChangeType.InlineStyle);
        }

        // Style Updates

        private StyleProperty GetStylePropertyByStyleName(string styleName)
        {
            var styleProperty = styleSheet.FindProperty(currentRule, styleName);
            if (styleProperty == null)
                styleProperty = styleSheet.AddProperty(currentRule, styleName);

            return styleProperty;
        }

        private void PostStyleFieldSteps(VisualElement target)
        {
            var styleRow = target.GetProperty(BuilderConstants.InspectorLinkedStyleRowVEPropertyName) as BuilderStyleRow;
            styleRow.AddToClassList(s_LocalStyleOverrideClassName);

            // Remove temporary min-size class on VisualElement.
            currentVisualElement.RemoveMinSizeSpecialElement();

            var styleFields = styleRow.Query<BindableElement>().ToList();
            
            var bindableElement = target as BindableElement;
            foreach (var styleField in styleFields)
            {
                styleField.RemoveFromClassList(s_LocalStyleResetClassName);
                if (bindableElement.bindingPath == styleField.bindingPath)
                {
                    styleField.AddToClassList(s_LocalStyleOverrideClassName);
                }
                else if (!string.IsNullOrEmpty(styleField.bindingPath) &&
                    bindableElement.bindingPath != styleField.bindingPath &&
                    !styleField.ClassListContains(s_LocalStyleOverrideClassName))
                {
                    styleField.AddToClassList(s_LocalStyleResetClassName);
                }
            }

            NotifyStyleChanges();
        }

#if UNITY_2019_3_OR_NEWER
        private void OnFieldDimensionChange(ChangeEvent<int> e, string styleName)
        {
            var styleProperty = GetStylePropertyByStyleName(styleName);

            var dimension = new Dimension();
            dimension.unit = Dimension.Unit.Pixel;
            dimension.value = e.newValue;

            if (styleProperty.values.Length == 0)
                styleSheet.AddValue(styleProperty, dimension);
            else // TODO: Assume only one value.
                styleSheet.SetValue(styleProperty.values[0], dimension);

            PostStyleFieldSteps(e.target as VisualElement);
        }
#endif

        private void OnFieldValueChange(ChangeEvent<int> e, string styleName)
        {
            var styleProperty = GetStylePropertyByStyleName(styleName);

            if (styleProperty.values.Length == 0)
                styleSheet.AddValue(styleProperty, e.newValue);
            else // TODO: Assume only one value.
                styleSheet.SetValue(styleProperty.values[0], e.newValue);

            PostStyleFieldSteps(e.target as VisualElement);
        }

        private void OnFieldValueChangeIntToFloat(ChangeEvent<int> e, string styleName)
        {
            var styleProperty = GetStylePropertyByStyleName(styleName);

            var newValue = (float)e.newValue;

            if (styleProperty.values.Length == 0)
                styleSheet.AddValue(styleProperty, newValue);
            else // TODO: Assume only one value.
                styleSheet.SetValue(styleProperty.values[0], newValue);

            PostStyleFieldSteps(e.target as VisualElement);
        }

        private void OnFieldValueChange(ChangeEvent<float> e, string styleName)
        {
            var styleProperty = GetStylePropertyByStyleName(styleName);

            if (styleProperty.values.Length == 0)
                styleSheet.AddValue(styleProperty, e.newValue);
            else // TODO: Assume only one value.
                styleSheet.SetValue(styleProperty.values[0], e.newValue);

            PostStyleFieldSteps(e.target as VisualElement);
        }

        private void OnFieldValueChange(ChangeEvent<Color> e, string styleName)
        {
            var styleProperty = GetStylePropertyByStyleName(styleName);

            if (styleProperty.values.Length == 0)
                styleSheet.AddValue(styleProperty, e.newValue);
            else // TODO: Assume only one value.
                styleSheet.SetValue(styleProperty.values[0], e.newValue);

            PostStyleFieldSteps(e.target as VisualElement);
        }

        private void OnFieldValueChange(ChangeEvent<string> e, string styleName)
        {
            var styleProperty = GetStylePropertyByStyleName(styleName);

            if (styleProperty.values.Length == 0)
                styleSheet.AddValue(styleProperty, e.newValue);
            else // TODO: Assume only one value.
                styleSheet.SetValue(styleProperty.values[0], e.newValue);

            PostStyleFieldSteps(e.target as VisualElement);
        }

        private void OnFieldValueChange(ChangeEvent<Object> e, string styleName)
        {
            var styleProperty = GetStylePropertyByStyleName(styleName);

            if (styleProperty.values.Length == 0)
                styleSheet.AddValue(styleProperty, e.newValue);
            else // TODO: Assume only one value.
                styleSheet.SetValue(styleProperty.values[0], e.newValue);

            PostStyleFieldSteps(e.target as VisualElement);
        }

        private void OnFieldValueChange(ChangeEvent<Enum> e, string styleName)
        {
            var styleProperty = GetStylePropertyByStyleName(styleName);

            if (styleProperty.values.Length == 0)
                styleSheet.AddValue(styleProperty, e.newValue);
            else // TODO: Assume only one value.
                styleSheet.SetValue(styleProperty.values[0], e.newValue);

            PostStyleFieldSteps(e.target as VisualElement);
        }
    }
}
