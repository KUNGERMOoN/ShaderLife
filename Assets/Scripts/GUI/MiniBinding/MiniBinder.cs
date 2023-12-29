using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine.UIElements;

namespace MiniBinding
{
    public static class MiniBinder<T>
    {
        static Dictionary<int, Bindable<T>> Bindables = new();
        static List<UIBindable<T>> UIBindables = new();

        public static T Get(
            [CallerMemberName] string propertyName = "",
            [CallerFilePath] string file = "",
            [CallerLineNumber] int line = 0)
        {
            int id = GetPropertyId(propertyName, file, line);
            Bindable<T> bindable = GetById(id);
            return bindable.Value;
        }

        public static void Set(T value,
            [CallerMemberName] string propertyName = "",
            [CallerFilePath] string file = "",
            [CallerLineNumber] int line = 0)
        {
            int id = GetPropertyId(propertyName, file, line);
            Bindable<T> bindable = GetById(id);
            bindable.Value = value;
        }

        public static void Bind(BaseField<T> ui, string propertyName = "", string file = "", int line = 0)
        {
            UIBindable<T> uiBindable = new(ui);
            UIBindables.Add(uiBindable);

            int id = GetPropertyId(propertyName, file, line);
            Bindable<T> bindable = GetById(id);
            uiBindable.Bind(bindable);
        }

        public static void Bind(BaseField<T> ui, Bindable<T> bindable)
        {
            UIBindable<T> uiBindable = new(ui);
            UIBindables.Add(uiBindable);
            uiBindable.Bind(bindable);
        }

        public static void UnbindUI()
        {
            foreach (var ui in UIBindables)
            {
                ui.Dispose();
            }
            UIBindables.Clear();
        }

        static Bindable<T> GetById(int id)
        {
            Bindable<T> bindable;

            if (Bindables.ContainsKey(id))
            {
                bindable = Bindables[id];
            }
            else
            {
                bindable = new();
                Bindables.Add(id, bindable);
            }

            return bindable;
        }

        static int GetPropertyId(string propertyName, string file, int line)
            => HashCode.Combine(BindableType.Property, propertyName, file, line);

        enum BindableType { Property, UIElement }
    }
}