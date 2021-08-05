using System.Collections;
using System.Linq;
using UnityEngine.UIElements;

namespace Unity.UI.Builder.EditorTests
{
    static class TreeViewExtensions
    {
        public static ITreeViewItem GetSelectedItem(this TreeView treeView)
        {
#if UNITY_2019_4
            return treeView.currentSelection.FirstOrDefault();
#else
            return treeView.selectedItems.FirstOrDefault();
#endif
        }

#if !UI_BUILDER_PACKAGE || UNITY_2021_2_OR_NEWER
        public static ITreeViewItem GetSelectedItem(this InternalTreeView treeView)
        {
            return treeView.selectedItems.FirstOrDefault();
        }
#endif

        public static IEnumerator SelectAndScrollToItemWithId(this TreeView treeView, int id)
        {
#if UNITY_2019_4
            // This is the only way to scroll to the item.
            treeView.SelectItem(id);
#else
            treeView.SetSelection(id);
            yield return UIETestHelpers.Pause(1);
            treeView.ScrollToItem(id);
#endif
            yield return UIETestHelpers.Pause(1);
        }

#if !UI_BUILDER_PACKAGE || UNITY_2021_2_OR_NEWER
        public static IEnumerator SelectAndScrollToItemWithId(this InternalTreeView treeView, int id)
        {
            treeView.SetSelection(id);
            yield return UIETestHelpers.Pause(1);
            treeView.ScrollToItem(id);
            yield return UIETestHelpers.Pause(1);
        }
#endif
    }
}
