using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    internal class FoldoutNumberField : FoldoutField
    {
        public new class UxmlFactory : UxmlFactory<FoldoutNumberField, UxmlTraits> { }

        public new class UxmlTraits : FoldoutField.UxmlTraits { }

        TextField m_TextField;
        IntegerField m_DraggerIntegerField;
        string m_LastValidInput;
        public List<string> fieldValues = new List<string>(); // Keeps track of child field values inputted from the header field

        public static readonly string textUssClassName = BuilderConstants.FoldoutFieldPropertyName + "__textfield";
        static readonly string k_DraggerFieldUssClassName = BuilderConstants.FoldoutFieldPropertyName + "__dragger-field";
        static readonly string k_FieldStringSeparator = " "; // Formatting the header field with multiple values

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

        public FoldoutNumberField()
        {
            // Used for its dragger.
            var toggleInput = toggle.Q(className: "unity-toggle__input");
            m_DraggerIntegerField = new IntegerField(" ");
            m_DraggerIntegerField.name = "dragger-integer-field";
            m_DraggerIntegerField.AddToClassList(k_DraggerFieldUssClassName);
            m_DraggerIntegerField.RegisterValueChangedCallback(OnDraggerFieldUpdate);
            toggleInput.Add(m_DraggerIntegerField);

            m_TextField = new TextField();
            m_TextField.isDelayed = true; // only updates on Enter or lost focus
            m_TextField.AddToClassList(textUssClassName);
            header.hierarchy.Add(m_TextField);
        }

        // IS VALID INPUT IF:
        // - one int: e.g. 0 OR 4
        //    - Usage: This one value will be the same for all four attributes (e.g. left, top, right, bottom)
        // - four ints separated by commas: e.g. 3, 5, 3, 3
        //    - Usage: Each int corresponds respectively to the left, top, right, and bottom attributes
        public bool IsValidInput(string input)
        {
            var splitBy = new char[]{ ' ' };
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
                fieldValues.Add("auto");

            var fieldIndex = Array.IndexOf(bindingPathArray, bindingPath);
            fieldValues[fieldIndex] = newValue;

            m_LastValidInput = GetFormattedInputString();
            m_TextField.SetValueWithoutNotify(m_LastValidInput);

            int.TryParse(fieldValues[0], out int intValue);
            m_DraggerIntegerField.SetValueWithoutNotify(intValue);
        }

        public string GetFormattedInputString()
        {
            if (fieldValues.Count == bindingPathArray.Length &&
                fieldValues.All(o => o == fieldValues[0]))
                return fieldValues[0].ToString();

            return String.Join(k_FieldStringSeparator, fieldValues);
        }

        void OnDraggerFieldUpdate(ChangeEvent<int> evt)
        {
            m_TextField.value = evt.newValue.ToString();
        }
    }
}