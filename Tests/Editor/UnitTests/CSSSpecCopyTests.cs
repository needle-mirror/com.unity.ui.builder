using NUnit.Framework;
using UnityEngine.UIElements;

namespace Unity.UI.Builder.EditorTests
{
    public class CSSSpecCopyTests
    {
        [Test]
        public void AcceptedTypeSelectors()
        {
            StyleSelectorPart[] parts;

            // Single character selectors should be valid
            CSSSpecCopy.ParseSelector("a", out parts);
            Assert.True(parts.Length == 1);
            Assert.True(parts[0].value == "a");

            // Single character can't be a symbol
            CSSSpecCopy.ParseSelector("@", out parts);
            Assert.True(parts == null || parts.Length == 0);

            // Type can start with a @ if type is a keyword
            CSSSpecCopy.ParseSelector("@class", out parts);
            Assert.True(parts.Length == 1);
            Assert.True(parts[0].value == "@class");

            // Regular case
            CSSSpecCopy.ParseSelector("MyTypeSelector", out parts);
            Assert.True(parts.Length == 1);
            Assert.True(parts[0].value == "MyTypeSelector");

            // Types can't start with a dash
            CSSSpecCopy.ParseSelector("-aa", out parts);
            foreach (var part in parts)
            {
                if (part.value == "-aa")
                    Assert.Fail("Selector with a dash is not accepted");
            }
        }
    }
}
