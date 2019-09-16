using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    internal class BuilderPane : VisualElement
    {
        static readonly string s_UssClassName = "unity-builder-pane";

        Label m_Title;
        Label m_SubTitle;
        VisualElement m_Container;

        public new class UxmlFactory : UxmlFactory<BuilderPane, UxmlTraits> {}

        public new class UxmlTraits : VisualElement.UxmlTraits
        {
            UxmlStringAttributeDescription m_Title = new UxmlStringAttributeDescription { name = "title" };

            public override IEnumerable<UxmlChildElementDescription> uxmlChildElementsDescription
            {
                get { yield break; }
            }

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);
                ((BuilderPane)ve).title = m_Title.GetValueFromBag(bag, cc);
            }
        }

        public string title
        {
            get { return m_Title.text; }
            set { m_Title.text = value; }
        }

        public string subTitle
        {
            get { return m_SubTitle.text; }
            set { m_SubTitle.text = value; }
        }

        public BuilderPane()
        {
            AddToClassList(s_UssClassName);

            var visualAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                BuilderConstants.UIBuilderPackagePath + "/BuilderPane.uxml");
            visualAsset.CloneTree(this);

            m_Title = this.Q<Label>("title");
            m_SubTitle = this.Q<Label>("sub-title");
            m_Container = this.Q("content-container");

            focusable = true;
        }

        public override VisualElement contentContainer => m_Container;
    }
}
