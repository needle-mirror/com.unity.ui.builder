# Inline styles vs StyleSheets

Style information for an element in UI Toolkit can come from three different places:
1. C# inline style properties set directly in C#. These properties override any same property coming from any other source.
1. UXML inline style properties on the element itself, stored directly inside UXML, using the special `style` attribute. These properties override any same property coming from StyleSheets.
1. StyleSheet style properties coming from a StyleSheet (`.uss`) asset and being applied to an element because the element matches a USS Selector (which is like a search query).

Similar to CSS on the web, StyleSheets allow sharing of styles across many UI elements and many UI panels and screens. They also allow quick refactoring and changing of styles across an entire application from a central location (ie. themes). As such, it is generally encouraged to keep as much styling in StyleSheets as possible, instead of inlining styles on each individual element.

In the UI Builder, you can start by creating elements and using inline styles only to experiment while the number of elements is still small. However, as you build more complex UI, it will become easier to manage styles using StyleSheets. You can use the **Extract Inlined Styles to New Class** in the **StyleSheet** section of the **Inspector** to quickly extract inline styles on an element to a StyleSheet.

Another reason to use StyleSheets more than inline styles is because there are some features that are not possible to use with inline styles, for example:
1. Pseudo states like `:hover`, which will apply some style properties to an element only when you hover over it with the mouse.
1. Styling of elements inside a read-only hierarchy inside a **Template Instance** (instance of another UI Document (UXML)) or custom C# element that creates an internal hierarchy. These elements can be styled with StyleSheets using hierarchical USS Selectors like:<br>
    ```
    .parentElement > .childElement
    ```