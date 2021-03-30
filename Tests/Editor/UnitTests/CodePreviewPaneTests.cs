using NUnit.Framework;

namespace Unity.UI.Builder.EditorTests
{
    class CodePreviewPaneTests
    {
        [Test]
        public void EnsureTextContentWithinLimitIsNotClipped()
        {
            // Verify that the text is not truncated if the number of printable characters does not exceed the maximum allowed
            var text = new string('a', BuilderConstants.MaxTextPrintableCharCount);
            var clampedText = BuilderCodePreview.GetClampedText(text, out var truncated);
            Assert.False(truncated);
            Assert.True(clampedText == text);

            // Verify that non printable characters are ignored
             text = text.PadLeft(100, ' ')
                .PadRight(100, '\n')
                .PadRight(100, '\t');
            clampedText = BuilderCodePreview.GetClampedText(text, out truncated);
            Assert.False(truncated);
            Assert.True(clampedText == text);
        }

        [Test]
        public void EnsureTextContentExceedingLimitIsClipped()
        {
            var partA = new string('a', BuilderConstants.MaxTextPrintableCharCount);
            var partB = "b";

            // Verify that the text is truncated if the number of printable characters exceeds the maximum allowed
            var clampedText = BuilderCodePreview.GetClampedText(partA + partB, out var truncated);
            Assert.True(truncated);
            Assert.True(clampedText == partA);

            // Verify that non printable characters are ignored
            partA = partA.PadLeft(100, ' ')
                .PadRight(100, '\n')
                .PadRight(100, '\t');
            clampedText = BuilderCodePreview.GetClampedText(partA + partB, out truncated);
            Assert.True(truncated);
            Assert.True(clampedText == partA);
        }
    }
}
