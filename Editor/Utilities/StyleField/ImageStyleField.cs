using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    [UsedImplicitly]
    class ImageStyleField : MultiTypeField
    {
        [UsedImplicitly]
        public new class UxmlFactory : UxmlFactory<ImageStyleField, UxmlTraits> {}

        public ImageStyleField() : this(null) {}

        public ImageStyleField(string label) : base(label)
        {
            AddType(typeof(Texture2D), "Texture");
        }

        public void TryEnableVectorGraphicTypeSupport()
        {
            AddType(typeof(VectorImage), "Vector");
        }
    }
}
