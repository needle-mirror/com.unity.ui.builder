// For most cases, except on 2021.1, we don't need to explicitly register UxmlFactories.  The only case where this is
// not true (2021.1) is when UI Builder was included directly in the editor (assembly overrides) but UI Toolkit wasn't
// (it was only available as a package and the code that replaced the manual registration of UxmlFactories was not
// available via that workflow).
#if UI_BUILDER_PACKAGE && !UIE_PACKAGE && UNITY_2021_1
using UnityEditor;
using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    [InitializeOnLoad]
    internal class BuilderUXMLEditorFactories
    {
        private static readonly bool k_Registered;

        static BuilderUXMLEditorFactories()
        {
            if (k_Registered)
                return;

            k_Registered = true;

            IUxmlFactory[] factories =
            {
                new BuilderPane.UxmlFactory(),
                new BuilderNewSelectorField.UxmlFactory(),
                new BuilderStyleRow.UxmlFactory(),
                new BuilderAnchorer.UxmlFactory(),
                new BuilderMover.UxmlFactory(),
                new BuilderParentTracker.UxmlFactory(),
                new BuilderResizer.UxmlFactory(),
                new BuilderSelectionIndicator.UxmlFactory(),
                new BuilderCanvasStyleControls.UxmlFactory(),
                new BuilderTooltipPreview.UxmlFactory(),
                new BuilderCanvas.UxmlFactory(),
                new BuilderNotifications.UxmlFactory(),
                new CheckerboardBackground.UxmlFactory(),
                new OverlayPainterHelperElement.UxmlFactory(),
                new FoldoutField.UxmlFactory(),
                new FoldoutColorField.UxmlFactory(),
                new FoldoutNumberField.UxmlFactory(),
                new FoldoutWithCheckbox.UxmlFactory(),
                new FontStyleStrip.UxmlFactory(),
                new HelpBox.UxmlFactory(),
                new LibraryFoldout.UxmlFactory(),
                new ModalPopup.UxmlFactory(),
                new PercentSlider.UxmlFactory(),
                new PersistedFoldout.UxmlFactory(),
                new BuilderAttributesTestElement.UxmlFactory(),
                new BuilderPlacementIndicator.UxmlFactory(),
                new DimensionStyleField.UxmlFactory(),
                new ImageStyleField.UxmlFactory(),
                new NumericStyleField.UxmlFactory(),
                new IntegerStyleField.UxmlFactory(),
                new TextAlignStrip.UxmlFactory(),
                new TextShadowStyleField.UxmlFactory(),
                new ToggleButtonStrip.UxmlFactory(),
                new TwoPaneSplitView.UxmlFactory(),
                new UnityUIBuilderSelectionMarker.UxmlFactory()
            };

            foreach (IUxmlFactory factory in factories)
            {
                VisualElementFactoryRegistry.RegisterFactory(factory);
            }
        }
    }
}
#endif
