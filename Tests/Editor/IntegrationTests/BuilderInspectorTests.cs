using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

#if !UI_BUILDER_PACKAGE || UNITY_2021_2_OR_NEWER || (UIE_PACKAGE && PACKAGE_TEXT_CORE && UNITY_2020_2_OR_NEWER)
using UnityEngine.UIElements.StyleSheets;
#endif

namespace Unity.UI.Builder.EditorTests
{
    class BuilderInspectorTests : BuilderIntegrationTest
    {
        const string k_AllStylePropertiesTestFileName = "AllStylePropertiesTest.uxml";
        const string k_AllStylePropertiesTestFilePath = BuilderConstants.UIBuilderTestsTestFilesPath + "/" + k_AllStylePropertiesTestFileName;
        const string k_ScalableImageSelectorTestFileName = "ScalableImageSelectorTestDocument.uxml";
        const string k_ScalableImageSelectorTestFilePath = BuilderConstants.UIBuilderTestsTestFilesPath + "/" + k_ScalableImageSelectorTestFileName;
#if !UI_BUILDER_PACKAGE || UNITY_2021_1_OR_NEWER
        const string k_SpriteTestFilePath = BuilderConstants.UIBuilderTestsTestFilesPath + "/" + "Tools.png";
        const string k_SpriteEditorWindowName = "SpriteEditorWindow";
#endif

        void CheckStyleFieldValue<TField, TValue>(string styleName, TValue value) where TField : BaseField<TValue>
        {
            var styleRow = inspector.styleFields.m_StyleFields[styleName].First();
            var field = styleRow.Q<TField>() as BaseField<TValue>;
            Assert.AreEqual(value, field.value);
        }

#if !UI_BUILDER_PACKAGE || UNITY_2021_2_OR_NEWER || (UIE_PACKAGE && PACKAGE_TEXT_CORE && UNITY_2020_2_OR_NEWER)
        void CheckTextShadowFieldValue(string styleName, BuilderTextShadow value)
        {
            var styleRow = inspector.styleFields.m_StyleFields[styleName].First();
            var field = styleRow.Q<TextShadowStyleField>();
            Assert.AreEqual(value.offsetX, field.value.offsetX);
            Assert.AreEqual(value.offsetY, field.value.offsetY);
            Assert.AreEqual(value.blurRadius, field.value.blurRadius);
            Assert.AreEqual(value.color.r, field.value.color.r);
            Assert.AreEqual(value.color.g, field.value.color.g);
            Assert.AreEqual(value.color.b, field.value.color.b);
        }

        [UnityTest]
        public IEnumerator TextShadowFieldValueIsReadAppropriately()
        {
            yield return LoadTestUXMLDocument(k_AllStylePropertiesTestFilePath);
            var element = builder.documentRootElement.Q("all-properties-inline");
            Assert.NotNull(element);
            selection.Select(null, element);
            BuilderTextShadow newValue = new BuilderTextShadow
            {
                offsetX = new Dimension(0, Dimension.Unit.Pixel),   // This would cause problems before issue #1351667 was fixed
                offsetY = new Dimension(1f, Dimension.Unit.Pixel),
                blurRadius = new Dimension(0, Dimension.Unit.Pixel),    // This would cause problems before issue #1351667 was fixed
                color = Color.red
            };
            var styleRow = inspector.styleFields.m_StyleFields["text-shadow"].First();
            var field = styleRow.Q<TextShadowStyleField>();
            field.value = newValue;
            yield return UIETestHelpers.Pause();
            CheckTextShadowFieldValue("text-shadow", newValue);
        }
#endif

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

#if !UI_BUILDER_PACKAGE || !UNITY_2020_3_12
            CheckStyleFieldValue<FontStyleStrip, string>("-unity-font-style", "italic");
#endif
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

#if !UI_BUILDER_PACKAGE || UNITY_2021_2_OR_NEWER || (UIE_PACKAGE && PACKAGE_TEXT_CORE && UNITY_2020_2_OR_NEWER)
            if (elementName != "all-properties-uss")
            {
                CheckTextShadowFieldValue("text-shadow", 
                    new BuilderTextShadow
                    {
                        offsetX = new Dimension(1f, Dimension.Unit.Pixel),
                        offsetY = new Dimension(1f, Dimension.Unit.Pixel),
                        blurRadius = new Dimension(1f, Dimension.Unit.Pixel),
                        color = new Color(1, 0, 0)
                    }); 
                CheckStyleFieldValue<StyleFieldBase, string>("-unity-text-outline-width", "1px");
                CheckColorFieldValue("-unity-text-outline-color", new Color(1, 0, 0));
            }
#endif
        }

#if UI_BUILDER_PACKAGE && !UNITY_2021_2_OR_NEWER
        [UnityTest, Ignore("Test broken on older editors")]
#else
        [UnityTest]
#endif
        public IEnumerator CheckThatScalableImageSelectorIsDisplayed()
        {
            yield return LoadTestUXMLDocument(k_ScalableImageSelectorTestFilePath);
            var testElement = builder.documentRootElement.Q<VisualElement>("TestElement");
            selection.Select(null, testElement);
            var matchingSelectorsContainer = inspector.Q("matching-selectors-container");
            var selectors = matchingSelectorsContainer.contentContainer.Query<PersistedFoldout>();
            Assert.That(selectors.ToList().Count, Is.GreaterThan(0));
            var selector = selectors.First();
            Assert.That(selector.text, Is.EqualTo(".scalable-image"));
        }
 
