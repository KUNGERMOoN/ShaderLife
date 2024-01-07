using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine.UIElements;

namespace MiniBinding
{
    public static class MiniBinder
    {
        public static List<IDisposable> UICallbacks = new();
        private static class GenericData<T>
        {
            public static Dictionary<int, Bindable<T>> Bindables = new();
        }

        public static T Get<T>(
            [CallerMemberName] string propertyName = "",
            [CallerFilePath] string file = "",
            [CallerLineNumber] int line = 0)
        {
            int id = GetPropertyId(propertyName, file, line);
            Bindable<T> bindable = GetById<T>(id);
            return bindable.Value;
        }

        public static void Set<T>(T value,
            [CallerMemberName] string propertyName = "",
            [CallerFilePath] string file = "",
            [CallerLineNumber] int line = 0)
        {
            int id = GetPropertyId(propertyName, file, line);
            Bindable<T> bindable = GetById<T>(id);
            bindable.Value = value;
        }

        public static void Bind<T>(BaseField<T> ui,
            string propertyName = "", string file = "", int line = 0,
            Func<T, T> @in = null, Func<T, T> @out = null)
        {
            UIBindable<T> uiBindable = new(ui, @in, @out);
            UICallbacks.Add(uiBindable);

            int id = GetPropertyId(propertyName, file, line);
            Bindable<T> bindable = GetById<T>(id);
            uiBindable.Bind(bindable);
        }

        public static void Bind<T>(BaseField<T> ui, Bindable<T> bindable,
            Func<T, T> @in = null, Func<T, T> @out = null)
        {
            UIBindable<T> uiBindable = new(ui, @in, @out);
            UICallbacks.Add(uiBindable);
            uiBindable.Bind(bindable);
        }

        public static void Bind(Button button, Action clicked)
        {
            ButtonBinder binder = new(button, clicked);
            UICallbacks.Add(binder);
        }

        public static void UnbindUI()
        {
            foreach (var ui in UICallbacks)
            {
                ui.Dispose();
            }
            UICallbacks.Clear();
        }

        static Bindable<T> GetById<T>(int id)
        {
            Bindable<T> bindable;

            if (GenericData<T>.Bindables.ContainsKey(id))
            {
                bindable = GenericData<T>.Bindables[id];
            }
            else
            {
                bindable = new();
                GenericData<T>.Bindables.Add(id, bindable);
            }

            return bindable;
        }



        static int GetPropertyId(string propertyName, string file, int line)
            => HashCode.Combine(BindableType.Property, propertyName, file, line);

        enum BindableType { Property, UIElement }
    }
}