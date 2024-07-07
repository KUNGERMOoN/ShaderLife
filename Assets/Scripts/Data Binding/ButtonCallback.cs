using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

#pragma warning disable IDE0028 // Simplify collection initialization

namespace GameOfLife.DataBinding
{
    using CallbacksMap = Dictionary<Action, EventCallback<ClickEvent>>;

    /// <summary>
    /// Utility that keeps track of Button click callbacks and disposing them
    /// </summary>
    public static class ButtonCallback
    {
        public static Dictionary<Button, CallbacksMap> ButtonCallbacks = new();

        public static void Bind(this Button button, Action clicked)
        {
            void callback(ClickEvent ctx) => clicked();
            button.RegisterCallback<ClickEvent>(callback);

            if (ButtonCallbacks.TryGetValue(button, out CallbacksMap callbacksMap))
                callbacksMap.Add(clicked, callback);
            else
            {
                callbacksMap = new CallbacksMap();
                callbacksMap.Add(clicked, callback);
                ButtonCallbacks.Add(button, callbacksMap);
            }
        }

        public static void Unbind(this Button button, Action clicked)
        {
            if (ButtonCallbacks.TryGetValue(button, out CallbacksMap callbacksMap) == false) return;
            if (callbacksMap.ContainsKey(clicked) == false) return;

            button.UnregisterCallback(callbacksMap[clicked]);
            callbacksMap.Remove(clicked);

            if (callbacksMap.Count == 0) ButtonCallbacks.Remove(button);
        }

        public static void Dispose()
        {
            foreach (KeyValuePair<Button, CallbacksMap> pair in ButtonCallbacks)
            {
                var button = pair.Key;
                var callbackMap = pair.Value;
                foreach (var callback in callbackMap.Values)
                {
                    button.UnregisterCallback(callback);
                }
                callbackMap.Clear();
            }
            ButtonCallbacks.Clear();
        }
    }
}