        [UnityTest]
        public IEnumerator CheckAllStylePropertiesForOverrides()
        {
            yield return LoadTestUXMLDocument(k_AllStylePropertiesTestFilePath);

            CheckAllStylePropertiesOnElement("all-properties-inline");
            CheckAllStylePropertiesOnElement("all-properties-uss");
        }

        IEnumerator CheckStyleToggleFieldValue<TField>(string styleName, string newValue, bool useContains = false) where TField : BaseField<string>, IToggleButtonStrip
        {
            var styleRow = inspector.styleFields.m_StyleFields[styleName].First();
            var field = styleRow.Q<TField>();
            var button = field.Q<Button>(newValue);

            var foldout = button.GetFirstAncestorOfType<PersistedFoldout>();
            foldout.value = true;
            yield return UIETestHelpers.Pause();

            var scrollView = button.GetFirstAncestorOfType<ScrollView>();
            scrollView.ScrollTo(button);
            yield return UIETestHelpers.Pause();

            yield return UIETestEvents.Mouse.SimulateClick(button);

            if (useContains)
                Assert.IsTrue(field.value.Contains(newValue));
            else
                Assert.AreEqual(newValue, field.value);
        }

        // Can be used as toggle icons in the document, so we want to check they all work in uss only.
        [UnityTest]
        public IEnumerator CheckAllTogglesForOverridesInUss()
        {
            yield return LoadTestUXMLDocument(k_AllStylePropertiesTestFilePath);

            yield return null;

            var selector = BuilderTestsHelper.GetExplorerItemWithName(styleSheetsPane, ".all-properties-set");
            yield return UIETestEvents.Mouse.SimulateClick(selector);

            yield return CheckStyleToggleFieldValue<ToggleButtonStrip>("display", "flex");
            yield return CheckStyleToggleFieldValue<ToggleButtonStrip>("visibility", "visible");
            yield return CheckStyleToggleFieldValue<ToggleButtonStrip>("overflow", "visible");

            yield return CheckStyleToggleFieldValue<ToggleButtonStrip>("flex-direction", "row-reverse");
            yield return CheckStyleToggleFieldValue<ToggleButtonStrip>("flex-wrap", "nowrap");

            yield return CheckStyleToggleFieldValue<ToggleButtonStrip>("align-items", "flex-end");
            yield return CheckStyleToggleFieldValue<ToggleButtonStrip>("justify-content", "space-between");

            // Text align is split in two strips
            yield return CheckStyleToggleFieldValue<TextAlignStrip>("-unity-text-align", "lower", true);
            yield return CheckStyleToggleFieldValue<TextAlignStrip>("-unity-text-align", "right", true);
            yield return CheckStyleToggleFieldValue<ToggleButtonStrip>("white-space", "normal");

            yield return CheckStyleToggleFieldValue<ToggleButtonStrip>("-unity-background-scale-mode", "scale-to-fit");
        }

#if !UI_BUILDER_PACKAGE || UNITY_2021_1_OR_NEWER
        [UnityTest]
        public IEnumerator CheckThatSpriteCanBeUsedAsBackgroundImage()
        {
            builder.documentRootElement.Clear();

            AddElementCodeOnly<Button>();

            var imageStyleField = inspector.styleFields.m_StyleFields["background-image"].First() as ImageStyleField;
            Assert.NotNull(imageStyleField);

            var spriteAsset = AssetDatabase.LoadAssetAtPath<Sprite>(k_SpriteTestFilePath);
            imageStyleField.SetTypePopupValueWithoutNotify(typeof(Sprite));
            imageStyleField.SetValueWithoutNotify(spriteAsset);

            Assert.That(imageStyleField.value is Sprite);
            yield return null;
        }

