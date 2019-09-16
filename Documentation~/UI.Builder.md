# UI Builder

UI Builder lets you visually create and edit UI using UIElements, UXML, and USS.

# Overview

![UI Builder Main Window](UIBuilderAnnotatedMainWindow.png)

## Explorer `#1` `#2`

Similar to the Hierarchy window in Unity, this gives you access to all components of the currently open documents.

### StyleSheet `#1`

This section lists all USS selectors in the main USS document referenced by the UXML document. Currently, UI Builder only supports a single USS document per UXML.

Here, you can create new selectors by clicking on the **StyleSheet** header and typing in the full name of the selector in the **Inspector**, or via the **Add new selector...** text field at the bottom of the list where you can more easily create simple class-based selectors.

Class names within selector names will be converted to pills that can be dragged onto elements in the **Hierarchy** below or onto elements in the **Canvas**.

Selecting a selector in this list will let you change its properties in the **Inspector**. You will also see highlighted in the **Canvas** all elements that are currently affected by this selector.

### Hierarchy `#2`

This section is a tree representing the current live hierarchy of your UXML document. You can select elements here to edit them via the **Inspector** and you can click-drag them to re-parent them.

Elements will appear in the tree using their `name` attribute if they have one set. If not they will appear using their C# type. You can double-click on an element to quickly rename it.

If you click on the `︙` icon in the top right corner of the **Explorer**'s toolbar, you'll be able to see the element's type C# at all times (even if they have a name), and you can also see the element's current style class list.

## Library `#3` `#4`

This pane is similar to the **Project** window in Unity. It is a catered list of available UI elements, both standard ones provided by Unity and your own custom ones.

You can click-drag them into the **Explorer** pane or into the **Canvas** to instantiate them, or you can double-click them. If you hover over an item in this list, you should see a preview of it on the right side of the **Library** pane.

### Unity Elements `#3`

These are built-in common controls provided by Unity. They come with standard styling that will work for all supported Unity themes in the Editor and Runtime.

### Project Elements `#4`

This section is a tree of custom user elements. This includes other `.uxml` assets in the current project or custom C# elements (that inherit from `VisualElement` and have their `UxmlFactory` set up to be instantiable via UXML).

## Viewport `#5` `#6` `#7`

This is where you can see the current UXML document, running within a UI Builder-provided edit-time **Canvas** that you can resize to see how the UI would adapt to different host sizes in the wild.

In the tile bar of the **Viewport** pane you will see the name of the currently loaded document, as well as a `*` if there are unsaved changes.

### Toolbar `#5`

The **Toolbar** contains the **File** menu where you can create a **New** document, **Save** or **Save As** the current document, or **Open** another document.

It contains the **Current Theme** selection for quickly previewing your UI in dark or light themes without changing the entire Editor theme.

And it has a **Preview** button that lets you remove the selection layer on top of the **Canvas** that allows for click-selecting elements and allows you to interact with the UI you're building (ie. click on buttons, type in text fields, change expand/collapse foldout states).

### Canvas `#6` `#7`

Inside the **Viewport** you have the independently-sized **Canvas**. You can resize it via handles on the sides. This size, and everything else about the **Canvas** is purely to aid authoring and nothing is saved in the document itself.

Within the **Canvas**, you can click on elements to select them. Once selected, you can use the handles to change their size. For elements that have their **Inspector > Position > Position** set to **Relative**, only their height and width will be adjustable via the **Canvas** handles. To have full resizing and movement freedom you have to set the **Position** style to **Absolute**, but this is not the recommended way of using UIElements as it by-passes the automatic layouting functionality.

You can set the background color or image of the edit-time **Canvas** via the **Inspector** by first selecting the **Hierarchy** element in the **Explorer**. You can also have a live feed from a **Camera** as the background of your **Canvas** to get more context while building your UI without having to launch the full game.

## Code Previews `#8` `#9`

As you build your UI, all changes will either be saved to the UXML document or the USS document. You can see the generated UXML and USS text at any time in the **UXML Preview** and the **USS Preview** panes.

## Inspector `#10` `#11` `#12`

Equivalent to Unity's own **Inspector** window, this is where you can change properties and styles of the currently selected element or USS selector.

