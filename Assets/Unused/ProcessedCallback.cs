/*using System;

namespace MiniBinding
{
    public class ProcessedCallback<T> : IEquatable<ProcessedCallback<T>>
    {
        public readonly Callback<T> Callback;
        public readonly Processor<T> Processor;

        public static void EmptyProcessor(T input, out T output) => output = input;

        public void Invoke(T value, byte version)
        {
            Processor(value, out T processedValue);
            Callback(processedValue, version);
        }

        public ProcessedCallback(Callback<T> callback, Processor<T> processor = null)
        {
            if (callback == null) throw new ArgumentNullException(nameof(callback));

            this.Callback = callback;
            processor ??= EmptyProcessor;
            this.Processor = processor;
        }

        public override bool Equals(object obj)
        {
            if (obj is not ProcessedCallback<T> other) return false;
            return other.Callback == Callback;
        }

        public override int GetHashCode() => Callback.GetHashCode();

        public bool Equals(ProcessedCallback<T> other)
        {
            if (other == null) return false;
            return other.Callback == Callback;
        }
    }

    
}*/