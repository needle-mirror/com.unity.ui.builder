# Testing outside the UI Builder

## Testing runtime UI (2020.2+)

Recall that for UI Toolkit runtime support you will need to install the `com.unity.ui` package. Once you have this package installed, you can add runtime UI to a Unity Scene using the **UIDocument** component. **UIDocument** takes a UI Document (UXML) asset as one of its parameters. If you are editing the UI Document (UXML) assigned a **UIDocument** component in UI Builder, you should see changes automatically in the Game Window. If you don't see automatic updates, check the **&#8942;** menu of the Game Window and make sure **UI Toolkit Live Reload** is enabled.

Note that **UI Toolkit Live Reload** will _re-run_ the `OnEnable()` functions of all other `MonoBehaviour`s on any GameObject that has a **UIDocument** component using your UI Document (UXML). It is assumed that any "companion" component that binds to and controls a **UIDocument** component will properly re-bind during these live-reloads invoked by UI Builder changes.

## Testing Editor Extension UI

In stock Unity, the only way to see updates to UI Document (UXML) in an Editor Window (including the Inspector) as you build it in UI Builder is to save it to disk and re-open the Editor Window.

An slightly easier option is to enable Unity's `internal` mode, by opening the About Window and typeing (blindly) the word `internal`. This will add a new option to each Editor Window's top-right **&#8942;** menu called **Reload Window** which is a faster way to force an Editor Window reload.

Unlike UI Document (UXML) changes, StyleSheet changes will be reflected anywhere they are used by simply saving them to disk. You should not need to reload Editor Windows to see changes to USS.

### Unity 2020.2+

For 2020.2, if you install the `com.unity.ui` package, you should see a new option in any Editor Window's top-right **&#8942;** menu called **UI Toolkit Live Reload**. Unlike with runtime explained above, for Editor Windows, Live Reload is disabled by default. You can enable it to see live updates as you work in the UI Builder.
