# Full Feature List

## Global

1. Can delete element via Delete key.
1. Can cut/copy/duplicate/paste element via keyboard shortcut. The copied element and its children are pasted as children of the parent of the currently selected element. If nothing is selected, they are pasted at the root.
1. Can cut/copy/duplicate/paste Template instances, adding the correct Template registrations to the file.
1. Can copy/paste elements to and from a text file as valid UXML.
1. Can undo/redo changes to styles, hierarchy, and selection.
1. Open documents, the current selection, and unsaved changes survive domain reload.
1. Double-clicking a `.uxml` asset in the Project opens it with UI Builder. Double-clicking a `.uxml` that is already opened in UI Builder window will open the file in the default IDE.
1. Previously open document is re-opened after a Unity Editor restart.
1. Renaming, moving, or deleting a `.uxml` or `.uss` that is currently open in the UI Builder will give you the option to abort the operation or reset the Builder and lose any unsaved changes.
1. A dialog to Save/DontSave/Cancel will be shown if there are unsaved changes in the UI Builder, even if the UI Builder window is not open.

## StyleSheets

1. Can select the main StyleSheet via the root item that should have the name of the .uss file. This displays its dedicated Inspector.
1. Selectors get draggable style class pills for each selector part that is a style class name.
1. In the StyleSheets pane, you can select selectors by clicking on the row or a style class pill.
1. Can drag a style class pill from the StyleSheets pane onto an element in the Viewport to add the class.
1. Can drag a style class pill from the StyleSheets pane onto an element in the Hierarchy to add the class.
1. Dragging a style class onto an element inside a template instance or C# type in the Viewport adds it to the parent instance or C# element.
1. Dragging a style class onto an element inside a template instance or C# type in the Hierarchy does nothing.
1. In the toolbar of the StyleSheets pane there's a field that lets you create new selectors.
    1. After the field is focused, the explanation text is replaced with a default `.` and the cursor is set right after the `.` to let you quickly add a class-based selector.
    1. To commit and add your new selector, you can click on the **Add** button or press **Enter**.
    1. You can discover and append `:pseudoStates` from the **States** menu.
    1. While the text field is selected, you should see a large tooltip displaying the selector cheatsheet.
1. When selecting or hovering over a style selector in the StyleSheets pane, all elements in the Canvas that match the selector are highlighted.
1. With a selector selected, you can use standard short-cuts or the Edit menu to copy/paste/duplicate/delete it. You can also copy/paste the USS for the selector to/from a text file.
1. Right-clicking anywhere in the StyleSheets pane opens the Copy/Paste/Duplicate/Delete context menu.
1. Selecting an element or a the main document (VisualTreeAsset) should deselect any selected tree items in the StyleSheets pane.

## Hierarchy

1. The root items should display the currently loaded .uxml filename.
1. Can click to select an element.
1. Can drag element onto other elements in the Hierarchy to re-parent.
1. Can drag an element between other elements to reorder, with live preview in the Canvas.
1. Can drag an element onto other elements in the Viewport to re-parent.
1. Elements are displayed using their #name in blue. If they have no name, they are displayed using their C# type in white.
1. Elements are displayed grayed out if they are children of a template instance or C# type.
1. Selecting an element inside a template instance or C# type displays the Inspector in read-only (disabled) mode.
1. Dragging an element onto a template instance or C# type element in the Viewport re-parents it to the parent instance or C# element.
1. Dragging an element onto a template instance or C# type element in the Hierarchy does nothing.
1. With an element selected, you can use the standard short-cuts and Edit menu to copy/paste/duplicate/delete it. You can also copy/paste the UXML for the element to/from a text file.
1. Right-clicking anywhere in the Hierarchy opens the Copy/Paste/Duplicate/Delete/Rename context menu.
1. Can double-click on an item to rename it.
1. Selecting an style selector or a the main StyleSheet in the StyleSheets pane should deselect any selected tree items in the Hierarchy.

## Library

1. Displays built-in elements under a **Unity** heading.
1. Displays project-defined factory elements and UXML files (with `.uxml` extension) under a **Project** heading. This includes assets inside the `Assets/` and `Packages/` folders.
1. Can double click to create a new element instance at the root.
1. Items that have corresponding `.uxml` assets have an "Open" button visible that opens the asset for editing in UI Builder. The currently open `.uxml` asset in the Library is grayed out and is not instantiable to prevent infinite recursion.
1. Can click and drag onto a Viewport element to create new instance as a child.
1. Can click and drag onto a Hierarchy element to create new instance as a child, or between elements to create as a sibling.
1. Can create (double-click or drag) template instances from other `.uxml` files.
1. When creating a new empty VisualElement, it has an artificial minimum size and border which is reset as soon as you parent a child element under it or change its styling.
1. Hovering over items in the Library shows a preview of that element in a floating preview box. The preview uses the current Theme selected for the Canvas.
1. Library pane updates if new `.uxml` files are added/deleted/moved/renamed to/from the project.

