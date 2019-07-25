# Changelog
All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [0.5.7] - 2019-07-24

- Added Foldout to the Library.
- Major refactor of domain reload, window reload (F5), and window maximizing survival logic. There should be no more flickering when any serialization events happen and the builder state will now always survive, even when the Layout is changed or the Builder window is closed and reopened.
- Added check for unsaved changes when quitting the Unity Editor.
- Added first sample UI in Samples.
- Added back Game preview button that replaces canvas background with main camera view.
- Added back preview on hover in the Library for built-in controls.
- Library preview on hover will now display elements using the currently selected Theme for the canvas.
- Fixed inability to save UXML/USS docs to the "Packages/" folder by remove auto prepending of "Assets/" to the path and adding path validation checks in the Save Dialog.
- Fixed dashes wrongly being disallowed in style class names in the Inspector.
- Fixed Editor theme overrides not properly overriding the text color (ie. Light theme used in Dark themed Editor would still use white text color).
- Fixed the Font style field not properly filtering for the Font asset type or allowing you to set it.
- Fixed children not being hidden/unhidden when change the parent's Overflow style property.
- Fixed Size section of Inspector to properly show the bold override field style in all cases.
- Fixed Viewport toolbar theme selection dropdown to be properly styled for the toolbar.
- Fixed url() in USS not getting the starting "/" for absolute paths when using paths inside the Packages folder.
- Fixed USS Preview not updating when Builder window was Maximized.
- (2019.3) Added support for border-(left/right/top/bottom)-color styles.
- (2019.3) Fixed for and bumped trunk version to **2019.3.0a10**.

## [0.5.6] - 2019-07-04

- (2019.3) Fixed for and bumped trunk version to **2019.3.0a9**.

## [0.5.5] - 2019-06-25

- Fixed url() paths being added to UXML style attributes with unescaped double quotes.
- Fixed url() in USS not getting the starting "/" for absolute paths.

## [0.5.4] - 2019-06-20

- (2019.3) Fixed copy/paste of elements with Length-type inline styles improperly importing and causing errors.

## [0.5.3] - 2019-06-20

- Added support for saving with Ctrl+S (or Command+S).
- Changed left-hand blue bar on fields for value overrides to white/black.
- Changed "Empty Element" to "VisualElement" in Library.
- Fixed "Assets/" being added to new file save path when the Assets root folder was currently selected in the Project Browser.
- Fixed saving of an empty document creating an invalid UXML asset.
- Fixed Save As dialog matching uss file name to uxml file name even if the loaded uss file name was different from the uxml name.
- Fixed dummy child element of an empty VisualElement appearing in the Hierarchy after initial creation.
- Fixed elements having opacity set to 0.5 right after initial creation via drag from the Library.
- Fixed validation errors in saved USS asset due to border-width styles not having units added.
- (2019.3) Fixed duplicate style and class attributes in generated UXML.
- (2019.3) Fixed glitchy overlapping text on top of Explorer header.

## [0.5.2] - 2019-06-19

- Removed use of the element preview window on Library mouse hover for the built-in Unity controls.
- The .uxml and .uss extensions will now be automatically applied if missing from the user supplied paths when saving.
- In the save dialog, changing the UXML path will now also update the USS path to match - until you manually change the USS path.
- The Builder will now properly save to the correct assets, even if they have been moved or renamed (in which case it will also update the USS Style paths inside the UXML document).
- Canvas size will now be restored after Domain Reload or Window reload. It will be reset when opening a new document.
- Fixed extra '>' appearing in the Explorer between selector parts.
- Fixed null ref exception when deleting an element while the mouse was over the element in the viewport.
- Fixed being able to save uxml/uss files outside the Assets folder.
- Fixed code previews overlapping the Inspector when the Viewport was too small.
- (2019.3) Fixed toolbar buttons shrinking when toolbar width got too small.

## [0.5.1] - 2019-06-18

