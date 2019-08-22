using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    internal class ButtonStrip : VisualElement
    {
        private static readonly string s_UssPathNoExt = BuilderConstants.UtilitiesPath + "/ButtonStrip/ButtonStrip";
#if UNITY_2019_3_OR_NEWER
        private static readonly string s_UssPath = s_UssPathNoExt + ".uss";
        private static readonly string s_UssDarkPath = s_UssPathNoExt + "Dark.uss";
        private static readonly string s_UssLightPath = s_UssPathNoExt + "Light.uss";
#else
        private static readonly string s_UssPath = s_UssPathNoExt + "2019_2.uss";
        private static readonly string s_UssDarkPath = s_UssPathNoExt + "Dark2019_2.uss";
        private static readonly string s_UssLightPath = s_UssPathNoExt + "Light2019_2.uss";
#endif

        private static readonly string s_UssClassName = "unity-button-strip";
        private static readonly string s_ButtonClassName = s_UssClassName + "__button";
        private static readonly string s_ButtonLeftClassName = s_ButtonClassName + "--left";
        private static readonly string s_ButtonMidClassName = s_ButtonClassName + "--mid";
        private static readonly string s_ButtonRightClassName = s_ButtonClassName + "--right";
        private static readonly string s_ButtonIconClassName = s_UssClassName + "__button-icon";

        private List<string> m_Choices = new List<string>();
        private List<string> m_Labels = new List<string>();

        public IEnumerable<string> choices
        {
            get { return m_Choices; }
            set
            {
                m_Choices.Clear();

                if (value == null)
                    return;

                m_Choices.AddRange(value);

                RecreateButtons();
            }
        }

        public IEnumerable<string> labels
        {
            get { return m_Labels; }
            set
            {
                m_Labels.Clear();

                if (value == null)
                    return;

                m_Labels.AddRange(value);

                RecreateButtons();
            }
        }

        public Action<EventBase> onButtonClick { get; set; }

        public ButtonStrip() : this(null)
        {
        }

        public ButtonStrip(IList<string> choices)
        {
            AddToClassList(s_UssClassName);

            styleSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet>(s_UssPath));
            if (EditorGUIUtility.isProSkin)
                styleSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet>(s_UssDarkPath));
            else
                styleSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet>(s_UssLightPath));

            this.choices = choices;
        }

        private void RecreateButtons()
        {
            this.Clear();
            for (int i = 0; i < m_Choices.Count; ++i)
            {
                var choice = m_Choices[i];
                string label = null;
                if (m_Labels.Count > i)
                    label = m_Labels[i];

                var button = new Button();
                button.AddToClassList(s_ButtonClassName);

                // Set button name for styling.
                button.name = choice;

                // Set tooltip.
                button.tooltip = choice;

                if (i == 0)
                    button.AddToClassList(s_ButtonLeftClassName);
                else if (i == m_Choices.Count - 1)
                    button.AddToClassList(s_ButtonRightClassName);
                else
                    button.AddToClassList(s_ButtonMidClassName);

                if (onButtonClick != null)
                    button.clickable.clickedWithEventInfo += onButtonClick;
                this.Add(button);

                if (string.IsNullOrEmpty(label))
                {
                    var icon = new VisualElement();
                    icon.AddToClassList(s_ButtonIconClassName);
                    button.Add(icon);
                }
                else
                {
                    button.text = label;
                }
            }
        }

        private void OnOptionChange(EventBase evt)
        {
            var button = evt.target as Button;
            var newValue = button.name;

            ToggleButtonStates(button);
        }

        protected virtual void ToggleButtonStates(Button button)
        {
            button.pseudoStates ^= PseudoStates.Checked;
            button.IncrementVersion(VersionChangeType.Styles);
        }
    }
}