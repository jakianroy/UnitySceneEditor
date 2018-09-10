using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Reflection;
using System.Linq;
using UnityEditor;


namespace QDazzle
{

    using MenuFunction = UnityEditor.GenericMenu.MenuFunction;
    using MenuFunctionData = UnityEditor.GenericMenu.MenuFunction2;

    public static class EditorInputSystem
    {

        private static List<KeyValuePair<EventHandlerAttribute, Delegate>> eventHandlers;
        private static List<KeyValuePair<HotkeyAttribute, Delegate>> hotkeyHandlers;
        private static List<KeyValuePair<ContextEntryAttribute, MenuFunctionData>> contextEntries;

        public static void SetupInput(List<Type> setType)
        {
            eventHandlers = new List<KeyValuePair<EventHandlerAttribute, Delegate>>();
            hotkeyHandlers = new List<KeyValuePair<HotkeyAttribute, Delegate>>();
            contextEntries = new List<KeyValuePair<ContextEntryAttribute, MenuFunctionData>>();

            IEnumerable<Assembly> scriptAssemblies = AppDomain.CurrentDomain.GetAssemblies().Where((Assembly assembly) => assembly.FullName.Contains("Assembly"));
            foreach (Assembly assembly in scriptAssemblies)
            {
                foreach (Type type in assembly.GetTypes())
                {
                    bool getType = false;
                    foreach(var t in setType)
                    {
                        if (type == t || type == typeof(EditorState))
                        {
                            getType = true;
                            break;
                        }
                    }

                    if (!getType)
                        continue;
                    
                    foreach (MethodInfo method in type.GetMethods(BindingFlags.FlattenHierarchy | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static))
                    {
                        Delegate actionDelegate = null;
                        foreach (object attr in method.GetCustomAttributes(true))
                        {
                            Type attrType = attr.GetType();
                            if (attrType == typeof(EventHandlerAttribute))
                            {
                                if (EventHandlerAttribute.AssureValidity(method, attr as EventHandlerAttribute))
                                {
                                    if (actionDelegate == null)
                                    {
                                        actionDelegate = Delegate.CreateDelegate(typeof(Action<EditorInputInfo>), method);
                                    }

                                    eventHandlers.Add(new KeyValuePair<EventHandlerAttribute, Delegate>(attr as EventHandlerAttribute, actionDelegate));
                                }
                            }
                            else if (attrType == typeof(HotkeyAttribute))
                            {
                                if (HotkeyAttribute.AssureValidity(method, attr as HotkeyAttribute))
                                {
                                    if (actionDelegate == null)
                                    {
                                        actionDelegate = Delegate.CreateDelegate(typeof(Action<EditorInputInfo>), method);
                                    }
                                    hotkeyHandlers.Add(new KeyValuePair<HotkeyAttribute, Delegate>(attr as HotkeyAttribute, actionDelegate));
                                }
                            }
                            else if (attrType == typeof(ContextEntryAttribute))
                            {
                                if (ContextEntryAttribute.AssureValidity(method, attr as ContextEntryAttribute))
                                {
                                    if (actionDelegate == null) actionDelegate = Delegate.CreateDelegate(typeof(Action<EditorInputInfo>), method);

                                    MenuFunctionData menuFunction = (object callbackObj) =>
                                    {
                                        if (!(callbackObj is EditorInputInfo))
                                            throw new UnityException("Callback Object passed by context is not of type EditorMenuCallback!");
                                        actionDelegate.DynamicInvoke(callbackObj as EditorInputInfo);
                                    };
                                    contextEntries.Add(new KeyValuePair<ContextEntryAttribute, MenuFunctionData>(attr as ContextEntryAttribute, menuFunction));
                                }
                            }
                        }
                    }
                }
            }

            eventHandlers.Sort((handlerA, handlerB) => handlerA.Key.priority.CompareTo(handlerB.Key.priority));
            hotkeyHandlers.Sort((handlerA, handlerB) => handlerA.Key.priority.CompareTo(handlerB.Key.priority));
        }