### Attributes `#10`

This is where you can change UXML attributes of a visual element like **Name**, **Tooltip**, or in the case of a text element, **Text**. These attributes are per-element can not be shared.

This section is only visible if you have a visual element selected.

### Inherited Styles `#11`

Styles can easily be shared between multiple elements so that you can make changes in one place and affect all uses. This is done via USS selectors which can change styles matching elements. Selectors can match on an element by either its name, style class, or C# type, or a combination of those, and/or a parent/child relationship.

But the simplest way to share styles is to assign one or more elements a style class (like `my-red-button`) and create a simple selector in the form `.my-red-button`.

To assign the selected element a style class, just type in the name in the **Style Class List** text field and press **Add Style Class to List**. If you already have a selector that matches, you should see the effects of the now-inherited styles immediately.

Alternatively, if you don't yet have a matching selector (and you've already applied some local styles via **Local Styles**), you can type in the name of your desired new selector (and style class) in the **Style Class List** text field and press **Extract Local Styles to New Class**. This will create a new USS selector (should see it in the **Explorer > StyleSheet**) and also add the style class to the current element.

At the bottom of the **Style Class List** foldout you should see (if any) all style classes currently on the selected element as pills. You can click the `x` to remove a style (ex. remove the default styling on a Unity-provided button). You can also double-click a pill to either create a simple selector for it if it doesn't exists or select the existing simple USS selector if it does exist.

At any time you can see all matching selectors and inherited styles inside the **Matching Selectors** foldout.

This section is only visible if you have a visual element selected.

### Local Styles `#12`

This section applies to both elements and USS selectors. For elements, any changes in the **Local Styles** will be overrides applied after all inherited styles and stored in the UXML document itself as inline styles. For USS selectors, any changes in the **Local Styles** will be added as properties in the USS rule in the StyleSheet. In all cases, you can see exactly which styles are set by looking for the white/black left borders and bolded text, similar to Prefab overridden fields.

# Full Feature List

## Global

1. Can delete element via Delete key.
1. Can cut/copy/duplicate/paste element via hot key, with the copied element (and all its children) being pasted as a child to the the parent of the currently selected element (or root if nothing is selected).
1. Can cut/copy/duplicate/paste Template instances, adding the correct Template registrations to the file.
1. Can copy/paste elements to and from a text file as valid UXML.
1. Can undo/redo style changes, hierarchy changes, and selection changes.
1. Open documents, current selection, and unsaved changes will survive domain reload.
1. Double-clicking a .uxml asset in the Project will open it using the UI Builder.
1. Previously open document will be re-opened after a Unity Editor restart.

## Explorer

### StyleSheet

1. Can select the main StyleSheet by the "StyleSheet" Explorer item, showing its dedicated Inspector.
1. Selectors with .classNames get pills for each class created in the Explorer.
1. In the Explorer, you can select selectors by clicking on the row or a class pill.
1. Can drag a style class pill from the Explorer onto an element in the Viewport to add the class.
1. Can drag a style class pill from the Explorer onto an element in the Hierarchy to add the class.
1. Dragging a style class onto an element inside a template instance or C# type in the Viewport adds it to the parent instance or C# element.
1. Dragging a style class onto an element inside a template instance or C# type in the Hierarchy does nothing.
1. Below all selectors there's a field that lets you create new selectors (by pressing enter).
    1. If **Class** mode is selected, a new `.class` selector will be added and you have options for the pseudo states to add.
    1. If **Complex** mode selected, the raw string will be used for the full selector name, and the pseudo stats MaskField should not be visible.
1. When hovering or selecting a style selector in the Explorer, all elements in the Canvas that match this selector will highlight.
1. With a selector selected, you can use the standard short-cuts and Edit menu to copy/paste/duplicate/delete it. You can also copy/paste the USS for the selector to/from a text file.
1. Right-clicking anywhere in the Hierarchy will open the Copy/Paste/Duplicate/Delete context menu.

### Hierarchy

