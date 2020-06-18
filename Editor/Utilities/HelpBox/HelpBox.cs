using UnityEditor;
using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    class HelpBox : BindableElement
    {
        public new class UxmlFactory : UxmlFactory<HelpBox, UxmlTraits> { }
        public new class UxmlTraits : BindableElement.UxmlTraits
        {
            readonly UxmlStringAttributeDescription m_Text = new UxmlStringAttributeDescription { name = "text" };

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);
                ((HelpBox)ve).Text = m_Text.GetValueFromBag(bag, cc);
            }
        }

        public string Text { get; set; }

        public HelpBox()
        {
            Add(new IMGUIContainer(() =>
            {
                EditorGUILayout.HelpBox(Text, MessageType.Info, true);
            }));
        }
    }
}
