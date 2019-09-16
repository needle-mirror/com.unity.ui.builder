using UnityEngine.UIElements;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;

namespace Unity.UI.Builder
{
    internal class BuilderPaneContent : VisualElement
    {
        BuilderPane m_Pane;
        List<VisualElement> m_Focusables = new List<VisualElement>();
        VisualElement m_PrimaryFocusable;

        public VisualElement primaryFocusable
        {
            get { return m_PrimaryFocusable; }
            protected set
            {
                if (m_PrimaryFocusable == value)
                    return;

                if (m_PrimaryFocusable != null)
                {
                    m_PrimaryFocusable.UnregisterCallback<FocusEvent>(OnChildFocus);
                    m_PrimaryFocusable.UnregisterCallback<BlurEvent>(OnChildBlur);
                }

                m_PrimaryFocusable = value;
                m_PrimaryFocusable.RegisterCallback<FocusEvent>(OnChildFocus);
                m_PrimaryFocusable.RegisterCallback<BlurEvent>(OnChildBlur);
            }
        }

        public BuilderPane pane
        {
            get { return m_Pane; }
        }

        public BuilderPaneContent()
        {

        }

        protected void AddFocusable(VisualElement focusable)
        {
            m_Focusables.Add(focusable);

            focusable.RegisterCallback<FocusEvent>(OnChildFocus);
            focusable.RegisterCallback<BlurEvent>(OnChildBlur);
            focusable.RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
        }

        protected void RemoveFocusable(VisualElement focusable)
        {
            if (!m_Focusables.Remove(focusable))
                return;

            focusable.UnregisterCallback<FocusEvent>(OnChildFocus);
            focusable.UnregisterCallback<BlurEvent>(OnChildBlur);
            focusable.UnregisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
        }

        protected override void ExecuteDefaultAction(EventBase evt)
        {
            base.ExecuteDefaultAction(evt);

            if (evt.eventTypeId == AttachToPanelEvent.TypeId())
            {
                m_Pane = GetFirstAncestorOfType<BuilderPane>();
                m_Pane.RegisterCallback<FocusEvent>(OnPaneFocus);
            }
        }

        void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            var focusable = evt.target as VisualElement;
            focusable.UnregisterCallback<FocusEvent>(OnChildFocus);
            focusable.UnregisterCallback<BlurEvent>(OnChildBlur);
        }

        void OnChildFocus(FocusEvent evt)
        {
            m_Pane.pseudoStates = m_Pane.pseudoStates | PseudoStates.Focus;

            var elementAskingForFocus = this.Q(className: BuilderConstants.PaneContentPleaseRefocusElementClassName);
            if (elementAskingForFocus == null)
                return;

            elementAskingForFocus.RemoveFromClassList(BuilderConstants.PaneContentPleaseRefocusElementClassName);
            elementAskingForFocus.Focus();
        }

        void OnChildBlur(BlurEvent evt)
        {
            m_Pane.pseudoStates = m_Pane.pseudoStates & ~PseudoStates.Focus;
        }

        void OnPaneFocus(FocusEvent evt)
        {
            primaryFocusable?.Focus();
        }
    }
}