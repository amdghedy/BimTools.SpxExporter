namespace BimTools.SpxExporter.Models
{
    public sealed class ChoiceItem<T>
    {
        public ChoiceItem(T value, string text)
        {
            Value = value;
            Text = text;
        }

        public T Value { get; }

        public string Text { get; }
    }
}
