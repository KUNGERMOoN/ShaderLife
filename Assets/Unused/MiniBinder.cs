/*using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace GameOfLife.GUI.DataBinding
{
    public static class MiniBinder
    {
        private static readonly List<IDisposable> Disposables = new();

        public static void Bind<T>(BaseField<T> element, Bindable<T> bindable,
            Processor<T> elementProcessor = null, Processor<T> bindableProcessor = null)
        {
            UIBindable<T> uiBindable = UIBindable<T>.Get(element);
            uiBindable.Bind(bindable, elementProcessor, bindableProcessor);
        }

        public static void BindButton(Button button, Action clicked)
        {
            ButtonCallback binder = new(button, clicked);
            if (!Disposables.Contains(binder)) Disposables.Add(binder);
        }

        public static void Bind<T>(Bindable<T> bindable, Action<T> action)
            => bindable.Bind(action);

        public static void Bind<T>(BaseField<T> element, Action<T> action)
        {
            UIBindable<T> uiBindable = UIBindable<T>.Get(element);
            uiBindable.Bind(action);
        }

        public static void Dispose()
        {
            UIBindableManager.ClearDisposables();
            foreach (var ui in Disposables)
            {
                ui.Dispose();
            }
            Disposables.Clear();
        }
    }
}*/