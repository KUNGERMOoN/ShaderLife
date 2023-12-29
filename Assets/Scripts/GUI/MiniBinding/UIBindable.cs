using System;
using UnityEngine.UIElements;

namespace MiniBinding
{
    public class UIBindable<T> : Bindable<T>, IDisposable
    {
        public readonly BaseField<T> VisualElement;

        protected override void OnChanged(T newValue)
            => VisualElement.value = newValue;

        public UIBindable(BaseField<T> visualElement)
        {
            VisualElement = visualElement;
            VisualElement.RegisterCallback<ChangeEvent<T>>(OnUiChanged);
        }

        void OnUiChanged(ChangeEvent<T> ctx) => Value = ctx.newValue;

        public void Dispose()
        {
            VisualElement.UnregisterCallback<ChangeEvent<T>>(OnUiChanged);
        }
    }
}
