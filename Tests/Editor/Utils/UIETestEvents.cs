using System;
using System.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UI.Builder.EditorTests
{
    internal static class UIETestEvents
    {
        // In order for tests to run without an EditorWindow but still be able to send
        // events, we sometimes need to force the event type. IMGUI::GetEventType() (native) will
        // return the event type as Ignore if the proper views haven't yet been
        // initialized. This (falsely) breaks tests that rely on the event type. So for tests, we
        // just ensure the event type is what we originally set it to when we sent it.
        // This original type can be retrieved via Event.rawType.
        static EventBase MakeEvent(Event evt)
        {
            return UIElementsUtility.CreateEvent(evt, evt.rawType);
        }

        public static EventBase MakeEvent(EventType type)
        {
            var evt = new Event { type = type };
            return MakeEvent(evt);
        }

        public static EventBase MakeEvent(EventType type, Vector2 position)
        {
            var evt = new Event { type = type, mousePosition = position };
            return MakeEvent(evt);
        }

        public static EventBase MakeCommandEvent(EventType type, string command)
        {
            var evt = new Event { type = type, commandName = command };
            return MakeEvent(evt);
        }

        public enum Command
        {
            Copy,
            Paste,
            Cut,
            Duplicate
        }

        public static void ExecuteCommand(EditorWindow window, Command command)
        {
            var evt = MakeCommandEvent(EventType.ExecuteCommand, command.ToString());
            window.rootVisualElement.SendEvent(evt);
        }

        internal static class Mouse
        {

            public static EventBase MakeEvent(Vector2 deltaMove, MouseButton button = MouseButton.LeftMouse, EventModifiers modifiers = EventModifiers.None, int clickCount = 1)
            {
                var evt = new Event { type = EventType.MouseMove, delta = deltaMove, button = (int)button, modifiers = modifiers, clickCount = clickCount};
                return UIETestEvents.MakeEvent(evt);
            }

            public static EventBase MakeEvent(EventType type, Vector2 position, MouseButton button = MouseButton.LeftMouse, EventModifiers modifiers = EventModifiers.None, int clickCount = 1)
            {
                var evt = new Event { type = type, mousePosition = position, button = (int)button, modifiers = modifiers, clickCount = clickCount};
                return UIETestEvents.MakeEvent(evt);
            }

            public static EventBase SimulateClick(Vector2 deltaMove, MouseButton button = MouseButton.LeftMouse, EventModifiers modifiers = EventModifiers.None, int clickCount = 1)
            {
                var evt = new Event { type = EventType.MouseMove, delta = deltaMove, button = (int)button, modifiers = modifiers, clickCount = clickCount};
                return UIETestEvents.MakeEvent(evt);
            }

            public static IEnumerator SimulateClick(VisualElement target, MouseButton button = MouseButton.LeftMouse, EventModifiers modifiers = EventModifiers.None)
            {
                yield return SimulateClick(target, button, modifiers, 1);
            }

            public static IEnumerator SimulateDoubleClick(VisualElement target, MouseButton button = MouseButton.LeftMouse, EventModifiers modifiers = EventModifiers.None)
            {
                yield return SimulateClick(target, button, modifiers, 2);
            }

            private static IEnumerator SimulateClick(VisualElement target, MouseButton button , EventModifiers modifiers, int clickCount)
            {
                var root = UIETestHelpers.GetRoot(target);
                var mouseDown = MakeEvent(EventType.MouseDown, target.worldBound.center, button, modifiers, clickCount);
                root.SendEvent(mouseDown);
                yield return UIETestHelpers.Pause(1);

                var mouseUp = MakeEvent(EventType.MouseUp, target.worldBound.center, button, modifiers, clickCount);
                root.SendEvent(mouseUp);
                yield return UIETestHelpers.Pause(1);
            }

            public static EventBase SimulateScroll(Vector2 delta, Vector2 position)
            {
                var evt = new Event
                {
                    type = EventType.ScrollWheel,
                    delta = delta,
                    mousePosition = position
                };

                return UIETestEvents.MakeEvent(evt);
            }

            public static void SimulateMouseEvent(EditorWindow window, EventType eventType, Vector2 position, MouseButton button = MouseButton.LeftMouse)
            {
                var mouseEvent = MakeEvent(eventType, position, button);
                window.rootVisualElement.SendEvent(mouseEvent);
            }

            public static IEnumerator SimulateDragAndDrop(EditorWindow window, Vector2 positionFrom, Vector2 positionTo, MouseButton button = MouseButton.LeftMouse)
            {
                var mouseDown = MakeEvent(EventType.MouseDown, positionFrom, button);
                window.rootVisualElement.SendEvent(mouseDown);

                yield return SimulateMoveMove(window, positionFrom, positionTo, button);

                var mouseUp = MakeEvent(EventType.MouseUp, positionTo, button);
                window.rootVisualElement.SendEvent(mouseUp);
                yield return UIETestHelpers.Pause(1);
            }

            public static IEnumerator SimulateMoveMove(EditorWindow window, Vector2 positionFrom, Vector2 positionTo, MouseButton button = MouseButton.LeftMouse)
            {
                const int dragSamples = 10;
                const int moveFramesDelay = 1;
                var dragDistance = Vector2.Distance(positionFrom, positionTo);
                var dragSpeed = Mathf.Max(1f, dragDistance / dragSamples) ;

                var normalizedDirection = (positionTo - positionFrom).normalized;
                var currentMousePosition = positionFrom;

                do
                {
                    currentMousePosition += dragSpeed * normalizedDirection;
                    var moveEvt = new Event
                    {
                        type = EventType.MouseMove,
                        mousePosition = currentMousePosition,
                        delta = normalizedDirection,
                        button = (int) button
                    };

                    window.rootVisualElement.SendEvent(UIETestEvents.MakeEvent(moveEvt));
                    yield return UIETestHelpers.Pause(moveFramesDelay);
                } while (Vector2.Distance(currentMousePosition, positionTo) > 1f);
            }
        }

        internal static class KeyBoard
        {
            public static EventBase MakeEvent(EventType type, KeyCode code, EventModifiers modifiers = EventModifiers.None)
            {
                var evt = new Event { type = type, keyCode = code, character =  '\0', modifiers = modifiers};
                return UIETestEvents.MakeEvent(evt);
            }

            public static EventBase MakeEvent(EventType type, char character, EventModifiers modifiers = EventModifiers.None)
            {
                var evt = new Event { type = type, keyCode =  KeyCode.None, character = character, modifiers = modifiers};
                return UIETestEvents.MakeEvent(evt);
            }

            public static IEnumerator SimulateTyping(EditorWindow window, string text) => SimulateTyping(window.rootVisualElement, text);

            public static IEnumerator SimulateTyping(VisualElement visualElement, string text)
            {
                foreach(var character in text)
                {
                    var evt = MakeEvent(EventType.KeyDown, character);
                    visualElement.SendEvent(evt);
                }

                yield return UIETestHelpers.Pause(1);
            }

            public static EventBase SimulateKeyDown(KeyCode code, EventModifiers modifiers = EventModifiers.None) => MakeEvent(EventType.KeyDown, code, modifiers);

            public static void SimulateKeyDown(EditorWindow window, KeyCode code, EventModifiers modifiers = EventModifiers.None)
                => SimulateKeyDown(window.rootVisualElement, code, modifiers);

            public static void SimulateKeyDown(VisualElement visualElement, KeyCode code,  EventModifiers modifiers = EventModifiers.None)
            {
                var evt = SimulateKeyDown(code, modifiers);
                UIETestHelpers.GetRoot(visualElement).SendEvent(evt);
            }
        }

#if UNITY_2019_3_OR_NEWER
        internal static class Pointer
        {
            public static EventBase MakeEvent(TouchPhase phase, Vector2 position, EventModifiers modifiers = EventModifiers.None, MouseButton button = MouseButton.LeftMouse)
            {
                var touch = MakeDefaultTouch();
                touch.position = position;
                touch.phase = phase;

                if (button != MouseButton.LeftMouse)
                    PointerDeviceState.PressButton(touch.fingerId, (int)button);

                switch (touch.phase)
                {
                    case TouchPhase.Began:
                        return PointerDownEvent.GetPooled(touch, modifiers);
                    case TouchPhase.Moved:
                        return PointerMoveEvent.GetPooled(touch, modifiers);
                    case TouchPhase.Ended:
                        return PointerUpEvent.GetPooled(touch, modifiers);
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            public static EventBase SimulateMove(Vector2 deltaPosition, EventModifiers modifiers = EventModifiers.None)
            {
                var touch = MakeDefaultTouch();
                touch.deltaPosition = deltaPosition;
                touch.phase = TouchPhase.Moved;

                return PointerMoveEvent.GetPooled(touch, modifiers);
            }

            private static Touch MakeDefaultTouch()
            {
                var touch = new Touch();
                touch.fingerId = 0;
                touch.rawPosition = touch.position;
                touch.deltaPosition = Vector2.zero;
                touch.deltaTime = 0;
                touch.tapCount = 1;
                touch.pressure = 0.5f;
                touch.maximumPossiblePressure = 1;
                touch.type = TouchType.Direct;
                touch.altitudeAngle = 0;
                touch.azimuthAngle = 0;
                touch.radius = 1;
                touch.radiusVariance = 0;

                return touch;
            }
        }
#endif
    }
}