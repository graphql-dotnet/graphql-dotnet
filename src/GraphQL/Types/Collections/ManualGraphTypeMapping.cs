namespace GraphQL.Types.Collections;

public class ManualGraphTypeMapping : IGraphTypeMapping
{
    private readonly Type _clrType;
    private readonly Type _graphType;
    private readonly bool _isInputType;
    private readonly bool _isOutputType;

    public ManualGraphTypeMapping(Type clrType, Type graphType)
    {
        _clrType = clrType ?? throw new ArgumentNullException(nameof(clrType));
        _graphType = graphType ?? throw new ArgumentNullException(nameof(graphType));
        _isInputType = graphType.IsInputType();
        _isOutputType = graphType.IsOutputType();
    }

    public Type? GetGraphTypeFromClrType(Type clrType, bool isInputType, Type? preferredGraphType)
        => clrType == _clrType && (isInputType && _isInputType || !isInputType && _isOutputType) ? _graphType : preferredGraphType;
}
