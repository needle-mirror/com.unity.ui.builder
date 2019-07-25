# UI Builder

UI Builder lets you visually create and edit UI using UIElements, UXML, and USS.

# Feature List

## Global

1. Can delete element via Delete key.
1. Can cut/copy/duplicate/paste element via hot key, with the copied element (and all its children) being pasted as a child to the the parent of the currently selected element (or root if nothing is selected).
1. Can cut/copy/duplicate/paste Template instances, adding the correct Template registrations to the file.
1. Can copy/paste elements to and from a text file as valid UXML.
1. Can undo/redo style changes, hierarchy changes, and selection changes.
1. Open documents, current selection, and unsaved changes will survive domain reload.
1. Double-clicking a .uxml asset in the Project will open it using the UI Builder.

## Explorer

### Shared Styles

1. Can select the main StyleSheet by the Shared Styles header, showing its dedicated Inspector.
1. Selectors with .classNames get pills for each class created in the Explorer.
1. In the Explorer, you can select selectors by clicking on the row or a class pill.
1. Can drag a style class pill from the Explorer onto an element in the Viewport to add the class.
1. Can drag a style class pill from the Explorer onto an element in the Hierarchy to add the class.
1. Dragging a style class onto an element inside a template instance or C# type in the Viewport adds it to the parent instance or C# element.
1. Dragging a style class onto an element inside a template instance or C# type in the Hierarchy does nothing.

### Hierarchy

1. Can click to select element.
1. Can drag element onto other elements in the Hierarchy to reparent.
1. Can drag element between other elements to reorder, with live preview in the canvas.
1. Can drag element onto other elements in the Viewport to reparent.
1. Elements are displayed using their #name in blue, or C# type in white if they have no name.
1. Elements are displayed grayed out if they are children of a template instance or C# type.
1. Selecting an element inside a template instance or C# type selects the parent instance or C# element.
1. Dragging element onto a template instance or C# type element in the Viewport reparents it to the parent instance or C# element.
1. Dragging element onto a template instance or C# type element in the Hierarchy does nothing.

## Library

1. Displays built-in elements under a **Unity** heading.
1. Displays project defined factory elements and UXML files (with .uxml extension) under a **Project** heading.
1. Can double click to create new element instance in the root.
1. Can click-drag onto a Viewport element to create new instance as a child.
1. Can click-drag onto a Hierarchy element to create new instance as a child, or between elements to create as a sibling.
1. Can create (double-click or drag) template instances from other uxml files.
1. When creating a new empty VisualElement, it will have an artificial minimum size and border which will be reset as soon as you parent a child element under it or change its styling.
1. Hovering over items in the Library will show a preview of that element in a floating preview box. The preview will use the current Theme selected for the canvas.
1. Library pane will update if new uxml files are added/deleted/move/renamed to/from the project.

## Viewport

### Toolbar

1. Pressing **New** will clear the selection, the Viewport canvas, the Explorer, and all undo/redo stack operations for the previous document. A prompt will be displayed if there are unsaved changes.
1. Pressing **Save** will ask for new file names for USS and UXML if it is the first save, otherwise, it will overwrite the previously saved/loaded files.
1. Saving should work even if the openend assets have been moved or renamed (in which case, the Builder should update the USS Style paths inside the UXML document).
1. Pressing **Save As** will always ask for a new file name and save as a copy of the current document.
1. Dragging a .uxml asset onto the Object Field will load that file.
1. Can preview Light/Dark theme inside the canvas via the **Theme** popup field, independent from the current Editor Theme. **Default Theme** will use the current Editor Theme, while the other options will force a theme to be used in the canvas.
1. Pressing **Game** will change the background of the canvas to a render texture of the main camera in the Scene. This will render the camera view in real time.
1. Pressing **Preview** will toggle *Preview Mode* where you can no longer select elements by clicking on them in the Viewport, but instead, Viewport elements recieve regular mouse and focus events.

### Save Dialog

1. Entering paths that do not start with "Assets/" or "Packages/" will show an invalid path message and the Save button will be disabled.
1. Updating the UXML Path field will automatically update the USS Path field to match, until the USS Path field is changed manually.
1. Entering file names without extensions will still add the correct extensions when creating the assets.

### Canvas

1. Can click to select element.
1. Selecting an element inside a template instance or C# type selects the parent instance or C# element.
1. Relative position elements have bottom, right, and bottom-right handles that change inline `height` and `width` styles.
1. Absolute position elements have all 4 side and 4 corner handles visible.
1. Absolute position elements have 4 anchor handles visible to set or unset the `left`/`right`/`top`/`bottom` inline styles.
1. Absolute position elements can be click-drag moved, changing `top`/`right`/`left`/`bottom` inline styles depending on anchor state.
1. Resize and position handles change different styles depeneding on anchor state (ie. if `left` and `right` styles are set, changing the width changes the `right` style - otherwise, changing the width changes the `width` style).
1. Canvas size will be restored after Domain Reload or Window reload. It will be reset when opening a new document.

## Previews

### UXML

1. Updates text on any hierarchy, attribute, or inline style changes.
1. Shows unsaved StyleSheet as path="*unsaved in-memory StyleSheet with...".
1. Upon saving, all unsaved StyleSheet paths are fixed.
1. Shows `<Style>` tags for all root elements.

### USS

1. Updates on all StyleSheet/Selector changes.
1. Dimension (Length) styles have the unit added to the USS (`px`). (No support for `%` yet.)

## Inspector

### StyleSheet Section

1. Only visible if the selection is a StyleSheet (by selecting the **Shared Styles** section header in the Explorer).
1. Can create new Selectors by entering text in the **Selector** field and pressing Enter (or the Create button).
1. Shows quick help on selectors.

### Style Selector Section

1. Only visible if the selection is a selector in the current StyleSheet.
1. Can change the selector text by changing the **Selector** field and pressing Enter.

### Attributes Section

1. Only visible if the selection is an element in the current document.
1. Shows all valid attributes for the selected element, given its C# type.
1. Attribtues already set to a non-default value are highlighted with the same styling as Prefab overrides.
1. Changing attributes updates the Explorer, the Viewport, and the UXML Preview and changes are immediate.
1. Right-click **Unset** on an attribute will remove it from the UXML tag, reset the value to the element-defined default, and reset the override styling.

### Shared Styles Section

1. Only visible if the selection is an element in the current document.
1. Can add new class to element by typing in the **Add Style Class:** field and pressing the **Add Style Class:** (or pressing Enter).
1. If the style class being added to an element is not valid, an error message will appear.
1. All style classes on the current element are displayed as pills.
1. Style class pills have an **X** button that lets you remove them from the element.
1. Under **Matching Selectors**, all matching selectors on the current element are displayed with read-only fields for their properties.

### Local Style Overrides

1. Only visible if the selection is an element in the current document, or a selector in the current StyleSheet.
1. Changing any value will set it in the StyleSheet or inline UXML style attribute and highlight it with the same styling as Prefab overrides.
1. All style value types are supported.
1. Sub-section foldout expanded states are preserved between selection changes and domain reload.
1. Right-click **Unset** on an style field will remove it from the UXML inline style or StyleSheet, reset the value to default, and reset the override styling.