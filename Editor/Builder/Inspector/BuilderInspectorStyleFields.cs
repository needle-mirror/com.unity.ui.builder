using UnityEngine;
using UnityEngine.UIElements;
using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.UIElements;
using Object = UnityEngine.Object;
using UnityEngine.Assertions;

#if UNITY_2019_3_OR_NEWER
using UnityEngine.UIElements.StyleSheets;
#endif

namespace Unity.UI.Builder
{
    internal class BuilderInspectorStyleFields
    {
        private BuilderInspector m_Inspector;
        private BuilderSelection m_Selection;

        private Dictionary<string, List<VisualElement>> m_StyleFields;

        private List<string> s_StyleChangeList = new List<string>();

        VisualElement currentVisualElement => m_Inspector.currentVisualElement;
        StyleSheet styleSheet => m_Inspector.styleSheet;
        StyleRule currentRule => m_Inspector.currentRule;

        public Action<Enum> updateFlexColumnGlobalState { get; set; }

        public Action updateStyleCategoryFoldoutOverrides { get; set; }

        public BuilderInspectorStyleFields(BuilderInspector inspector)
        {
            m_Inspector = inspector;
            m_Selection = inspector.selection;

            m_StyleFields = new Dictionary<string, List<VisualElement>>();
        }

        public List<VisualElement> GetFieldListForStyleName(string styleName)
        {
            List<VisualElement> fieldList;
            m_StyleFields.TryGetValue(styleName, out fieldList);
            return fieldList;
        }

        private StyleProperty GetStyleProperty(StyleRule rule, string styleName)
        {
            if (rule == null)
                return null;

            foreach (var property in rule.properties)
            {
                if (property.name == styleName)
                    return property;
            }

            return null;
        }

        private string ConvertUssStyleNameToCSharpStyleName(string ussStyleName)
        {
            if (ussStyleName == "-unity-font-style")
                return "-unity-font-style-and-weight";

            return ussStyleName;
        }

        private PropertyInfo FindStylePropertyInfo(string styleName)
        {
            var cSharpStyleName = ConvertUssStyleNameToCSharpStyleName(styleName);

            foreach (PropertyInfo field in StyleSheetUtilities.ComputedStylesFieldInfos)
            {
                var styleNameFrom = BuilderNameUtilities.ConverStyleCSharpNameToUssName(field.Name);
                if (styleNameFrom == cSharpStyleName)
                    return field;
            }
            return null;
        }

