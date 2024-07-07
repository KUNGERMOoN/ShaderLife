using System;
using System.Collections.Generic;
using System.Linq;

namespace GameOfLife.DataBinding
{
    public delegate T Processor<T>(T input);

    public abstract class Bindable<T>
    {
        readonly List<Callback> Callbacks = new();

        public T Value
        {
            get => _value;
            set
            {
                Propagate(value, (byte)(version + 1));
            }
        }

#pragma warning disable IDE1006
        protected abstract T _value { get; set; }
#pragma warning restore IDE1006

        //We use the version value to determine whether a change has been already applied to this bindable
        private byte version;

        private void Propagate(T newValue, byte newVersion)
        {
            //Skip if the changes have already been applied, to avoid endless recursion
            if (newVersion == version) return;

            _value = newValue;
            version = newVersion;

            foreach (var callback in Callbacks)
                callback.Invoke(newValue);
        }

        public void Bind(Action<T> action)
        {
            if (action == null) return;
            if (Callbacks.Any(
                c => c.Action == action)) return;

            Callbacks.Add(new(action));
        }

        public void Bind(Bindable<T> target, Processor<T> processor = null, Processor<T> targetProcessor = null)
        {
            if (target == this || target == null) return;

            if (Callbacks
                .OfType<BindableCallback>()
                .Any(bc => bc.Bindable == target))
                return;

            processor ??= x => x;
            targetProcessor ??= x => x;

            Callbacks.Add(new BindableCallback(this, target, targetProcessor));
            target.Callbacks.Add(new BindableCallback(target, this, processor));

            byte newVersion = version != target.version
                ? target.version
                : (byte)(version + 1);

            Propagate(processor(target.Value), newVersion);
        }

        public void Unbind(Action<T> action)
        {
            if (action == null) return;

            var callback = Callbacks
                .FirstOrDefault(c => c.Action == action);

            if (callback == null) return;

            Callbacks.Remove(callback);
        }

        public void Unbind(Bindable<T> target)
        {
            if (target == this || target == null) return;

            BindableCallback callback = Callbacks
                .OfType<BindableCallback>()
                .FirstOrDefault(x => x.Bindable == target);

            if (callback == null) return;

            BindableCallback targetCallback = target.Callbacks
                .OfType<BindableCallback>()
                .FirstOrDefault(x => x.Bindable == this);

            Callbacks.Remove(callback);
            target.Callbacks.Remove(targetCallback);
        }

        public Bindable() { }

        public Bindable(T value)
        {
            _value = value;
        }

        public static implicit operator T(Bindable<T> bindable)
        => bindable.Value;

        override public string ToString() => Value?.ToString();

        private class Callback
        {
            public readonly Action<T> Action;

            public void Invoke(T value) => Action(value);

            public Callback(Action<T> action)
                => Action = action;
        }

        private class BindableCallback : Callback
        {
            public readonly Bindable<T> Bindable;

            public BindableCallback(Bindable<T> caller, Bindable<T> bindable, Processor<T> processor) : base(
                action: value => bindable.Propagate(processor(value), caller.version))
            {
                Bindable = bindable;
            }
        }
    }
}