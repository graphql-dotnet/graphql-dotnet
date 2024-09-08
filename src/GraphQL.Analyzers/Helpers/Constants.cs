// ReSharper disable InconsistentNaming

namespace GraphQL.Analyzers.Helpers;

#pragma warning disable IDE1006 // Naming Styles
public static class Constants
{
    public const string GraphQL = "GraphQL";

    public static class AttributeNames
    {
        public const string Attribute = "Attribute";
        public const string DefaultValue = "DefaultValue";
        public const string Ignore = "Ignore";
        public const string OneOf = "OneOf";
        public const string Parser = "Parser";
        public const string ValidateArguments = "ValidateArguments";
        public const string Validator = "Validator";
    }

    public static class ArgumentNames
    {
        public const string Arguments = "arguments";
        public const string Configure = "configure";
        public const string DefaultValue = "defaultValue";
        public const string DeprecationReason = "deprecationReason";
        public const string Description = "description";
        public const string Expression = "expression";
        public const string Name = "name";
        public const string Nullable = "nullable";
        public const string Resolve = "resolve";
        public const string Subscribe = "subscribe";
        public const string Type = "type";
    }

    public static class MethodNames
    {
        public const string Argument = "Argument";
        public const string Arguments = "Arguments";
        public const string Connection = "Connection";
        public const string Create = "Create";
        public const string DefaultValue = "DefaultValue";
        public const string DeprecationReason = "DeprecationReason";
        public const string Description = "Description";
        public const string Field = "Field";
        public const string FieldAsync = "FieldAsync";
        public const string FieldDelegate = "FieldDelegate";
        public const string FieldSubscribe = "FieldSubscribe";
        public const string FieldSubscribeAsync = "FieldSubscribeAsync";
        public const string Name = "Name";
        public const string ParseDictionary = "ParseDictionary";
        public const string Resolve = "Resolve";
        public const string ResolveAsync = "ResolveAsync";
        public const string ResolveDelegate = "ResolveDelegate";
        public const string ResolveScoped = "ResolveScoped";
        public const string ResolveScopedAsync = "ResolveScopedAsync";
        public const string ResolveStream = "ResolveStream";
        public const string ResolveStreamAsync = "ResolveStreamAsync";
        public const string Returns = "Returns";
    }

    public static class ObjectProperties
    {
        public const string DefaultValue = "DefaultValue";
    }

    public static class AnalyzerProperties
    {
        public const string BuilderMethodName = "BuilderMethodName";
        public const string IsAsync = "IsAsync";
        public const string IsDelegate = "IsDelegate";
        public const string IsExpression = "IsExpression";
    }

    public static class Types
    {
        public const string AutoRegisteringInputObjectGraphType = "AutoRegisteringInputObjectGraphType";
        public const string FieldBuilder = "FieldBuilder";
        public const string FieldType = "FieldType";
        public const string GraphQLConstructorAttribute = "GraphQLConstructorAttribute";
        public const string NonNullGraphType = "NonNullGraphType";
    }

    public static class Interfaces
    {
        public const string IInputObjectGraphType = "IInputObjectGraphType";
        public const string IObjectGraphType = "IObjectGraphType";
    }

    public static class MetadataNames
    {
        public const string AllowedOnAttribute = "AllowedOnAttribute";
        public const string GraphQLConstructorAttribute = "GraphQL.GraphQLConstructorAttribute";
        public const string IGraphType = "GraphQL.Types.IGraphType";
        public const string InputObjectGraphType = "GraphQL.Types.InputObjectGraphType`1";
        public const string NotAGraphTypeAttribute = "NotAGraphTypeAttribute";
        public const string RequireParameterlessConstructorAttribute = "RequireParameterlessConstructorAttribute";
    }
}

#pragma warning restore IDE1006 // Naming Styles
