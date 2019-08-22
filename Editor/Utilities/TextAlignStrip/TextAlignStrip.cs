using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    internal class TextAlignStrip : BaseField<string>, IToggleButtonStrip
    {
        private static readonly string s_UssPath = BuilderConstants.UtilitiesPath + "/TextAlignStrip/TextAlignStrip.uss";

        private static readonly string s_UssClassName = "unity-text-align-strip";
        private static readonly string s_ButtonStripContainerClassName = s_UssClassName + "__button-strip-container";

        private static readonly List<string> s_HorizontalChoices = new List<string>() { "left", "center", "right" };
        private static readonly List<string> s_VerticalChoices = new List<string>() { "upper", "middle", "lower" };

        public new class UxmlFactory : UxmlFactory<TextAlignStrip, UxmlTraits> { }

        private VisualElement m_ButtonStripContainer;
        private ToggleButtonStrip m_HorizontalButtonStrip;
        private ToggleButtonStrip m_VerticalButtonStrip;

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
            }
        }

        public Type enumType { get; set; }

        public TextAlignStrip() : this(null) { }

        public TextAlignStrip(string label) : base(label)
        {
            AddToClassList(s_UssClassName);

            styleSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet>(s_UssPath));

            m_ButtonStripContainer = new VisualElement();
            m_ButtonStripContainer.AddToClassList(s_ButtonStripContainerClassName);

            m_HorizontalButtonStrip = new ToggleButtonStrip();
            m_HorizontalButtonStrip.name = "horizontal-align-strip";
            m_HorizontalButtonStrip.RegisterValueChangedCallback(OnHorizontalValueChange);
            m_HorizontalButtonStrip.choices = s_HorizontalChoices;
            m_ButtonStripContainer.Add(m_HorizontalButtonStrip);

            m_VerticalButtonStrip = new ToggleButtonStrip();
            m_VerticalButtonStrip.name = "vertical-align-strip";
            m_VerticalButtonStrip.RegisterValueChangedCallback(OnVerticalValueChange);
            m_VerticalButtonStrip.choices = s_VerticalChoices;
            m_ButtonStripContainer.Add(m_VerticalButtonStrip);

            visualInput = m_ButtonStripContainer;
        }

        public override void SetValueWithoutNotify(string newValue)
        {
            base.SetValueWithoutNotify(newValue);

            var values = newValue.Split('-');
            m_VerticalButtonStrip.SetValueWithoutNotify(values[0]);
            m_HorizontalButtonStrip.SetValueWithoutNotify(values[1]);
        }

        private void OnHorizontalValueChange(ChangeEvent<string> evt)
        {
            var newValue = m_VerticalButtonStrip.value + "-" + evt.newValue;
            evt.StopPropagation();
            value = newValue;
        }

        private void OnVerticalValueChange(ChangeEvent<string> evt)
        {
            var newValue = evt.newValue + "-" + m_HorizontalButtonStrip.value;
            evt.StopPropagation();
            value = newValue;
        }
    }
}