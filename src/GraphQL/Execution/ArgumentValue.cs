namespace GraphQL.Execution
{
    public struct ArgumentValue
    {
        public ArgumentValue(object value, ArgumentSource source)
        {
            Value = value;
            Source = source;
        }

        public object Value { get; }
        public ArgumentSource Source { get; }
    }

    public enum ArgumentSource
    {
        FieldDefault,
        Literal,
        Variable
    }

}
