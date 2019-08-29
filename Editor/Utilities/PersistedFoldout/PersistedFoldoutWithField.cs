using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    internal class PersistedFoldoutWithField : PersistedFoldout
    {
        public new class UxmlFactory : UxmlFactory<PersistedFoldoutWithField, UxmlTraits> { }

        public new class UxmlTraits : BindableElement.UxmlTraits
        {
            UxmlStringAttributeDescription m_Text = new UxmlStringAttributeDescription { name = "text" };
            UxmlStringAttributeDescription m_BindingPaths = new UxmlStringAttributeDescription { name = "binding-paths" };

            public override IEnumerable<UxmlChildElementDescription> uxmlChildElementsDescription
            {
                get { yield break; }
            }

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);
                ((PersistedFoldoutWithField)ve).text = m_Text.GetValueFromBag(bag, cc);

                var separator = ' ';
                ((PersistedFoldoutWithField)ve).bindingPathArray = m_BindingPaths.GetValueFromBag(bag, cc).Split(separator);

                ((PersistedFoldoutWithField)ve).ReAssignTooltipToHeaderLabel();
            }
        }

        protected TextField m_TextField;
        protected string m_LastValidInput;
        protected string[] m_BindingPathArray;
        public List<string> fieldValues = new List<string>(); // Keeps track of child field values inputted from the header field

        public static readonly string textUssClassName = BuilderConstants.PersistedFoldoutWithFieldPropertyName + "__textfield";
        private static readonly string k_FieldStringSeparator = ", "; // Formatting the header field with multiple values

        public string[] bindingPathArray
        {
            get
            {
                return m_BindingPathArray;
            }
            set
            {
                m_BindingPathArray = value;
            }
        }

        public TextField headerInputField
        {
            get
            {
                return m_TextField;
            }
        }

        public string lastValidInput
        {
            get
            {
                return m_LastValidInput;
            }
        }

        public PersistedFoldoutWithField()
        {
            m_Value = true;
            AddToClassList(BuilderConstants.PersistedFoldoutWithFieldPropertyName);

            m_TextField = new TextField();
            m_TextField.isDelayed = true; // only updates on Enter or lost focus

            m_TextField.AddToClassList(textUssClassName);
            header.hierarchy.Add(m_TextField);
            header.AddToClassList(BuilderConstants.PersistedFoldoutWithFieldHeaderClassName);
        }

        // IS VALID INPUT IF:
        // - one int: e.g. 0 OR 4
        //    - Usage: This one value will be the same for all four attributes (e.g. left, top, right, bottom)
        // - four ints separated by commas: e.g. 3, 5, 3, 3
        //    - Usage: Each int corresponds respectively to the left, top, right, and bottom attributes
        public bool IsValidInput(string input)
        {
            var splitBy = new char[]{ ',' };
            string[] inputArray = input.Split(splitBy);

            if (inputArray.Length == 1)
            {
                if (int.TryParse(input, out int intResult))
                {
                    fieldValues.Clear();
                    while (fieldValues.Count != bindingPathArray.Length)
                        fieldValues.Add(intResult.ToString());
                    return true;
                }
                return false;
            }

            if (inputArray.Length != m_BindingPathArray.Length)
                return false;

            var newValues = new List<string>();
            for (int i = 0; i < inputArray.Length; i++)
            {
                if (!int.TryParse(inputArray[i], out int intResult))
                        return false;
                newValues.Add(intResult.ToString());
            }

            fieldValues = newValues;
            return true;
        }

        public void UpdateFromChildField(string bindingPath, string newValue)
        {
            while (fieldValues.Count != bindingPathArray.Length)
            {
                fieldValues.Add("auto");
            }

            fieldValues[Array.IndexOf(bindingPathArray, bindingPath)] = newValue;

            m_LastValidInput = GetFormattedInputString();
            m_TextField.SetValueWithoutNotify(m_LastValidInput);
        }

        public string GetFormattedInputString()
        {
            if (fieldValues.Count == bindingPathArray.Length &&
                fieldValues.All(o => o == fieldValues[0]))
                return fieldValues[0].ToString();

            return String.Join(k_FieldStringSeparator, fieldValues);
        }
    }
}