- Improved UX for size style fields in the Inspector.
- Fixed Save dialog displaying .uxml file extensions as initial value for .uss path.
- (2019.3) Fixed saving and loading of embedded USS paths in the UXML document.
- (2019.3) Fixed Save As dialog missing Cancel button printing rendering errors in console.
- (2019.3) Fixed glitchy overlapping text artifacts in the Inspector pane.

## [0.5.0] - 2019-06-14

- Added support for reordering of elements in the Hierarchy, with live preview in the canvas.
- Added support for dragging from the Library to the Hierarchy in between elements to place elements at a specific index, with preview.
- New empty VisualElements will now have an artificial minimum size that will be reset if a child is added or its styling is changed in any way.
- Added Light/Dark Theme preview mode to the Toolbar, independent from the current Editor Theme.
- Hovering over items in the Library will show a preview of that element in a floating preview box.
- Added style class name validation when adding it to an element in the Inspector.
- Can now press enter to add a style class to an element in the Inspector.
- Library pane will now update if new uxml files are added/deleted/move/renamed to/from the project.
- Removed PopupField from the Library as it has no UXML support right now.
- Fixed Save/SaveAs dialog to look correctly when using the Editor Light skin.
- Fixed error and stuck preview when dragging over an EnumField from the Library to the Viewport.
- Added support for **2019.3.0a6**.
- Added installation instructions to README.

## [0.4.2] - 2019-06-08

- Cut/copy/duplicate/paste of elements now only work while the Viewport or the Explorer are focused.
- Save As dialog now uses the name of the current document as the initial new file name.
- Fixed Backspace attempting to delete the selected element while a field in the Inspector was focused.
- Fixed regression with the anchors resizing the element slightly when activated.
- Fixed delete, backspace, and pasting working in the code previews.

## [0.4.1] - 2019-06-06

- Added blue highlight at the top of the currently focused pane.
- Selection overlay will hide when going into Preview mode and re-appear if selecting something else or going out of Preview mode.
- Added support for copy/pasting elements to and from a text file as valid UXML.
- Expanded cut/copy/duplicate/paste to work recursively on all children of the selected element.
- Added support for cut/copy/duplicate/paste of Template instances.
- Moved Local Style Overrides and Attributes fields tooltips to be centered on just their Labels.
- Added border highlight hover effect when moving mouse over canvas elements.
- Fixed Cut deleting the element before the Paste.
- Fixed pasted elements not being added at the end of the list of siblings.
- Fixed duplicate elements being sometimes created after a paste.
- Fixed resize handles being offset if element selected during Preview mode.
- Fixed expanded states in the Explorer not properly persisting or breaking when reparenting.
- Fixed Delete (Backspace) key not working on Mac unless the Fn or Shift key is also pressed.
- Fixed Preview orange border disappearing if the canvas is bigger than the viewport.

## [0.4.0] - 2019-06-04

- Added cut/copy/duplicate/paste support for VisualElements.
- Inspector local style overrides sections has been rewritten, with each section getting a dedicated uxml layout instead of being generated from reflection. A new custom binding system is used to bind fields to style values, meaning multiple fields can bind to the same style value.
- Inspector local style override fields now have nice looking names, instead of USS property names.
- Tooltips added to each style field displaying the USS style property name.
- Added **Save As** button/option to the toolbar.
- Added blue bars next to overridden styles and removed blue text to match prefab overrides UI.
- Added support for editing element type-specific attributes, like name, text, and tooltip. This change also fixes the previously limited support for changing the text attribute. Includes support for undo/redo and reverting to default.
- Can now drag style class pill onto elements in hierarchy.
- Can now drag items from the Library to the hierarchy.
- Added initial documentation draft, listing all features.
- Renamed Selector text field to "Selector" from "Name" in the Selector and StyleSheet Inspectors for clarity.
- Fixed inspector flickering when using undo/redo or sometimes when using the canvas handles.
- Fixed the root stylesheet path being added to all elements in UXML, instead of just the root elements.
- Fixed being able to reparent elements inside template instances or C# elements.
- Fixed look when using Unity Light theme.
- Fixed occasional drag-and-drop instabilities.
- Fixed being able to parent an element under one of its children.
- (2019.3) Fixed mis-handling of Dimension styles value types as Floats.
- (2019.3) Fixed compilation issues and bugs specific to **2019.3.0a4**. Any newer trunk versions will not work. Attributes, apart for **Name** will do not work at all in this version.