        [UnityTest]
        public IEnumerator CheckThat2DSpriteEditorOpensOnRequest()
        {
            builder.documentRootElement.Clear();

            AddElementCodeOnly<Button>();
            var element = GetFirstDocumentElement();
            yield return UIETestEvents.Mouse.SimulateClick(element);

            var imageStyleField = inspector.styleFields.m_StyleFields["background-image"].First() as ImageStyleField;

            var spriteAsset = AssetDatabase.LoadAssetAtPath<Sprite>(k_SpriteTestFilePath);
            imageStyleField?.SetTypePopupValueWithoutNotify(typeof(Sprite));
            imageStyleField?.SetValueWithoutNotify(spriteAsset);

            if (BuilderExternalPackages.is2DSpriteEditorInstalled)
            {
                var backgroundFoldout =
                    inspector.Query<PersistedFoldout>().Where(f => f.text.Equals("Background")).ToList().First();
                backgroundFoldout.value = true;
                yield return UIETestHelpers.Pause(1);

                var inspectorScrollView = inspector.Q<ScrollView>();
                var openSpriteEditorButton = imageStyleField.Q<Button>();
                inspectorScrollView.ScrollTo(openSpriteEditorButton);
                yield return UIETestHelpers.Pause(1);

                yield return UIETestEvents.Mouse.SimulateClick(openSpriteEditorButton);

                EditorWindow spriteEditorWindow = null;
                var loopCount = 0;
                while (spriteEditorWindow == null && loopCount < 10)
                {
                    yield return UIETestHelpers.Pause(30); // Let window appear
                    spriteEditorWindow = Resources.FindObjectsOfTypeAll(typeof(EditorWindow))
                        .Cast<EditorWindow>()
                        .FirstOrDefault(win => win.name.Equals(k_SpriteEditorWindowName));
                    loopCount++;
                }

                Assert.NotNull(spriteEditorWindow, "Could not find Sprite Editor Window");
                spriteEditorWindow.Close();
            }
            else
            {
                Debug.LogWarning("You must install the 2D Sprite Editor package in order to run this test successfully.");
            }

            yield return null;
        }
#endif

        [UnityTest]
        public IEnumerator AssignBuiltInAssetToImageFields()
        {
            yield return LoadTestUXMLDocument(k_AllStylePropertiesTestFilePath);
            var img = AssetDatabase.GetBuiltinExtraResource<Texture2D>("UI/Skin/Knob.psd");
            Assert.NotNull(img);
            var cursorImageField = inspector.styleFields.m_StyleFields["cursor"].First() as ObjectField;
            var backgroundImageField = inspector.styleFields.m_StyleFields["background-image"].First() as ImageStyleField;
            Assert.NotNull(cursorImageField);
            Assert.NotNull(backgroundImageField);

            // Assign the built-in Knob asset to the Cursor Image.
            cursorImageField.value = img;

            // Set Background image type to Texture2D and assign the built-in Knob asset to it.
            backgroundImageField.SetTypePopupValueWithoutNotify(typeof(Texture2D));
            backgroundImageField.value = img;
        }

        [UnityTest]
        public IEnumerator InspectorShowsAttributesWhenElementIsSelected()
        {
            yield return LoadTestUXMLDocument(k_AllStylePropertiesTestFilePath);

            var element = builder.documentRootElement.Q("all-properties-inline");
            Assert.NotNull(element);

            selection.Select(null, element);

            yield return UIETestHelpers.Pause();

            PersistedFoldout attributesSection = inspector.Q<PersistedFoldout>("inspector-attributes-foldout");
            Assert.NotNull(attributesSection);

            // Pick the first input from the list (usually for the name of the element),
            // any will do since we just want to make sure the section contains something
            var input = attributesSection.Q("unity-text-input");
            Assert.NotNull(input);
        }
    }
}