        private static void CallEventHandlers(EditorInputInfo inputInfo, bool late)
        {
            object[] parameter = new object[] { inputInfo };
            foreach (KeyValuePair<EventHandlerAttribute, Delegate> eventHandler in eventHandlers)
            {
                if ((eventHandler.Key.handledEvent == null || eventHandler.Key.handledEvent == inputInfo.inputEvent.type)
                    && (late ? eventHandler.Key.priority >= 100 : eventHandler.Key.priority < 100))
                {
                    eventHandler.Value.DynamicInvoke(parameter);
                    if (inputInfo.inputEvent.type == EventType.Used)
                        return;
                }
            }
        }

        private static void CallHotkeys(EditorInputInfo inputInfo, KeyCode keyCode, EventModifiers mods)
        {
            object[] parameter = new object[] { inputInfo };
            foreach (KeyValuePair<HotkeyAttribute, Delegate> hotKey in hotkeyHandlers)
            {
                if (hotKey.Key.handledHotKey == keyCode &&
                    (hotKey.Key.modifiers == null || hotKey.Key.modifiers == mods) &&
                    (hotKey.Key.limitingEventType == null || hotKey.Key.limitingEventType == inputInfo.inputEvent.type))
                {
                    hotKey.Value.DynamicInvoke(parameter);
                    if (inputInfo.inputEvent.type == EventType.Used)
                        return;
                }
            }
        }

        public static void FillContextMenu(EditorInputInfo inputInfo, GenericMenu contextMenu)
        {
            foreach (KeyValuePair<ContextEntryAttribute, MenuFunctionData> contextEntry in contextEntries)
            {
                contextMenu.AddItem(new GUIContent(contextEntry.Key.contextPath), false, contextEntry.Value, inputInfo);
            }
        }


        public static void HandleInputEvents(EditorState state)
        {
            if (shouldIgnoreInput(state))
                return;

            EditorInputInfo inputInfo = new EditorInputInfo(state);
            CallEventHandlers(inputInfo, false);
            CallHotkeys(inputInfo, Event.current.keyCode, Event.current.modifiers);
        }

        public static void HandleLateInputEvents(EditorState state)
        {
            if (shouldIgnoreInput(state))
                return;

            EditorInputInfo inputInfo = new EditorInputInfo(state);
            CallEventHandlers(inputInfo, true);
        }

        internal static bool shouldIgnoreInput(EditorState state)
        {
            if (state == null)
                return true;
            //if (OverlayGUI.HasPopupControl())
            //return true;
            //if (!state.canvasRect.Contains(Event.current.mousePosition))
            //    return true;
            //for (int ignoreCnt = 0; ignoreCnt < state.ignoreInput.Count; ignoreCnt++)
            //{
            //    if (state.ignoreInput[ignoreCnt].Contains(Event.current.mousePosition))
            //        return true;
            //}
            return false;
        }

        
    }

    public class EditorInputInfo
    {
        public string message;
        public EditorState editorState;
        public Event inputEvent;
        public Vector2 inputPos;

        public EditorInputInfo(EditorState EditorState)
        {
            message = null;
            editorState = EditorState;
            inputEvent = Event.current;
            inputPos = inputEvent.mousePosition;
        }

        public EditorInputInfo(string Message, EditorState EditorState)
        {
            message = Message;
            editorState = EditorState;
            inputEvent = Event.current;
            inputPos = inputEvent.mousePosition;
        }