1. Can click to select element.
1. Can drag element onto other elements in the Hierarchy to re-parent.
1. Can drag element between other elements to reorder, with live preview in the Canvas.
1. Can drag element onto other elements in the Viewport to re-parent.
1. Elements are displayed using their #name in blue, or C# type in white if they have no name.
1. Elements are displayed grayed out if they are children of a template instance or C# type.
1. Selecting an element inside a template instance or C# type will display the Inspector in read-only (disabled) mode.
1. Dragging element onto a template instance or C# type element in the Viewport re-parents it to the parent instance or C# element.
1. Dragging element onto a template instance or C# type element in the Hierarchy does nothing.
1. With an element selected, you can use the standard short-cuts and Edit menu to copy/paste/duplicate/delete it. You can also copy/paste the UXML for the element to/from a text file.
1. Right-clicking anywhere in the Hierarchy will open the Copy/Paste/Duplicate/Delete/Rename context menu.
1. Can double-click on an item to rename it.

## Library

1. Displays built-in elements under a **Unity** heading.
1. Displays project defined factory elements and UXML files (with `.uxml` extension) under a **Project** heading. This includes assets inside both the `Assets/` and `Packages/` folders.
1. Can double click to create new element instance in the root.
1. Items that have corresponding `.uxml` assets will have an "Open" button visible that will open the asset for editing in the UI Builder. The currently open .uxml asset in the Library will be grayed out and will not be instantiable to prevent infinite recursion.
1. Can click-drag onto a Viewport element to create new instance as a child.
1. Can click-drag onto a Hierarchy element to create new instance as a child, or between elements to create as a sibling.
1. Can create (double-click or drag) template instances from other uxml files.
1. When creating a new empty VisualElement, it will have an artificial minimum size and border which will be reset as soon as you parent a child element under it or change its styling.
1. Hovering over items in the Library will show a preview of that element in a floating preview box. The preview will use the current Theme selected for the canvas.
1. Library pane will update if new uxml files are added/deleted/move/renamed to/from the project.

## Viewport

### Header

