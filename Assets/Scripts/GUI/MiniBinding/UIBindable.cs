using System;
using UnityEngine.UIElements;

namespace MiniBinding
{
    public class UIBindable<T> : Bindable<T>, IDisposable
    {
        public readonly BaseField<T> VisualElement;

        protected override void OnValueChanged(T newVal)
        {
            VisualElement.value = newVal;
        }

        public UIBindable(BaseField<T> visualElement, Func<T, T> @in = null, Func<T, T> @out = null)
            : base(visualElement.value, @in, @out)
        {
            VisualElement = visualElement;
            VisualElement.RegisterCallback<ChangeEvent<T>>(OnUiChanged);
        }

        void OnUiChanged(ChangeEvent<T> ctx)
        {
            Value = Out(ctx.newValue);
        }

        public void Dispose()
        {
            VisualElement.UnregisterCallback<ChangeEvent<T>>(OnUiChanged);
        }
    }
}
