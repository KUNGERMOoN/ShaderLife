using System;
using System.Collections.Generic;

namespace MiniBinding
{
    public class Bindable<T>
    {
        readonly List<Bindable<T>> Bound = new();

        protected T value;
        public T Value
        {
            get => this.value;
            set
            {
                Apply(value);
            }
        }

        void Apply(T newValue)
        {
            T newInternalValue = In(newValue);
            if (newInternalValue.Equals(value) == false)
            {
                OnValueChanged(newInternalValue);
                value = newInternalValue;
                foreach (var bindable in Bound)
                {
                    bindable.Apply(Out(value));
                }
            }
        }

        protected virtual void OnValueChanged(T newVal) { }

        protected readonly Func<T, T> In, Out;

        public void Bind(Bindable<T> bindable)
        {
            if (bindable == this) return;

            if (Bound.Contains(bindable) == false)
            {
                Bound.Add(bindable);
                bindable.Bind(this);
                Value = bindable.value;
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

        public Bindable(Func<T, T> @in = null, Func<T, T> @out = null)
        {
            //Todo: check if value == @out(@in(value)).
            //If it's false, throw an exception - such behaviour could lead
            //to infinite loops or StackOverflow
            @in ??= x => x;
            In = @in;
            @out ??= x => x;
            Out = @out;
        }

        public Bindable(T value, Func<T, T> @in = null, Func<T, T> @out = null) : this(@in, @out)
        {
            this.value = value;
        }

        override public string ToString() => value.ToString();
    }
}