## Viewport

### Header

1. The currently open UXML asset name, or `<unsaved asset>`, is displayed in the Viewport header, grayed out.
1. If there are unsaved changes, a `*` is appended to the asset name.
1. The current UI Builder package version is displayed in the **Viewport** title bar.

### Toolbar

1. Selecting **File > New** clears the selection, the Viewport canvas, the StyleSheets pane, the Hierarchy, and all undo/redo stack operations for the previous document. A prompt is displayed if there are unsaved changes.
1. Selecting **File > Save** asks for new file names for USS and UXML if it is the first save, otherwise, it overwrites the previously saved/loaded files.
1. Saving should work even if the opened assets have been moved or renamed (in which case, the UI Builder should update the USS Style paths inside the UXML document).
1. Selecting **File > Save As...** always asks for a new file name and saves as a copy of the current document.
1. Selecting **File > Open...** displays an Open File Dialog and lets you select a `.uxml` asset inside your Project.
1. Can select a zoom level from the **100%** dropdown. Can also zoom via the mouse scroll wheel and Alt + RightClick + Mouse Move.
1. Can reset the view and the canvas size via the **Reset** button.
1. Can reset the view and make sure the canvas fits the viewport with the **Fit Canvas** button.
1. Can preview Light/Dark/Runtime themes inside the Canvas via the **Theme** popup field, independent from the current Editor Theme. **Default Theme** uses the current Editor Theme, while the other options force a theme to be used in the Canvas. If the runtime package is not installed, the Runtime theme will be substituted by the Light Editor theme.
1. Pressing **Preview** toggles _Preview_ mode, where you can no longer select elements by clicking them in the Viewport. Instead, Viewport elements receive regular mouse and focus events.

### Save Dialog

1. Entering paths that do not start with `Assets/` or `Packages/` shows an invalid path message and disables the Save button.
1. Updating the **UXML Path** field automatically updates the **USS Path** field to match, until the **USS Path** field is changed manually.
1. Entering file names without extensions still adds the correct extensions when creating the assets.
1. The ellipsis "**...**" button beside each path field should bring up the system **Save File** Dialog.
1. Folders in an otherwise valid path are created if missing.

### Canvas

1. Can be resized via handles on the right and bottom.
1. Canvas has a minimum size.
1. Right-clicking an element in the Canvas opens the Copy/Paste/Duplicate/Delete/Rename context menu.
1. Can click to select element.
1. Selecting an element inside a template instance or C# type selects the parent instance or C# element.
1. Relative position elements have bottom, right, and bottom-right handles that change inline `height` and `width` styles.
1. Absolute position elements have all four side handles and all four corner handles visible.
1. Absolute position elements have four anchor handles visible to set or unset the `left`/`right`/`top`/`bottom` inline styles.
1. Absolute position elements can be moved by clicking and dragging, changing `top`/`right`/`left`/`bottom` inline styles depending on anchor state.
1. Resize and position handles change different styles depending on anchor state (ie. if `left` and `right` styles are set, changing the width changes the `right` style - otherwise, changing the width changes the `width` style).
1. Canvas size is restored after Domain Reload or Window reload. It is reset when opening a new document.
1. When changing Width or Height in the Inspector, the corresponding resize handles in the canvas are highlighted.
1. When hovering over elements in the Canvas, the corresponding entry in the Hierarchy is highlighted.
1. When hovering over elements in the Canvas, all StyleSheets pane entries of style selectors that match this element are highlighted.
1. Canvas size is remembered for each asset and restored when loading the asset. It also means it survives Editor restarts.
1. Clicking the root item (with the .uxml filename) in the Hierarchy displays the Canvas options in the Inspector:
    1. Can see and change the Canvas height and width.
    1. Can set the custom Canvas background color/image Opacity.
    1. Can set the Canvas background to be a solid color via the Color Background mode.
    1. Can set the Canvas background to be an image, can set the ScaleMode of the image, and can have the Canvas resize to match the image via the **Fit Canvas to Image** button.
    1. Can set the Canvas background to be a render texture for a chosen Camera.
    1. All of these settings are remembered next time you open the same UXML document.

### Viewport Surface

1. Can pan by holding down middle mouse button in the Viewport and moving the mouse.
1. Can pan by holding down Ctrl + Alt + LeftClick and moving the mouse.
1. Can zoom in and out witht he mouse scrollwheel.
1. Can zoom in and out by holding down Alt + RightClick and moving the mouse right and left.