        public void BindStyleField(BuilderStyleRow styleRow, string styleName, VisualElement fieldElement)
        {
            // Link the row.
            fieldElement.SetProperty(BuilderConstants.InspectorLinkedStyleRowVEPropertyName, styleRow);

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
            else if (val is StyleFloat && fieldElement is PercentSlider)
            {
                var uiField = fieldElement as PercentSlider;
                uiField.RegisterValueChangedCallback(e => OnFieldValueChange(e, styleName));
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
                uiField.RegisterValueChangedCallback(e => OnFieldValueChangeFont(e, styleName));
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
            else if (valType.IsGenericType && valType.GetGenericArguments()[0].IsEnum)
            {
                var propInfo = valType.GetProperty("value");
                var enumValue = propInfo.GetValue(val, null) as Enum;

                if (fieldElement is EnumField)
                {
                    var uiField = fieldElement as EnumField;

                    uiField.Init(enumValue);
                    uiField.RegisterValueChangedCallback(e => OnFieldValueChange(e, styleName));
                }
                else if (fieldElement is IToggleButtonStrip)
                {
                    var uiField = fieldElement as IToggleButtonStrip;

                    var choices = new List<string>();
                    var labels = new List<string>();
                    var enumType = enumValue.GetType();
                    foreach (Enum item in Enum.GetValues(enumType))
                    {
                        var typeName = item.ToString();
                        var label = string.Empty;
                        if (typeName == "Auto")
                            label = "AUTO";
                        var choice = BuilderNameUtilities.ConvertCamelToDash(typeName);
                        choices.Add(choice);
                        labels.Add(label);
                    }

                    uiField.enumType = enumType;
                    uiField.choices = choices;
                    uiField.labels = labels;
                    uiField.RegisterValueChangedCallback(e => OnFieldValueChange(e, styleName));
                }
                else
                {
                    // Unsupported style value type.
                    return;
                }
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

                    foldoutElement.header.AddToClassList(BuilderConstants.InspectorLocalStyleOverrideClassName);
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
                    foldoutElement.header.AddToClassList(BuilderConstants.InspectorLocalStyleOverrideClassName);
                    foldoutElement.headerInputField.value = foldoutElement.GetFormattedInputString();
                    for (int i = 0; i < foldoutElement.bindingPathArray.Length; i++)
                    {
                        var styleName = foldoutElement.bindingPathArray[i];

                        var styleProperty = GetStylePropertyByStyleName(styleName);

#if UNITY_2019_3_OR_NEWER
                        var field = FindStylePropertyInfo(styleName);

                        // We don't care which element we get the value for here as we're only interested
                        // in the type of Enum it might be (and for validation), but not it's actual value.
                        var val = field?.GetValue(foldoutElement.computedStyle, null);

                        if ((val != null && val is StyleLength) ||
                            BuilderConstants.SpecialSnowflakeLengthSytles.Contains(foldoutElement.bindingPathArray[i]))
                        {
                            var newValue = 0;
                            // TryParse to check if fieldValue is not a style string like "auto"
                            if (Int32.TryParse(foldoutElement.fieldValues[i], out newValue))
                            {
                                var isNewValue = styleProperty.values.Length == 0;

                                // If the current style property is saved as a float instead of a dimension,
                                // it means it's a user file where they left out the unit. We need to resave
                                // it here as a dimension to create final proper uss.
                                if (!isNewValue && styleProperty.values[0].valueType != StyleValueType.Dimension)
                                {
                                    styleProperty.values = new StyleValueHandle[0];
                                    isNewValue = true;
                                }

                                var dimension = new Dimension();
                                dimension.unit = Dimension.Unit.Pixel;
                                dimension.value = newValue;

                                if (isNewValue)
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

                    m_Inspector.StylingChanged(foldoutElement.bindingPathArray.ToList());
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
            var cSharpStyleName = ConvertUssStyleNameToCSharpStyleName(styleName);
            var styleProperty = GetStyleProperty(currentRule, cSharpStyleName);

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
            else if (val is StyleFloat && fieldElement is PercentSlider)
            {
                var style = (StyleFloat)val;
                var uiField = fieldElement as PercentSlider;

                var value = style.value;
                if (styleProperty != null)
                    value = styleSheet.GetFloat(styleProperty.values[0]);

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
            else if (valType.IsGenericType && valType.GetGenericArguments()[0].IsEnum)
            {
                var propInfo = valType.GetProperty("value");
                var enumValue = propInfo.GetValue(val, null) as Enum;

                if (styleProperty != null)
                {
                    var enumStr = styleSheet.GetEnum(styleProperty.values[0]);
                    var enumStrHungarian = BuilderNameUtilities.ConvertDashToHungarian(enumStr);
                    var enumObj = Enum.Parse(enumValue.GetType(), enumStrHungarian);
                    enumValue = enumObj as Enum;
                }

                // The state of Flex Direction can affect many other Flex-related fields.
                if (styleName == "flex-direction")
                    updateFlexColumnGlobalState?.Invoke(enumValue);

                if (fieldElement is EnumField)
                {
                    var uiField = fieldElement as EnumField;
                    uiField.SetValueWithoutNotify(enumValue);
                }
                else if (fieldElement is IToggleButtonStrip)
                {
                    var enumStr = BuilderNameUtilities.ConvertCamelToDash(enumValue.ToString());
                    var uiField = fieldElement as IToggleButtonStrip;
                    uiField.SetValueWithoutNotify(enumStr);
                }
                else
                {
                    // Unsupported style value type.
                    return;
                }
            }
            else
            {
                // Unsupported style value type.
                return;
            }

            // Add override style to field if it is overwritten.
            var styleRow = fieldElement.GetProperty(BuilderConstants.InspectorLinkedStyleRowVEPropertyName) as BuilderStyleRow;
            Assert.IsNotNull(styleRow);
            if (styleRow == null)
                return;
            var styleFields = styleRow.Query<BindableElement>().ToList();

            bool isRowOverride = false;
            foreach (var styleField in styleFields)
            {
                var cShartStyleName = ConvertUssStyleNameToCSharpStyleName(styleField.bindingPath);
                if (GetStyleProperty(currentRule, cShartStyleName) != null)
                {
                    isRowOverride = true;
                    styleField.RemoveFromClassList(BuilderConstants.InspectorLocalStyleResetClassName);
                    styleField.AddToClassList(BuilderConstants.InspectorLocalStyleOverrideClassName);
                }
                else if (!string.IsNullOrEmpty(styleField.bindingPath))
                {
                    styleField.AddToClassList(BuilderConstants.InspectorLocalStyleResetClassName);
                }
            }

            if (styleProperty != null || isRowOverride)
                styleRow.AddToClassList(BuilderConstants.InspectorLocalStyleOverrideClassName);
            else
            {
                styleRow.RemoveFromClassList(BuilderConstants.InspectorLocalStyleOverrideClassName);
                foreach (var styleField in styleFields)
                {
                    styleField.RemoveFromClassList(BuilderConstants.InspectorLocalStyleOverrideClassName);
                    styleField.RemoveFromClassList(BuilderConstants.InspectorLocalStyleResetClassName);
                }
            }
        }

        public void RefreshStyleField(PersistedFoldoutWithField foldoutElement)
        {
            var isDirty = false;

            foreach (var path in foldoutElement.bindingPathArray)
            {
                var cSharpStyleName = ConvertUssStyleNameToCSharpStyleName(path);
                var styleProperty = GetStyleProperty(currentRule, cSharpStyleName);

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
                else if (val is StyleInt)
                {
                    var style = (StyleInt)val;
                    var value = style.value;

                    if (styleProperty != null)
                    {
                        isDirty = true;
                        value = styleSheet.GetInt(styleProperty.values[0]);
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
                foldoutElement.header.AddToClassList(BuilderConstants.InspectorLocalStyleOverrideClassName);
            else
                foldoutElement.header.RemoveFromClassList(BuilderConstants.InspectorLocalStyleOverrideClassName);
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
                    m_Inspector.schedule.Execute(() =>
                    {
                        RefreshStyleField(beStyleName, bindableElement);
                        updateStyleCategoryFoldoutOverrides?.Invoke();
                    });
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
            m_Inspector.schedule.Execute(() =>
            {
                RefreshStyleField(styleName, fieldElement);
                updateStyleCategoryFoldoutOverrides?.Invoke();
            });

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
                            m_Inspector.schedule.Execute(() =>
                            {
                                RefreshStyleField(path, linkedField);
                                updateStyleCategoryFoldoutOverrides?.Invoke();
                            });
                        styleSheet.RemoveProperty(currentRule, path);
                    }

                    m_Inspector.schedule.Execute(() =>
                    {
                        RefreshStyleField(foldout);
                        updateStyleCategoryFoldoutOverrides?.Invoke();
                    });
                }
                else
                    // The unset element was a child of a PersistedFoldoutWithField
                    m_Inspector.schedule.Execute(() =>
                    {
                        RefreshStyleField(foldout);
                        updateStyleCategoryFoldoutOverrides?.Invoke();
                    });
            }

            NotifyStyleChanges();
        }

        private void NotifyStyleChanges(List<string> styles = null)
        {
            if (BuilderSharedStyles.IsSelectorElement(currentVisualElement))
            {
                m_Selection.NotifyOfStylingChange(m_Inspector, styles);
            }
            else
            {
                m_Selection.NotifyOfStylingChange(m_Inspector, styles);
                m_Selection.NotifyOfHierarchyChange(m_Inspector, currentVisualElement, BuilderHierarchyChangeType.InlineStyle);
            }
        }

        // Style Updates

        private StyleProperty GetStylePropertyByStyleName(string styleName)
        {
            var styleProperty = styleSheet.FindProperty(currentRule, styleName);
            if (styleProperty == null)
                styleProperty = styleSheet.AddProperty(currentRule, styleName);

            return styleProperty;
        }

        private void PostStyleFieldSteps(VisualElement target, string styleName, bool isNewValue)
        {
            var styleRow = target.GetProperty(BuilderConstants.InspectorLinkedStyleRowVEPropertyName) as BuilderStyleRow;
            styleRow.AddToClassList(BuilderConstants.InspectorLocalStyleOverrideClassName);

            // Remove temporary min-size class on VisualElement.
            currentVisualElement.RemoveMinSizeSpecialElement();

            var styleFields = styleRow.Query<BindableElement>().ToList();
            
            var bindableElement = target as BindableElement;
            foreach (var styleField in styleFields)
            {
                styleField.RemoveFromClassList(BuilderConstants.InspectorLocalStyleResetClassName);
                if (bindableElement.bindingPath == styleField.bindingPath)
                {
                    styleField.AddToClassList(BuilderConstants.InspectorLocalStyleOverrideClassName);
                }
                else if (!string.IsNullOrEmpty(styleField.bindingPath) &&
                    bindableElement.bindingPath != styleField.bindingPath &&
                    !styleField.ClassListContains(BuilderConstants.InspectorLocalStyleOverrideClassName))
                {
                    styleField.AddToClassList(BuilderConstants.InspectorLocalStyleResetClassName);
                }
            }

            s_StyleChangeList.Clear();
            s_StyleChangeList.Add(styleName);
            NotifyStyleChanges(s_StyleChangeList);

            if (isNewValue && updateStyleCategoryFoldoutOverrides != null)
                updateStyleCategoryFoldoutOverrides();
        }

#if UNITY_2019_3_OR_NEWER
        private void OnFieldDimensionChange(ChangeEvent<int> e, string styleName)
        {
            var styleProperty = GetStylePropertyByStyleName(styleName);
            var isNewValue = styleProperty.values.Length == 0;

            // If the current style property is saved as a float instead of a dimension,
            // it means it's a user file where they left out the unit. We need to resave
            // it here as a dimension to create final proper uss.
            if (!isNewValue && styleProperty.values[0].valueType != StyleValueType.Dimension)
            {
                styleProperty.values = new StyleValueHandle[0];
                isNewValue = true;
            }

            var dimension = new Dimension();
            dimension.unit = Dimension.Unit.Pixel;
            dimension.value = e.newValue;

            if (isNewValue)
                styleSheet.AddValue(styleProperty, dimension);
            else // TODO: Assume only one value.
                styleSheet.SetValue(styleProperty.values[0], dimension);

            PostStyleFieldSteps(e.target as VisualElement, styleName, isNewValue);
        }
#endif

        private void OnFieldValueChange(ChangeEvent<int> e, string styleName)
        {
            var styleProperty = GetStylePropertyByStyleName(styleName);
            var isNewValue = styleProperty.values.Length == 0;

            if (isNewValue)
                styleSheet.AddValue(styleProperty, e.newValue);
            else // TODO: Assume only one value.
                styleSheet.SetValue(styleProperty.values[0], e.newValue);

            PostStyleFieldSteps(e.target as VisualElement, styleName, isNewValue);
        }

        private void OnFieldValueChangeIntToFloat(ChangeEvent<int> e, string styleName)
        {
            var styleProperty = GetStylePropertyByStyleName(styleName);
            var isNewValue = styleProperty.values.Length == 0;

            var newValue = (float)e.newValue;

            if (isNewValue)
                styleSheet.AddValue(styleProperty, newValue);
            else // TODO: Assume only one value.
                styleSheet.SetValue(styleProperty.values[0], newValue);

            PostStyleFieldSteps(e.target as VisualElement, styleName, isNewValue);
        }

        private void OnFieldValueChange(ChangeEvent<float> e, string styleName)
        {
            var styleProperty = GetStylePropertyByStyleName(styleName);
            var isNewValue = styleProperty.values.Length == 0;

            if (isNewValue)
                styleSheet.AddValue(styleProperty, e.newValue);
            else // TODO: Assume only one value.
                styleSheet.SetValue(styleProperty.values[0], e.newValue);

            PostStyleFieldSteps(e.target as VisualElement, styleName, isNewValue);
        }

        private void OnFieldValueChange(ChangeEvent<Color> e, string styleName)
        {
            var styleProperty = GetStylePropertyByStyleName(styleName);
            var isNewValue = styleProperty.values.Length == 0;

            if (isNewValue)
                styleSheet.AddValue(styleProperty, e.newValue);
            else // TODO: Assume only one value.
                styleSheet.SetValue(styleProperty.values[0], e.newValue);

            PostStyleFieldSteps(e.target as VisualElement, styleName, isNewValue);
        }

        private void OnFieldValueChange(ChangeEvent<string> e, string styleName)
        {
            var styleProperty = GetStylePropertyByStyleName(styleName);
            var isNewValue = styleProperty.values.Length == 0;
            var field = e.target as VisualElement;

            if (field is IToggleButtonStrip)
            {
                var newValue = e.newValue;
                var newEnumValueStr = BuilderNameUtilities.ConvertDashToHungarian(newValue);
                var enumType = (field as IToggleButtonStrip).enumType;
                var newEnumValue = Enum.Parse(enumType, newEnumValueStr) as Enum;

                if (isNewValue)
                    styleSheet.AddValue(styleProperty, newEnumValue);
                else // TODO: Assume only one value.
                    styleSheet.SetValue(styleProperty.values[0], newEnumValue);

                // The state of Flex Direction can affect many other Flex-related fields.
                if (styleName == "flex-direction")
                    updateFlexColumnGlobalState?.Invoke(newEnumValue);
            }
            else
            {
                if (isNewValue)
                    styleSheet.AddValue(styleProperty, e.newValue);
                else // TODO: Assume only one value.
                    styleSheet.SetValue(styleProperty.values[0], e.newValue);
            }

            PostStyleFieldSteps(e.target as VisualElement, styleName, isNewValue);
        }

        private void OnFieldValueChange(ChangeEvent<Object> e, string styleName)
        {
            var styleProperty = GetStylePropertyByStyleName(styleName);
            var isNewValue = styleProperty.values.Length == 0;

            if (isNewValue)
                styleSheet.AddValue(styleProperty, e.newValue);
            else // TODO: Assume only one value.
                styleSheet.SetValue(styleProperty.values[0], e.newValue);

            PostStyleFieldSteps(e.target as VisualElement, styleName, isNewValue);
        }

        private void OnFieldValueChangeFont(ChangeEvent<Object> e, string styleName)
        {
            var field = e.target as ObjectField;
            if (e.newValue == null)
            {
                Debug.Log(BuilderConstants.FontCannotBeNoneMessage);
                field.SetValueWithoutNotify(e.previousValue);
                return;
            }

            var styleProperty = GetStylePropertyByStyleName(styleName);
            var isNewValue = styleProperty.values.Length == 0;

            if (isNewValue)
                styleSheet.AddValue(styleProperty, e.newValue);
            else // TODO: Assume only one value.
                styleSheet.SetValue(styleProperty.values[0], e.newValue);

            PostStyleFieldSteps(field, styleName, isNewValue);
        }

        private void OnFieldValueChange(ChangeEvent<Enum> e, string styleName)
        {
            var styleProperty = GetStylePropertyByStyleName(styleName);
            var isNewValue = styleProperty.values.Length == 0;

            if (isNewValue)
                styleSheet.AddValue(styleProperty, e.newValue);
            else // TODO: Assume only one value.
                styleSheet.SetValue(styleProperty.values[0], e.newValue);

            // The state of Flex Direction can affect many other Flex-related fields.
            if (styleName == "flex-direction")
                updateFlexColumnGlobalState?.Invoke(e.newValue);

            PostStyleFieldSteps(e.target as VisualElement, styleName, isNewValue);
        }
    }
}
