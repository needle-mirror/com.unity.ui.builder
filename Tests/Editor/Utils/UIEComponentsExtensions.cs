using System.Linq;
using UnityEngine.UIElements;

namespace Unity.UI.Builder.EditorTests
{
    internal static class TreeViewExtensions
    {
        public static ITreeViewItem GetSelectedItem(this TreeView treeView)
        {
#if UNITY_2020_1_OR_NEWER
            return treeView.selectedItem;
#else
            return treeView.currentSelection.FirstOrDefault();
#endif
        }
    }
}
