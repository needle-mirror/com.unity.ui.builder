using UnityEditor.UIElements.Debugger;

namespace Unity.UI.Builder
{
    internal partial class BuilderInspector
    {
        private MatchedRulesExtractor m_MatchedRulesExtractor;

        private void InitMatchingSelectors()
        {
            m_MatchedRulesExtractor = new MatchedRulesExtractor();
        }

        private void GetElementMatchers()
        {
            if (currentVisualElement == null || currentVisualElement.elementPanel == null)
                return;

            m_MatchedRulesExtractor.selectedElementRules.Clear();
            m_MatchedRulesExtractor.selectedElementStylesheets.Clear();
            m_MatchedRulesExtractor.FindMatchingRules(currentVisualElement);
        }
    }
}