1. The currently open UXML asset name, or `<unsaved asset>`, will be displayed in the Viewport header, grayed out.
1. If there are unsaved changes, a `*` will be appended to the asset name.
1. The current UI Builder package version is displayed in` the **Viewport** title bar.

### Toolbar

1. Pressing **File > New** will clear the selection, the Viewport canvas, the Explorer, and all undo/redo stack operations for the previous document. A prompt will be displayed if there are unsaved changes.
1. Pressing **File > Save** will ask for new file names for USS and UXML if it is the first save, otherwise, it will overwrite the previously saved/loaded files.
1. Saving should work even if the opened assets have been moved or renamed (in which case, the Builder should update the USS Style paths inside the UXML document).
1. Pressing **File > Save As...** will always ask for a new file name and save as a copy of the current document.
1. Pressing **File > Open...** will display an Open File Dialog and let you select a `.uxml` asset inside your Project.
1. Dragging a `.uxml` asset onto the Object Field will load that file.
1. Can preview Light/Dark theme inside the canvas via the **Theme** popup field, independent from the current Editor Theme. **Default Theme** will use the current Editor Theme, while the other options will force a theme to be used in the canvas.
1. Pressing **Preview** will toggle *Preview Mode* where you can no longer select elements by clicking on them in the Viewport, but instead, Viewport elements receive regular mouse and focus events.

### Save Dialog

1. Entering paths that do not start with "Assets/" or "Packages/" will show an invalid path message and the Save button will be disabled.
1. Updating the UXML Path field will automatically update the USS Path field to match, until the USS Path field is changed manually.
1. Entering file names without extensions will still add the correct extensions when creating the assets.
1. The "..." button beside each path field should bring up the system Save File Dialog.
1. Folders in an otherwise valid path will be created if missing.

### Canvas

1. Can be resized via handles on the right, left, and bottom.
1. Canvas has a minimum size.
1. Can click to select element.
1. Selecting an element inside a template instance or C# type selects the parent instance or C# element.
1. Relative position elements have bottom, right, and bottom-right handles that change inline `height` and `width` styles.
1. Absolute position elements have all 4 side and 4 corner handles visible.
1. Absolute position elements have 4 anchor handles visible to set or unset the `left`/`right`/`top`/`bottom` inline styles.
1. Absolute position elements can be click-drag moved, changing `top`/`right`/`left`/`bottom` inline styles depending on anchor state.
1. Resize and position handles change different styles depeneding on anchor state (ie. if `left` and `right` styles are set, changing the width changes the `right` style - otherwise, changing the width changes the `width` style).
1. Canvas size will be restored after Domain Reload or Window reload. It will be reset when opening a new document.
1. When changing Width or Height in the Inspector, the corresponding resize handles in the canvas will highlight.
1. When hovering over elements in the Canvas, the corresponding entry in the Hierarchy will highlight.
1. When hovering over elements in the Canvas, all Explorer entries of style selectors that match this element will highlight.
1. If the Canvas is bigger than the Viewport, a **Fit Canvas** button will appear that will resize the Canvas to fit in the Viewport.
1. Canvas size will be remembered for each asset and restored when loading the asset. It also means it will survive Editor restarts.
1. Clicking the **Hierarchy** item in the Explorer will display the Canvas options in the Inspector:
    1. Can see and change the Canvas height and width.
    1. Can set the custom Canvas background color/image Opacity.
    1. Can set the Canvas background to be a solid color via the Color Background mode.
    1. Can set the Canvas background to be an image, can set the ScaleMode of the image, and can have the Canvas resize to match the image via the **Fit Canvas to Image** button.
    1. Can set the Canvas background to be a render texture for a chosen Camera.
    1. All of these settings will be remembered next time you open the same UXML document.

## Previews

### UXML

1. Updates text on any hierarchy, attribute, or inline style changes.
1. Shows unsaved StyleSheet as path="*unsaved in-memory StyleSheet with...".
1. Upon saving, all unsaved StyleSheet paths are fixed.
1. Shows `<Style>` tags for all root elements.
1. The `UnityEngine.UIElements` namespace is aliased to `ui:` and `UnityEditor.UIElements` namespace is aliased to `uie:`.
1. Custom C# elements not in the `UnityEngine.UIElements` and `UnityEditor.UIElements` namespaces have no namespace alias and appear as their full type.
1. (2019.3+) A relative path to a `.uss` asset will be used in the `src` attribute of the `<Style>` tag if the asset is in the same folder (directly or in a sub-folder) as the main asset. Otherwise, an absolute path will be used.
1. (2019.3+) A relative path to a `.uxml` asset will be used in the `src` attribute of the `<Template>` tag if the asset is in the same folder (directly or in a sub-folder) as the main asset. Otherwise, an absolute path will be used.

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
1. Attributes already set to a non-default value are highlighted with the same styling as Prefab overrides.
1. Changing attributes updates the Explorer, the Viewport, and the UXML Preview and changes are immediate.
1. Right-click **Unset** on an attribute will remove it from the UXML tag, reset the value to the element-defined default, and reset the override styling.

### Inherited Styles Section

1. Only visible if the selection is an element in the current document.
1. Can add existing style class to element by typing the name of the class in the field inside **Inherited Styles** and pressing the **Add Style Class to List** button (or pressing Enter).
1. Can extract all overwritten **Local Styles** to a new style class by typing the name of the class in the field inside **Inherited Styles** and pressing the **Extract Local Styles to New Class** button.
1. If the style class being added to an element is not valid, an error message will appear.
1. All style classes on the current element are displayed as pills.
1. Style class pills have an **X** button that lets you remove them from the element.
1. Under **Matching Selectors**, all matching selectors on the current element are displayed with read-only fields for their properties.
1. Inspector style class pills in the **Inherited Styles** section will show faded if there is no single-class selector in the main StyleSheet.
1. Double-clicking on a style class pill in the **Inherited Styles** section it will select the corresponding single-class selector in the main StyleSheet, if one exists, otherwise it will create it.

### Local Styles

1. Only visible if the selection is an element in the current document, or a selector in the current StyleSheet.
1. Changing any value will set it in the StyleSheet or inline UXML style attribute and highlight it with a solid bar on the left side and bold font.
1. Style category headers will have an override bar and bold font if any child styles are overridden.
1. All style value types are supported.
1. Sub-section foldout expanded states are preserved between selection changes and domain reload.
1. Right-click **Unset** on an style field will remove it from the UXML inline style or StyleSheet, reset the value to default, and reset the override styling.
1. Align section toggle button strips will change icons depending on the value of the flex-direction style.