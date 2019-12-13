using UnityEngine;
using System;

namespace Unity.UI.Builder
{
    [Serializable]
    internal class BuilderDocumentSettings
    {
        public string UxmlGuid;
        public string UxmlPath;
        public int CanvasX;
        public int CanvasY;
        public int CanvasWidth;
        public int CanvasHeight;

        public float CanvasBackgroundOpacity = 1.0f;
        public BuilderCanvasBackgroundMode CanvasBackgroundMode = BuilderCanvasBackgroundMode.None;
        public Color CanvasBackgroundColor = new Color(0, 0, 0, 255);
        public Texture2D CanvasBackgroundImage;
        public ScaleMode CanvasBackgroundImageScaleMode = ScaleMode.ScaleAndCrop;
        public string CanvasBackgroundCameraName;
    }
}