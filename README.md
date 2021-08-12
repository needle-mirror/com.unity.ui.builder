# UI Builder

The **UI Builder** lets you visually create and edit UI assets such as UI documents (`.uxml` files), and StyleSheets (`.uss` files), that you use with Unity's **UI Toolkit** (formerly UIElements). You can open the UI Builder window from the menu (**Window > UI Toolkit > UI Builder**), or from the Project window (double-click a `.uxml` asset).

> Note: In Unity 2019.x, you open the UI Builder from the **Window > UI > UI Builder** menu.

> Note: UI Builder is the visual authoring tool for UI Toolkit. It does not include runtime support. To enable runtime support in Unity 2020.1 and later, install the UI Toolkit package. For details, see this post on the Unity Forum: https://forum.unity.com/threads/ui-toolkit-1-0-preview-available.927822/.

**Internal Developers:** Please **join our [#devs-uibuilder](https://unity.slack.com/archives/CJ3TX00QJ) Slack channel** for feedback and questions.

## Installation

Unity versions supported:
- **2019.4**: 2019.4.29f1 or newer
- **2020.3**: 2020.3.15f2 or newer
- **2021.1**: 2021.1.16f1 or newer
- **2021.2**: 2021.2.0b7 or newer
- **2022.1**: 2022.1.0a2 or newer

To install:
* **2021+**: Nothing to do. UI Builder comes built-in in Unity 2021.1+.
* **2019-2020**:
    1. Open the **Window > Package Manager**.
        * **For 2020.x**, go to **Edit > Project Settings... > Package Manager**, and enable check **Enable Preview Packages**:
        ![Enable Preview Packages (new)](Documentation~/images/InstallationPackageManagerEnablePreview.png)
        * **For 2019.x**, enable **Advanced > Show preview Packages**: ![Enable Preview Packages](Documentation~/images/InstallationPackageManagerAdvancedOptions.png)
    1. Go back to the **Window > Package Manager**.
    1. Search for `UI Builder`:![Search Package Manager](Documentation~/images/InstallationPackageManagerSearch.png)
    1. Press **Install**.

## Documentation

![UI Builder Main Window](Documentation~/images/UIBuilderAnnotatedMainWindow.png)

### ![1](Documentation~/images/Numeral_1_half.png) StyleSheets
* Create USS Selectors for sharing common styling between multiple elements.
### ![2](Documentation~/images/Numeral_2_half.png) Hierarchy
* Current UI Document (UXML) element hierarchy.
### ![3](Documentation~/images/Numeral_3_half.png) Library
* **Standard tab:** Built-in Unity elements.
* **Project tab:** Custom user elements like other `.uxml` templates in the current project.
### ![4](Documentation~/images/Numeral_4_half.png) Viewport
* **Toolbar:** Can Save/Load, change the Theme and activate Preview mode.
* Currently selected element with manipulation handles.
* Edit-time Canvas for editing and previewing current document with optional edit-time-only background image.
### ![5](Documentation~/images/Numeral_5_half.png) Code Previews
* **UXML Preview:** Preview of the generated UXML hierarchy asset.
* **USS Preview:** Preview of the generated USS styles asset.
### ![6](Documentation~/images/Numeral_6_half.png) Inspector
* **Attributes:** Change attributes, like element name, that are set in the UXML document.
* **Inherited Styles:** Add/remove style classes and see which selectors match the current element.
* **Local Styles:** Override styles on the current element, inlined in the UXML document.

For more info, see our [documentation page](Documentation~/index.md).
