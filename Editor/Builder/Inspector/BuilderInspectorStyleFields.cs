using UnityEngine;
using UnityEngine.UIElements;
using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.UIElements;
using Object = UnityEngine.Object;
using UnityEngine.Assertions;
using UnityEditor;

#if UNITY_2019_3_OR_NEWER
using UnityEngine.UIElements.StyleSheets;
#endif

namespace Unity.UI.Builder
{
    internal class BuilderInspectorStyleFields
    {
        BuilderInspector m_Inspector;
        BuilderSelection m_Selection;

        Dictionary<string, List<VisualElement>> m_StyleFields;

        List<string> s_StyleChangeList = new List<string>();

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

        StyleProperty GetStyleProperty(StyleRule rule, string styleName)
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

        string ConvertUssStyleNameToCSharpStyleName(string ussStyleName)
        {
            if (ussStyleName == "-unity-font-style")
                return "-unity-font-style-and-weight";

            return ussStyleName;
        }

        PropertyInfo FindStylePropertyInfo(string styleName)
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
            else if (val is StyleFloat && fieldElement is NumericStyleField)
            {
                var uiField = fieldElement as NumericStyleField;
                uiField.RegisterValueChangedCallback(e => OnNumericStyleFieldValueChange(e, styleName));
            }
            else if (val is StyleFloat && fieldElement is DimensionStyleField)
            {
                var uiField = fieldElement as DimensionStyleField;
                uiField.RegisterValueChangedCallback(e => OnDimensionStyleFieldValueChange(e, styleName));
            }
            else if (val is StyleInt && fieldElement is IntegerField)
            {
                var uiField = fieldElement as IntegerField;
                uiField.RegisterValueChangedCallback(e => OnFieldValueChange(e, styleName));
            }
            else if (val is StyleInt && fieldElement is IntegerStyleField)
            {
                var uiField = fieldElement as IntegerStyleField;
                uiField.RegisterValueChangedCallback(e => OnIntegerStyleFieldValueChange(e, styleName));
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
            else if (val is StyleLength && fieldElement is DimensionStyleField)
            {
                var uiField = fieldElement as DimensionStyleField;
                uiField.RegisterValueChangedCallback(e => OnDimensionStyleFieldValueChange(e, styleName));
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

        public void BindStyleField(BuilderStyleRow styleRow, FoldoutNumberField foldoutElement)
        {
            var intFields = foldoutElement.Query<StyleFieldBase>().ToList();

            foreach (var field in intFields)
            {
                field.SetProperty(BuilderConstants.FoldoutFieldPropertyName, foldoutElement);
                field.RegisterValueChangedCallback((evt) =>
                {
                    foldoutElement.UpdateFromChildFields();
                    foldoutElement.header.AddToClassList(BuilderConstants.InspectorLocalStyleOverrideClassName);
                });

                BuilderStyleRow.ReAssignTooltipToChild(field);
            }
            foldoutElement.header.SetProperty(BuilderConstants.FoldoutFieldPropertyName, foldoutElement);
            foldoutElement.headerInputField.RegisterValueChangedCallback(FoldoutNumberFieldOnValueChange);

            foldoutElement.headerInputField.AddManipulator(
                new ContextualMenuManipulator((e) =>
                {
                    e.target = foldoutElement.header;
                    BuildStyleFieldContextualMenu(e);
                }));

            foldoutElement.header.AddManipulator(
                new ContextualMenuManipulator(BuildStyleFieldContextualMenu));
        }

        public void BindStyleField(BuilderStyleRow styleRow, FoldoutColorField foldoutElement)
        {
            var colorFields = foldoutElement.Query<ColorField>().ToList();

            foreach (var field in colorFields)
            {
                field.SetProperty(BuilderConstants.FoldoutFieldPropertyName, foldoutElement);
                field.RegisterValueChangedCallback((evt) =>
                {
                    var row = field.GetProperty(BuilderConstants.InspectorLinkedStyleRowVEPropertyName) as BuilderStyleRow;
                    if (row != null && !string.IsNullOrEmpty(row.bindingPath))
                        foldoutElement.UpdateFromChildField(row.bindingPath, field.value);

                    foldoutElement.header.AddToClassList(BuilderConstants.InspectorLocalStyleOverrideClassName);
                });

                BuilderStyleRow.ReAssignTooltipToChild(field);
            }
            foldoutElement.header.SetProperty(BuilderConstants.FoldoutFieldPropertyName, foldoutElement);
            foldoutElement.headerInputField.RegisterValueChangedCallback(FoldoutColorFieldOnValueChange);

            foldoutElement.headerInputField.AddManipulator(
                new ContextualMenuManipulator((e) =>
                {
                    e.target = foldoutElement.header;
                    BuildStyleFieldContextualMenu(e);
                }));

            foldoutElement.header.AddManipulator(
                new ContextualMenuManipulator(BuildStyleFieldContextualMenu));
        }

        void FoldoutNumberFieldOnValueChange(ChangeEvent<string> evt)
        {
            var newValue = evt.newValue;
            var target = evt.target as TextField;
            var foldoutElement = target.GetFirstAncestorOfType<FoldoutNumberField>();

            var splitBy = new char[] { ' ' };
            string[] inputArray = newValue.Split(splitBy);
            var styleFields = foldoutElement.Query<StyleFieldBase>().ToList();

            if (inputArray.Length == 1 && styleFields.Count > 0)
            {
                var newCommonValue = inputArray[0];
                var newCommonValueWithUnit = newValue;

                for (int i = 0; i < styleFields.Count; ++i)
                {
                    if (i == 0 && styleFields[0] is DimensionStyleField)
                    {
                        styleFields[i].value = newCommonValueWithUnit;
                        newCommonValueWithUnit = styleFields[i].value;
                        continue;
                    }

                    if (styleFields[i] is DimensionStyleField)
                        styleFields[i].value = newCommonValueWithUnit;
                    else
                        styleFields[i].value = newCommonValue;
                }
            }
            else
            {
                for (int i = 0; i < Mathf.Min(inputArray.Length, styleFields.Count); ++i)
                {
                    styleFields[i].value = inputArray[i];
                }
            }

            foldoutElement.UpdateFromChildFields();

            evt.StopPropagation();
        }

        void FoldoutColorFieldOnValueChange(ChangeEvent<Color> evt)
        {
            var newValue = evt.newValue;
            var target = evt.target as ColorField;
            var foldoutColorField = target.GetFirstAncestorOfType<FoldoutColorField>();

            foldoutColorField.header.AddToClassList(BuilderConstants.InspectorLocalStyleOverrideClassName);
            foldoutColorField.headerInputField.value = newValue;
            for (int i = 0; i < foldoutColorField.bindingPathArray.Length; i++)
            {
                var styleName = foldoutColorField.bindingPathArray[i];
                var styleProperty = GetStylePropertyByStyleName(styleName);

                if (styleProperty.values.Length == 0)
                    styleSheet.AddValue(styleProperty, newValue);
                else // TODO: Assume only one value.
                    styleSheet.SetValue(styleProperty.values[0], newValue);

                NotifyStyleChanges();

                m_Inspector.StylingChanged(foldoutColorField.bindingPathArray.ToList());
            }
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
            else if (val is StyleFloat && fieldElement is NumericStyleField)
            {
                var style = (StyleFloat)val;
                var uiField = fieldElement as NumericStyleField;

                var value = (int)style.value;
                if (styleProperty != null)
                {
                    var styleValue = styleProperty.values[0];
                    if (styleValue.valueType == StyleValueType.Keyword)
                    {
                        var keyword = styleSheet.GetKeyword(styleValue);
                        uiField.keyword = keyword;
                    }
                    else if (styleValue.valueType == StyleValueType.Float)
                    {
                        var number = styleSheet.GetFloat(styleValue);
                        uiField.SetValueWithoutNotify(number.ToString());
                    }
                    else
                    {
                        throw new InvalidOperationException("StyleValueType " + styleValue.valueType.ToString() + " not compatible with StyleFloat.");
                    }
                }
                else
                {
                    if (style.keyword != StyleKeyword.Undefined)
                        uiField.keyword = StyleSheetUtilities.ConvertStyleKeyword(style.keyword);
                    else
                        uiField.SetValueWithoutNotify(value.ToString());
                }
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
            else if (val is StyleInt && fieldElement is IntegerStyleField)
            {
                var style = (StyleInt)val;
                var uiField = fieldElement as IntegerStyleField;

                var value = style.value;
                if (styleProperty != null)
                {
                    var styleValue = styleProperty.values[0];
                    if (styleValue.valueType == StyleValueType.Keyword)
                    {
                        var keyword = styleSheet.GetKeyword(styleValue);
                        uiField.keyword = keyword;
                    }
                    else if (styleValue.valueType == StyleValueType.Float)
                    {
                        var number = styleSheet.GetFloat(styleValue);
                        uiField.SetValueWithoutNotify(number.ToString());
                    }
                    else
                    {
                        throw new InvalidOperationException("StyleValueType " + styleValue.valueType.ToString() + " not compatible with StyleFloat.");
                    }
                }
                else
                {
                    if (style.keyword != StyleKeyword.Undefined)
                        uiField.keyword = StyleSheetUtilities.ConvertStyleKeyword(style.keyword);
                    else
                        uiField.SetValueWithoutNotify(value.ToString());
                }
            }
            else if (val is StyleLength && fieldElement is IntegerField)
            {
                var style = (StyleLength)val;
                var uiField = fieldElement as IntegerField;

                // If the styleProperty exists for this length but it's a keyword,
                // just ignore it. IntegerField cannot represent keywords.
                if (styleProperty != null && style.keyword != StyleKeyword.Undefined)
                    styleProperty = null;

                var value = (int)style.value.value;
                if (styleProperty != null)
#if UNITY_2019_3_OR_NEWER
                    value = (int)styleSheet.GetDimension(styleProperty.values[0]).value;
#else
                    value = styleSheet.GetInt(styleProperty.values[0]);
#endif

                uiField.SetValueWithoutNotify(value);
            }
            else if (val is StyleFloat && fieldElement is DimensionStyleField)
            {
                var style = (StyleFloat)val;
                var uiField = fieldElement as DimensionStyleField;

                var value = (int)style.value;
                if (styleProperty != null)
                {
                    var styleValue = styleProperty.values[0];
                    if (styleValue.valueType == StyleValueType.Keyword)
                    {
                        var keyword = styleSheet.GetKeyword(styleValue);
                        uiField.keyword = keyword;
                    }
#if UNITY_2019_3_OR_NEWER
                    else if (styleValue.valueType == StyleValueType.Dimension)
                    {
                        var dimension = styleSheet.GetDimension(styleValue);
                        uiField.unit = dimension.unit;
                        uiField.length = dimension.value;
                    }
#endif
                    else if (styleValue.valueType == StyleValueType.Float)
                    {
                        var length = styleSheet.GetFloat(styleValue);
                        uiField.SetValueWithoutNotify(length.ToString() + DimensionStyleField.defaultUnit);
                    }
                    else
                    {
                        throw new InvalidOperationException("StyleValueType " + styleValue.valueType.ToString() + " not compatible with StyleFloat.");
                    }
                }
                else
                {
                    if (style.keyword != StyleKeyword.Undefined)
                        uiField.keyword = StyleSheetUtilities.ConvertStyleKeyword(style.keyword);
                    else
                        uiField.SetValueWithoutNotify(value.ToString());
                }
            }
            else if (val is StyleLength && fieldElement is DimensionStyleField)
            {
                var style = (StyleLength)val;
                var uiField = fieldElement as DimensionStyleField;

                var value = (int)style.value.value;
                if (styleProperty != null)
                {
                    var styleValue = styleProperty.values[0];
                    if (styleValue.valueType == StyleValueType.Keyword)
                    {
                        var keyword = styleSheet.GetKeyword(styleValue);
                        uiField.keyword = keyword;
                    }
#if UNITY_2019_3_OR_NEWER
                    else if (styleValue.valueType == StyleValueType.Dimension)
                    {
                        var dimension = styleSheet.GetDimension(styleValue);
                        uiField.unit = dimension.unit;
                        uiField.length = dimension.value;
                    }
#endif
                    else if (styleValue.valueType == StyleValueType.Float)
                    {
                        var length = styleSheet.GetFloat(styleValue);
                        uiField.SetValueWithoutNotify(length.ToString() + DimensionStyleField.defaultUnit);
                    }
                    else
                    {
                        throw new InvalidOperationException("StyleValueType " + styleValue.valueType.ToString() + " not compatible with StyleLength.");
                    }
                }
                else
                {
                    if (style.keyword != StyleKeyword.Undefined)
                        uiField.keyword = StyleSheetUtilities.ConvertStyleKeyword(style.keyword);
                    else
                        uiField.SetValueWithoutNotify(value.ToString());
                }
            }
            else if (val is StyleColor && fieldElement is ColorField)
            {
                var style = (StyleColor)val;
                var uiField = fieldElement as ColorField;

                var value = style.value;
                if (styleProperty != null)
                    value = styleSheet.GetColor(styleProperty.values[0]);

                // We keep falling into the alpha==0 trap. This patches the issue a little.
                if (value.a < 0.1f)
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

            var foldout = fieldElement.GetProperty(BuilderConstants.FoldoutFieldPropertyName) as FoldoutNumberField;
            if (foldout != null)
            {
                foldout.UpdateFromChildFields();
            }
        }

        public void RefreshStyleField(FoldoutField foldoutElement)
        {
            if (foldoutElement is FoldoutNumberField)
                RefreshStyleFoldoutNumberField(foldoutElement as FoldoutNumberField);
            else if (foldoutElement is FoldoutColorField)
                RefreshStyleFoldoutColorField(foldoutElement as FoldoutColorField);
        }

        void RefreshStyleFoldoutNumberField(FoldoutNumberField foldoutElement)
        {
            var isDirty = false;

            foreach (var path in foldoutElement.bindingPathArray)
            {
                var cSharpStyleName = ConvertUssStyleNameToCSharpStyleName(path);
                var styleProperty = GetStyleProperty(currentRule, cSharpStyleName);

                var field = FindStylePropertyInfo(path);
                if (field == null)
                    continue;

                if (styleProperty != null)
                {
                    isDirty = true;
                    break;
                }
            }

            if (isDirty)
                foldoutElement.header.AddToClassList(BuilderConstants.InspectorLocalStyleOverrideClassName);
            else
                foldoutElement.header.RemoveFromClassList(BuilderConstants.InspectorLocalStyleOverrideClassName);
        }

        void RefreshStyleFoldoutColorField(FoldoutColorField foldoutElement)
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
                if (val is StyleColor)
                {
                    var style = (StyleColor)val;
                    var value = style.value;

                    // We keep falling into the alpha==0 trap. This patches the issue a little.
                    if (value.a < 0.1f)
                        value.a = 255.0f;

                    if (styleProperty != null)
                    {
                        isDirty = true;
                        value = styleSheet.GetColor(styleProperty.values[0]);
                    }

                    foldoutElement.UpdateFromChildField(path, value);
                }
            }

            if (isDirty)
                foldoutElement.header.AddToClassList(BuilderConstants.InspectorLocalStyleOverrideClassName);
            else
                foldoutElement.header.RemoveFromClassList(BuilderConstants.InspectorLocalStyleOverrideClassName);
        }

        void BuildStyleFieldContextualMenu(ContextualMenuPopulateEvent evt)
        {
            evt.menu.AppendAction(
                BuilderConstants.ContextMenuUnsetMessage,
                UnsetStyleProperty,
                DropdownMenuAction.AlwaysEnabled,
                evt.target);
            
            evt.menu.AppendAction(
                BuilderConstants.ContextMenuUnsetAllMessage,
                UnsetAllStyleProperties,
                DropdownMenuAction.AlwaysEnabled,
                evt.target);
        }

        void UnsetAllStyleProperties(DropdownMenuAction action)
        {
            var fields = new List<VisualElement>();
            foreach (var pair in m_StyleFields)
            {
                var styleFields = pair.Value;
                fields.AddRange(styleFields);
            }

            UnsetStyleProperties(fields);
            NotifyStyleChanges();
        }
        
        void UnsetStyleProperty(DropdownMenuAction action)
        {
            var listToUnset = (action.userData as VisualElement)?.userData;
            if (listToUnset != null && listToUnset is List<BindableElement> bindableElements)
            {
                UnsetStyleProperties(bindableElements);
                NotifyStyleChanges();
                return;
            }

            var fieldElement = action.userData as VisualElement;
            UnsetStyleProperties(new List<VisualElement>{ fieldElement });
            NotifyStyleChanges();
        }
        
         void UnsetStyleProperties(IEnumerable<VisualElement> fields) 
         {
            foreach (var fieldElement in fields)
            {
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
                
                var foldout = fieldElement.GetProperty(BuilderConstants.FoldoutFieldPropertyName) as FoldoutField;
                if (foldout != null)
                {
                    // Check if the unset element was the header field.
                    if (fieldElement.ClassListContains(BuilderConstants.FoldoutFieldHeaderClassName))
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
                        // The unset element was a child of a FoldoutNumberField
                        m_Inspector.schedule.Execute(() =>
                        {
                            RefreshStyleField(foldout);
                            updateStyleCategoryFoldoutOverrides?.Invoke();
                        });
                }
            }
        }

        void NotifyStyleChanges(List<string> styles = null)
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

        StyleProperty GetStylePropertyByStyleName(string styleName)
        {
            var styleProperty = styleSheet.FindProperty(currentRule, styleName);
            if (styleProperty == null)
                styleProperty = styleSheet.AddProperty(currentRule, styleName);

            return styleProperty;
        }

        void PostStyleFieldSteps(VisualElement target, string styleName, bool isNewValue)
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

        void OnFieldKeywordChange(StyleValueKeyword keyword, VisualElement target, string styleName)
        {
            var styleProperty = GetStylePropertyByStyleName(styleName);
            var isNewValue = styleProperty.values.Length == 0;

            // If the current style property is saved as a different type than the new style type,
            // we need to resave it here as the new type. We do this by just removing the current value.
            if (!isNewValue && styleProperty.values[0].valueType != StyleValueType.Keyword)
            {
                Undo.RegisterCompleteObjectUndo(styleSheet, BuilderConstants.ChangeUIStyleValueUndoMessage);

                styleProperty.values = new StyleValueHandle[0];
                isNewValue = true;
            }

            if (isNewValue)
                styleSheet.AddValue(styleProperty, keyword, BuilderConstants.ChangeUIStyleValueUndoMessage);
            else // TODO: Assume only one value.
            {
                Undo.RegisterCompleteObjectUndo(styleSheet, BuilderConstants.ChangeUIStyleValueUndoMessage);

                var styleValue = styleProperty.values[0];
                styleValue.valueIndex = (int)keyword;
                styleProperty.values[0] = styleValue;
            }

            PostStyleFieldSteps(target, styleName, isNewValue);
        }

#if UNITY_2019_3_OR_NEWER
        void OnFieldDimensionChangeImpl(float newValue, Dimension.Unit newUnit, VisualElement target, string styleName)
        {
            var styleProperty = GetStylePropertyByStyleName(styleName);
            var isNewValue = styleProperty.values.Length == 0;

            // If the current style property is saved as a different type than the new style type,
            // we need to resave it here as the new type. We do this by just removing the current value.
            if (!isNewValue && styleProperty.values[0].valueType != StyleValueType.Dimension)
            {
                Undo.RegisterCompleteObjectUndo(styleSheet, BuilderConstants.ChangeUIStyleValueUndoMessage);

                styleProperty.values = new StyleValueHandle[0];
                isNewValue = true;
            }

            var dimension = new Dimension();
            dimension.unit = newUnit;
            dimension.value = newValue;

            if (isNewValue)
                styleSheet.AddValue(styleProperty, dimension);
            else // TODO: Assume only one value.
                styleSheet.SetValue(styleProperty.values[0], dimension);

            PostStyleFieldSteps(target, styleName, isNewValue);
        }

        void OnFieldDimensionChange(ChangeEvent<int> e, string styleName)
        {
            OnFieldDimensionChangeImpl(e.newValue, Dimension.Unit.Pixel, e.target as VisualElement, styleName);
        }
#endif

        void OnDimensionStyleFieldValueChange(ChangeEvent<string> e, string styleName)
        {
            var dimensionStyleField = e.target as DimensionStyleField;

            if (dimensionStyleField.isKeyword)
            {
                OnFieldKeywordChange(
                    dimensionStyleField.keyword,
                    dimensionStyleField,
                    styleName);
            }
            else
            {
#if UNITY_2019_3_OR_NEWER
                OnFieldDimensionChangeImpl(
                    dimensionStyleField.length,
                    dimensionStyleField.unit,
                    dimensionStyleField,
                    styleName);
#else
                OnFieldValueChangeImpl(
                    dimensionStyleField.length,
                    dimensionStyleField,
                    styleName);
#endif
            }
        }

        void OnFieldValueChangeImpl(int newValue, VisualElement target, string styleName)
        {
            var styleProperty = GetStylePropertyByStyleName(styleName);
            var isNewValue = styleProperty.values.Length == 0;

            // If the current style property is saved as a different type than the new style type,
            // we need to resave it here as the new type. We do this by just removing the current value.
            if (!isNewValue && styleProperty.values[0].valueType != StyleValueType.Float)
            {
                Undo.RegisterCompleteObjectUndo(styleSheet, BuilderConstants.ChangeUIStyleValueUndoMessage);

                styleProperty.values = new StyleValueHandle[0];
                isNewValue = true;
            }

            if (isNewValue)
                styleSheet.AddValue(styleProperty, newValue);
            else // TODO: Assume only one value.
                styleSheet.SetValue(styleProperty.values[0], newValue);

            PostStyleFieldSteps(target, styleName, isNewValue);
        }

        void OnFieldValueChange(ChangeEvent<int> e, string styleName)
        {
            OnFieldValueChangeImpl(e.newValue, e.target as VisualElement, styleName);
        }

        void OnFieldValueChangeIntToFloat(ChangeEvent<int> e, string styleName)
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

        void OnFieldValueChangeImpl(float newValue, VisualElement target, string styleName)
        {
            var styleProperty = GetStylePropertyByStyleName(styleName);
            var isNewValue = styleProperty.values.Length == 0;

            // If the current style property is saved as a different type than the new style type,
            // we need to resave it here as the new type. We do this by just removing the current value.
            if (!isNewValue && styleProperty.values[0].valueType != StyleValueType.Float)
            {
                Undo.RegisterCompleteObjectUndo(styleSheet, BuilderConstants.ChangeUIStyleValueUndoMessage);

                styleProperty.values = new StyleValueHandle[0];
                isNewValue = true;
            }

            if (isNewValue)
                styleSheet.AddValue(styleProperty, newValue);
            else // TODO: Assume only one value.
                styleSheet.SetValue(styleProperty.values[0], newValue);

            PostStyleFieldSteps(target, styleName, isNewValue);
        }

        void OnFieldValueChange(ChangeEvent<float> e, string styleName)
        {
            OnFieldValueChangeImpl(e.newValue, e.target as VisualElement, styleName);
        }

        void OnNumericStyleFieldValueChange(ChangeEvent<string> e, string styleName)
        {
            var numericStyleField = e.target as NumericStyleField;

            if (numericStyleField.isKeyword)
            {
                OnFieldKeywordChange(
                    numericStyleField.keyword,
                    numericStyleField,
                    styleName);
            }
            else
            {
                OnFieldValueChangeImpl(
                    numericStyleField.number,
                    numericStyleField,
                    styleName);
            }
        }

        void OnIntegerStyleFieldValueChange(ChangeEvent<string> e, string styleName)
        {
            var styleField = e.target as IntegerStyleField;

            if (styleField.isKeyword)
            {
                OnFieldKeywordChange(
                    styleField.keyword,
                    styleField,
                    styleName);
            }
            else
            {
                OnFieldValueChangeImpl(
                    styleField.number,
                    styleField,
                    styleName);
            }
        }

        void OnFieldValueChange(ChangeEvent<Color> e, string styleName)
        {
            var styleProperty = GetStylePropertyByStyleName(styleName);
            var isNewValue = styleProperty.values.Length == 0;

            if (isNewValue)
                styleSheet.AddValue(styleProperty, e.newValue);
            else // TODO: Assume only one value.
                styleSheet.SetValue(styleProperty.values[0], e.newValue);

            PostStyleFieldSteps(e.target as VisualElement, styleName, isNewValue);
        }

        void OnFieldValueChange(ChangeEvent<string> e, string styleName)
        {
            var field = e.target as VisualElement;

            // HACK: For some reason, when using "Pick Element" feature of Debugger and
            // hovering over the button strips, we get bogus value change events with
            // empty strings.
            if (field is IToggleButtonStrip && string.IsNullOrEmpty(e.newValue))
                return;

            var styleProperty = GetStylePropertyByStyleName(styleName);
            var isNewValue = styleProperty.values.Length == 0;

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

        void OnFieldValueChange(ChangeEvent<Object> e, string styleName)
        {
            var styleProperty = GetStylePropertyByStyleName(styleName);
            var isNewValue = styleProperty.values.Length == 0;

            var resourcesPath = BuilderAssetUtilities.GetResourcesPathForAsset(e.newValue);
            if (!isNewValue)
            {
                if (styleProperty.values[0].valueType == StyleValueType.ResourcePath && string.IsNullOrEmpty(resourcesPath))
                {
                    styleSheet.RemoveValue(styleProperty, styleProperty.values[0]);
                    isNewValue = true;
                }
                else if (styleProperty.values[0].valueType == StyleValueType.AssetReference && !string.IsNullOrEmpty(resourcesPath))
                {
                    styleSheet.RemoveValue(styleProperty, styleProperty.values[0]);
                    isNewValue = true;
                }
            }

            if (isNewValue)
                styleSheet.AddValue(styleProperty, e.newValue);
            else // TODO: Assume only one value.
                styleSheet.SetValue(styleProperty.values[0], e.newValue);

            PostStyleFieldSteps(e.target as VisualElement, styleName, isNewValue);
        }

        void OnFieldValueChangeFont(ChangeEvent<Object> e, string styleName)
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

        void OnFieldValueChange(ChangeEvent<Enum> e, string styleName)
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
