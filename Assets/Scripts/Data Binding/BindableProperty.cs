namespace GameOfLife.DataBinding
{
    public class BindableProperty<T> : Bindable<T>
    {
        protected override T _value { get; set; }

        public BindableProperty() { }

        public BindableProperty(T value) : base(value) { }
    }
}