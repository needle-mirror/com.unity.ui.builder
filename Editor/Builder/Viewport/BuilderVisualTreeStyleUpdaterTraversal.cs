#if !UI_BUILDER_PACKAGE || ((UNITY_2021_1_OR_NEWER && UIE_PACKAGE) || UNITY_2021_2_OR_NEWER)
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    class BuilderVisualTreeStyleUpdaterTraversal : VisualTreeStyleUpdaterTraversal
    {
        struct SavedContext
        {
            public static SavedContext none = new SavedContext();
            public List<StyleSheet> styleSheets;
            public StyleVariableContext variableContext;
        }

        SavedContext m_SavedContext = SavedContext.none;
        VisualElement m_DocumentElement;

        public BuilderVisualTreeStyleUpdaterTraversal(VisualElement document)
        {
            m_DocumentElement = document;
        }

        void SaveAndClearContext()
        {
            var originalStyleSheets = new List<StyleSheet>();
            var originalVariableContext = styleMatchingContext.variableContext;

            for (var index = 0; index < styleMatchingContext.styleSheetCount; index++)
            {
                originalStyleSheets.Add(styleMatchingContext.GetStyleSheetAt(index));
            }

            styleMatchingContext.RemoveStyleSheetRange(0, styleMatchingContext.styleSheetCount);
            styleMatchingContext.variableContext = StyleVariableContext.none;

            m_SavedContext = new SavedContext() { styleSheets = originalStyleSheets, variableContext = originalVariableContext };
        }

        void RestoreSavedContext()
        {
            styleMatchingContext.RemoveStyleSheetRange(0, styleMatchingContext.styleSheetCount);
            foreach (var sheet in m_SavedContext.styleSheets)
            {
                styleMatchingContext.AddStyleSheet(sheet);
            }
            styleMatchingContext.variableContext = m_SavedContext.variableContext;
            m_SavedContext = SavedContext.none;
        }

        public override void TraverseRecursive(VisualElement element, int depth)
        {
            if (ShouldSkipElement(element))
            {
                return;
            }

            // In order to ensure that only the selected preview theme is applied to the document content in the viewport, 
            // we clear the current style context to prevent the document element from inheriting from the actual Unity Editor theme.
            bool shouldClearStyleContext = element == m_DocumentElement && m_DocumentElement.styleSheets.count != 0;

            if (shouldClearStyleContext)
            {
                SaveAndClearContext();
            }
            try
            {
                base.TraverseRecursive(element, depth);
            }
            finally
            {
                if (shouldClearStyleContext)
                {
                    RestoreSavedContext();
                }
            }
        }
    }
}
#endif
