using System.Collections.Generic;

namespace MiniBinding
{
    public class Bindable<T>
    {
        T value;
        public T Value
        {
            get => this.value;
            set
            {
                OnChanged(value);
                this.value = value;
                foreach (var bindable in Bound)
                {
                    bindable.OnChanged(value);
                    bindable.value = value;
                }
            }
        }

        protected virtual void OnChanged(T newValue) { }

        readonly List<Bindable<T>> Bound = new();

        public void Bind(Bindable<T> bindable)
        {
            if (bindable == this) return;

            if (Bound.Contains(bindable) == false)
            {
                Bound.Add(bindable);
                value = bindable.value;
                bindable.Bind(this);
            }
        }

        public void Unbind(Bindable<T> bindable)
        {

            if (bindable == this) return;

            if (Bound.Contains(bindable) == true)
            {
                Bound.Remove(bindable);
                bindable.Unbind(this);
            }
        }

        public Bindable() { }
        public Bindable(T value)
            => this.value = value;

        override public string ToString() => value.ToString();
    }
}