        public void SetAsCurrentEnvironment()
        {
            //NodeEditor.curEditorState = editorState;
            //NodeEditor.curNodeCanvas = editorState.canvas;
        }
    }


    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class EventHandlerAttribute : Attribute
    {
        public EventType? handledEvent { get; private set; }
        public int priority { get; private set; }

        public EventHandlerAttribute(EventType eventType, int priorityValue)
        {
            handledEvent = eventType;
            priority = priorityValue;
        }

        public EventHandlerAttribute(int priorityValue)
        {
            handledEvent = null;
            priority = priorityValue;
        }

        public EventHandlerAttribute(EventType eventType)
        {
            handledEvent = eventType;
            priority = 50;
        }

        public EventHandlerAttribute()
        {
            handledEvent = null;
        }

        internal static bool AssureValidity(MethodInfo method, EventHandlerAttribute attr)
        {
            if (!method.IsGenericMethod && !method.IsGenericMethodDefinition && (method.ReturnType == null || method.ReturnType == typeof(void)))
            {
                ParameterInfo[] methodParams = method.GetParameters();
                if (methodParams.Length == 1 && methodParams[0].ParameterType == typeof(EditorInputInfo))
                    return true;
                else
                    Debug.LogWarning("Method " + method.Name + " has incorrect signature for EventHandlerAttribute!");
            }
            return false;
        }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class ContextEntryAttribute : Attribute
    {
        public string contextPath { get; private set; }

        public ContextEntryAttribute(string path)
        {
            contextPath = path;
        }

        internal static bool AssureValidity(MethodInfo method, ContextEntryAttribute attr)
        {
            if (!method.IsGenericMethod && !method.IsGenericMethodDefinition && (method.ReturnType == null || method.ReturnType == typeof(void)))
            {
                ParameterInfo[] methodParams = method.GetParameters();
                if (methodParams.Length == 1 && methodParams[0].ParameterType == typeof(EditorInputInfo))
                    return true;
                else
                    Debug.LogWarning("Method " + method.Name + " has incorrect signature for ContextAttribute!");
            }
            return false;
        }
    }


    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class HotkeyAttribute : Attribute
    {
        public KeyCode handledHotKey { get; private set; }
        public EventModifiers? modifiers { get; private set; }
        public EventType? limitingEventType { get; private set; }
        public int priority { get; private set; }

        public HotkeyAttribute(KeyCode handledKey)
        {
            handledHotKey = handledKey;
            modifiers = null;
            limitingEventType = null;
            priority = 50;
        }

        public HotkeyAttribute(KeyCode handledKey, EventModifiers eventModifiers)
        {
            handledHotKey = handledKey;
            modifiers = eventModifiers;
            limitingEventType = null;
            priority = 50;
        }

        public HotkeyAttribute(KeyCode handledKey, EventType LimitEventType)
        {
            handledHotKey = handledKey;
            modifiers = null;
            limitingEventType = LimitEventType;
            priority = 50;
        }

        public HotkeyAttribute(KeyCode handledKey, EventType LimitEventType, int priorityValue)
        {
            handledHotKey = handledKey;
            modifiers = null;
            limitingEventType = LimitEventType;
            priority = priorityValue;
        }


        public HotkeyAttribute(KeyCode handledKey, EventModifiers eventModifiers, EventType LimitEventType)
        {
            handledHotKey = handledKey;
            modifiers = eventModifiers;
            limitingEventType = LimitEventType;
            priority = 50;
        }

        public HotkeyAttribute(KeyCode handledKey, EventModifiers eventModifiers, EventType LimitEventType, int priorityValue)
        {
            handledHotKey = handledKey;
            modifiers = eventModifiers;
            limitingEventType = LimitEventType;
            priority = priorityValue;
        }

        internal static bool AssureValidity(MethodInfo method, HotkeyAttribute attr)
        {
            if (!method.IsGenericMethod && !method.IsGenericMethodDefinition && (method.ReturnType == null || method.ReturnType == typeof(void)))
            {
                ParameterInfo[] methodParams = method.GetParameters();
                if (methodParams.Length == 1 && methodParams[0].ParameterType.IsAssignableFrom(typeof(EditorInputInfo)))
                    return true;
                else
                    Debug.LogWarning("Method " + method.Name + " has incorrect signature for HotkeyAttribute!");
            }
            return false;
        }
    }
}