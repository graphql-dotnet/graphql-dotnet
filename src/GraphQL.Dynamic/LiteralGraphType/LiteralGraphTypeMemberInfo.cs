namespace GraphQL.Dynamic.Types.LiteralGraphType
{
    internal class LiteralGraphTypeMemberInfo
    {
        public GetValueFn GetValueFn { get; set; }
        public LiteralGraphTypeMemberInfoType Type { get; set; }
        public bool IsList { get; set; }
        public string Name { get; set; }
        public string DeclaringTypeName { get; set; }
    }

    internal enum LiteralGraphTypeMemberInfoType
    {
        Unknown,
        String,
        Boolean,
        Int,
        Long,
        Float,
        Double,
        Guid,
        DateTime,
        DateTimeOffset,
        Complex
    }
}
