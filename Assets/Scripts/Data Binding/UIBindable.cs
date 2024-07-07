using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace GameOfLife.DataBinding
{
    public class UIBindable<T> : Bindable<T>, IDisposable
    {
        private static readonly Dictionary<BaseField<T>, UIBindable<T>> Cache = new();

        public static UIBindable<T> Get(BaseField<T> element)
        {
            UIBindable<T> bindable;
            if (Cache.TryGetValue(element, out bindable))
                return bindable;
            else
            {
                bindable = new UIBindable<T>(element);
                UIBindingManager.AddDisposable(bindable);
                Cache.Add(element, bindable);
                return bindable;
            }
        }

        public readonly BaseField<T> VisualElement;

        protected override T _value
        {
            get => VisualElement.value;
            set => VisualElement.value = value;
        }

        private UIBindable(BaseField<T> visualElement)
        {
            VisualElement = visualElement;
            VisualElement.RegisterCallback<ChangeEvent<T>>(OnUiChanged);
        }

        public virtual void Dispose()
        {
            UIBindingManager.RemoveDisposable(this);
            Cache.Remove(VisualElement);
            VisualElement.UnregisterCallback<ChangeEvent<T>>(OnUiChanged);
        }

        void OnUiChanged(ChangeEvent<T> ctx)
            => Value = ctx.newValue;
    }
}