## Previews

### UXML

1. Updates text on any changes to hierarchy, attributes, or inline styles.
1. Shows unsaved StyleSheet as path="&#42;unsaved in-memory StyleSheet with...".
1. Upon saving, all unsaved StyleSheet paths are fixed.
1. Shows `<Style>` tags for all root elements.
1. The `UnityEngine.UIElements` namespace is aliased to `ui:` and `UnityEditor.UIElements` namespace is aliased to `uie:`.
1. Custom C# elements not in the `UnityEngine.UIElements` and `UnityEditor.UIElements` namespaces have no namespace alias and appear as their full type.
1. (2019.3+) A relative path to a `.uss` asset is used in the `src` attribute of the `<Style>` tag if the asset is in the same folder as the main asset, or a subfolder of that folder. Otherwise, an absolute path is used.
1. (2019.3+) A relative path to a `.uxml` asset is used in the `src` attribute of the `<Template>` tag if the asset is in the same folder as the main asset, or a subfolder of that folder. Otherwise, an absolute path is used.
1. Pane header displays the name of the `.uxml` asset being previewed.
1. If asset is saved on disk, a button to open the `.uxml` asset in the default IDE will appear in the top-right corner of the pane header.

### USS

1. Updates on all StyleSheet/Selector changes.
1. Dimension (Length) styles have the unit added to the USS (`px`). (No support for `%` yet.)
1. Pane header displays the name of the `.uss` asset being previewed.
1. If asset is saved on disk, a button to open the `.uss` asset in the default IDE will appear in the top-right corner of the pane header.

## Inspector

### StyleSheet Inspector

1. Only visible if the selection is a StyleSheet (by selecting the root item in the StyleSheets pane).
1. Can create new Selectors by entering text in the **Selector** field and pressing Enter (or the **Create** button).
1. Shows quick help on selectors.

### Style Selector Inspector

1. Only visible if the selection is a selector in the current StyleSheet.
1. Can change the selector text by changing the **Selector** field and pressing Enter.

### Attributes Section

1. Only visible if the selection is an element in the current document.
1. Shows all valid attributes for the selected element, given its C# type.
1. Attributes already set to a non-default value are highlighted with the same styling as Prefab overrides.
1. Changing attributes updates the Hierarchy (or the StyleSheets pane), the Viewport, and the UXML Preview and changes are immediate.
1. Right-clicking **Unset** on an attribute removes it from the UXML tag, resets the value to the element-defined default, and resets the override styling.
1. Right-clicking **Unset All** on an attribute is the same as **Unset** except it unsets all set attributes.

### StyleSheet Section

1. Only visible if the selection is an element in the current document.
1. Can add existing style class to element by typing the name of the class in the field inside the **StyleSheet** section and pressing the **Add Style Class to List** button (or pressing Enter).
1. Can extract all overwritten **Inlined Styles** to a new style class by typing the name of the class in the field inside **StyleSheet** and pressing the **Extract Inlined Styles to New Class** button.
1. If the style class being added to an element is not valid, an error message appears.
1. All style classes on the current element are displayed as pills.
1. Style class pills have an **X** button that lets you remove them from the element.
1. Under **Matching Selectors**, all matching selectors on the current element are displayed with read-only fields for their properties.
1. Style class pills in the **StyleSheet** section show faded if there is no single-class selector in the main StyleSheet.
1. Double-clicking on a style class pill in the **StyleSheet** section selects the corresponding single-class selector in the main StyleSheet, if one exists, otherwise it creates it.

### (Inlined) Styles Section

1. Only visible if the selection is an element in the current document, or a selector in the current StyleSheet (in this case, the tile will change to just **Styles**).
1. Changing any value sets it in the StyleSheet or inline UXML style attribute and highlights it with a solid bar on the left side and bold font.
1. Style category headers have an override bar and bold font if any child styles are overridden.
1. All style value types are supported.
1. Sub-section foldout expanded states are preserved between selection changes and domain reload.
1. Right-clicking **Unset** on an style field removes it from the UXML inline style or StyleSheet, resets the value to default, and resets the override styling.
1. Right-clicking **Unset All** on a style field is the same as **Unset** except it unsets all overridden style fields.
1. Align section toggle button strips change icons depending on the value of the flex-direction style.
1. Length style fields have a dropdown to select **Keyword** or **Unit**.
1. (2019.3+) Some Length style fields support the `%` **Unit**.
1. Foldout style fields (like Margin and Padding) properly add the unit or keyword for each child style property.