## [0.3.1] - 2019-05-28

- You can now create new USS Selectors by pressing Enter (instead of using the button).
- Clicking (and not dragging) on a style class pill in the Shared Styles explorer will now select the selector.
- Added VERY brief help section in the Shared Styles Inspector to help with selector creation.
- Added .uxml to Library entries representing UXML assets in the project.
- Fixed template instance uxml file name appearing before element name in Hierarchy.
- Fixed inspector style fields not updating after adding/removing a style class on an element.
- Fixed enum styles not displaying accurate state after reselecting the shared style/element.
- Fixed shared styles not re-applying after changing the selector name.
- (2019.3) Fixed unable to parse file error when builder is opened with a new file.
- (2019.3) Fixed elements getting a content-container attribute set in UXML, leading to corrupt UXML generation.
- (2019.3) Fixed unhandled type Dimension error when resizing the canvas.
- (2019.3) Fixed various styling issues in the N* theme.

## [0.3.0] - 2019-05-27

- Switched from using the live canvas VisualElement hiearchy as the source of truth to using the actual VisualTreeAsset (UXML asset). Nothing major should be noticed as a result of this refactor. It just makes certain future improvements possible.
- No longer generating element names and adding name-based selectors in the global StyleSheet for Local Style Overrides. Instead, Local Style Overrides will be written as inline-styles on the element in UXML. The global StyleSheet will only be used for shared styles.
- The main StyleSheet path will now be added to all root elements in the UXML document. This allows the Builder to now work with any externally created UXML file by removing the reliance on a special root element.
- Finished and fixed support for creating Template Instances (using UXML documents inside other UXML documents).
- Elements inside a Template Instance will appear grayed out in the Hierarchy.
- Selecting elements inside a Template Instance will now select the TemplateContainer.
- TemplateContainers now have their .uxml file names listed in the Hierarchy.
- Added Undo/Redo support.
- Added right-click menu on style override fields to Unset an overridden style.
- Added support for opening a UXML document via double-click in the Project Browser.
- Added "px" units on Length styles in generated USS.
- Added support for Background, Font, and Cursor style property types in the Inspector.
- Removed top-level menus for SplitView and Builder. The UI Builder window is now under: **Window > UI > UI Builder**.
- Removed the "New (Test)" toolbar button in non-development mode.
- Hierarchy no longer shows elements with their first style class. Name if it is set; type otherwise.
- Fixed name/tab of Builder Window becoming corrupt.
- Fixed flickering while dragging elements from the Library.
- Fixed being able to drag-and-drop a Style class onto the main document element.
- Fixed -unity* custom style properties being written to USS without the starting "-".

## [0.2.1] - 2019-05-10

- Added confirmation dialog when discarding unsaved changes.
- Added tracking of unsaved changes via * in EditorWindow title.
- Added ability to change an existing style selector in its Inspector.
- Fixed shared styles selectors only showing first word in Explorer.
- Fixed style selector selections in the Explorer not surviving domain reload.
- Fixed Shared Styles sections sometimes showing a duplicated list of selectors.

## [0.2.0] - 2019-05-10

- Early iteration of shared styles.
- Draggable style classes onto elements.
- Style class list managemenent in the Inspector.
- Re-parenting of elements via drag-and-drop in the Hierarchy.
- Library now containes user defined templates from UXML and source factories.

## [0.1.0] - 2019-04-15

### This is the first release of *Unity Package UI Builder*.

- Basic element creation, manipulation, and editing.
- Loading and saving of UXML documents.
- Preview mode.
- Drag and drop of elements from Library.
- Resize, move, and anchoring manipulators.
- Basic hierarchy view with selection preview.
- Basic styles Inspector.
