using System;
using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Unity.UI.Builder.EditorTests
{
    class BuilderInspectorTests : BuilderIntegrationTest
    {
        const string k_AllStylePropertiesTestFileName = "AllStylePropertiesTest.uxml";
        const string k_AllStylePropertiesTestFilePath = BuilderConstants.UIBuilderTestsTestFilesPath + "/" + k_AllStylePropertiesTestFileName;

        void CheckStyleFieldValue<TField, TValue>(string styleName, TValue value) where TField : BaseField<TValue> 
        {
            var styleRow = inspector.styleFields.m_StyleFields[styleName].First();
            var field = styleRow.Q<TField>() as BaseField<TValue>;
            Assert.AreEqual(value, field.value);
        }

        // Seems we are currently inconsistent between:
        //  - reading styles from inline UXML, where 255 alpha is converted to 1.0f alpha
        //  - reading styles from USS, where 255 alpha is not converted and stays 255
        // Doesn't seem to cause any bugs but should be fixed at some point.
        // Jira: https://jira.unity3d.com/browse/UIT-1092
        void CheckColorFieldValue(string styleName, Color value)
        {
            var styleRow = inspector.styleFields.m_StyleFields[styleName].First();
            var field = styleRow.Q<ColorField>();
            Assert.AreEqual(value.r, field.value.r);
            Assert.AreEqual(value.g, field.value.g);
            Assert.AreEqual(value.b, field.value.b);
        }

        void CheckAllStylePropertiesOnElement(string elementName)
        {
            var element = builder.documentRootElement.Q(elementName);
            Assert.NotNull(element);

            selection.Select(null, element);

            CheckStyleFieldValue<PercentSlider, float>("opacity", 0.82f);
            CheckStyleFieldValue<ToggleButtonStrip, string>("display", "none");
            CheckStyleFieldValue<ToggleButtonStrip, string>("visibility", "hidden");
            CheckStyleFieldValue<ToggleButtonStrip, string>("overflow", "hidden");

            CheckStyleFieldValue<EnumField, Enum>("position", Position.Absolute);
            CheckStyleFieldValue<StyleFieldBase, string>("left", "7px");
            CheckStyleFieldValue<StyleFieldBase, string>("top", "10px");
            CheckStyleFieldValue<StyleFieldBase, string>("right", "7px");
            CheckStyleFieldValue<StyleFieldBase, string>("bottom", "11px");

            CheckStyleFieldValue<StyleFieldBase, string>("flex-basis", "8px");
            CheckStyleFieldValue<StyleFieldBase, string>("flex-shrink", "2");
            CheckStyleFieldValue<StyleFieldBase, string>("flex-grow", "2");
            CheckStyleFieldValue<ToggleButtonStrip, string>("flex-direction", "column-reverse");
            CheckStyleFieldValue<ToggleButtonStrip, string>("flex-wrap", "wrap");

            CheckStyleFieldValue<ToggleButtonStrip, string>("align-items", "center");
            CheckStyleFieldValue<ToggleButtonStrip, string>("justify-content", "center");

            CheckStyleFieldValue<StyleFieldBase, string>("width", "14px");
            CheckStyleFieldValue<StyleFieldBase, string>("height", "15px");
            CheckStyleFieldValue<StyleFieldBase, string>("max-width", "7px");
            CheckStyleFieldValue<StyleFieldBase, string>("max-height", "15px");
            CheckStyleFieldValue<StyleFieldBase, string>("min-width", "9px");
            CheckStyleFieldValue<StyleFieldBase, string>("min-height", "14px");

            CheckStyleFieldValue<StyleFieldBase, string>("margin-left", "15px");
            CheckStyleFieldValue<StyleFieldBase, string>("margin-right", "15px");
            CheckStyleFieldValue<StyleFieldBase, string>("margin-top", "15px");
            CheckStyleFieldValue<StyleFieldBase, string>("margin-bottom", "15px");
            CheckStyleFieldValue<StyleFieldBase, string>("padding-left", "15px");
            CheckStyleFieldValue<StyleFieldBase, string>("padding-right", "15px");
            CheckStyleFieldValue<StyleFieldBase, string>("padding-top", "15px");
            CheckStyleFieldValue<StyleFieldBase, string>("padding-bottom", "15px");

            CheckStyleFieldValue<FontStyleStrip, string>("-unity-font-style", "italic");
            CheckStyleFieldValue<StyleFieldBase, string>("font-size", "32px");
            CheckColorFieldValue("color", new Color(1, 0, 0));
            CheckStyleFieldValue<TextAlignStrip, string>("-unity-text-align", "upper-center");
            CheckStyleFieldValue<ToggleButtonStrip, string>("white-space", "nowrap");

            CheckStyleFieldValue<ToggleButtonStrip, string>("-unity-background-scale-mode", "scale-and-crop");
            CheckColorFieldValue("-unity-background-image-tint-color", new Color(1, 0, 0));
            CheckColorFieldValue("background-color", new Color(1, 0, 0));
            CheckStyleFieldValue<StyleFieldBase, string>("-unity-slice-left", "5");
            CheckStyleFieldValue<StyleFieldBase, string>("-unity-slice-top", "5");
            CheckStyleFieldValue<StyleFieldBase, string>("-unity-slice-right", "5");
            CheckStyleFieldValue<StyleFieldBase, string>("-unity-slice-bottom", "5");

            CheckColorFieldValue("border-left-color", new Color(1, 0, 0.0117647061f));
            CheckColorFieldValue("border-right-color", new Color(1, 0, 0.97647059f));
            CheckColorFieldValue("border-top-color", new Color(0, 0.0235294122f, 1));
            CheckColorFieldValue("border-bottom-color", new Color(0, 0.933333337f, 1));
            CheckStyleFieldValue<StyleFieldBase, string>("border-left-width", "7px");
            CheckStyleFieldValue<StyleFieldBase, string>("border-right-width", "7px");
            CheckStyleFieldValue<StyleFieldBase, string>("border-top-width", "7px");
            CheckStyleFieldValue<StyleFieldBase, string>("border-bottom-width", "7px");
            CheckStyleFieldValue<StyleFieldBase, string>("border-top-left-radius", "7px");
            CheckStyleFieldValue<StyleFieldBase, string>("border-bottom-left-radius", "7px");
            CheckStyleFieldValue<StyleFieldBase, string>("border-top-right-radius", "7px");
            CheckStyleFieldValue<StyleFieldBase, string>("border-bottom-right-radius", "7px");
        }

        [UnityTest]
        public IEnumerator CheckAllStylePropertiesForOverrides()
        {
            yield return LoadTestUXMLDocument(k_AllStylePropertiesTestFilePath);

            CheckAllStylePropertiesOnElement("all-properties-inline");
            CheckAllStylePropertiesOnElement("all-properties-uss");
        }
    }
}
