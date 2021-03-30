# Workflow overview

The general approach to UI creation in the UI Builder is the following iteration cycle:
1. Open an existing or create a new UI Document (UXML).
1. Drag elements or other UI Documents (UXML) (created using this same iteration cycle) from the **Library** into the **Hierarchy** to create you UI hierarchy.
1. Select elements in the **Hierarchy** or **Canvas** to access their attributes and style properties in the **Inspector**.
1. Set per-element attributes like `Label` Text or `Button` Tooltip via the **Inspector**'s **Attributes** section.
1. Set per-element style properties, including layouting and position properties, via the **Inspector**'s **Inline Styles** section.
1. When more than one element starts to need the same style properties, add or create a **StyleSheet** to the UI Document (UXML) using the **StyleSheets** pane.
1. With an element selected, extract its inline style properties to a **StyleSheet** via the **Inspector**'s **StyleSheet > Style Class List** section by giving it a new style class name and clicking **Extract Inlined Styles to New Class**.
1. Create additional USS Selectors via the **Add new selector...** at the top of the **StyleSheets** pane that override a subset of style properties on a specific set of elements, like a `.my-button:hover { color: blue; }` selector that sets the color blue on any element that has the class `.my-button` and the mouse is on top of it.
1. Test your UI by clicking the **Preview** button in the **Viewport**'s toolbar to disable **Canvas** authoring and allow the UI to become interactable (ie. test the `:hover` styles).
1. Go back to step (2) and/or save the UI Document (UXML).