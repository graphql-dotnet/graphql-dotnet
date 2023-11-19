namespace GraphQL.Analyzers.Helpers;

public static class DiagnosticIds
{
    public const string DEFINE_THE_NAME_IN_FIELD_METHOD = "GQL001";
    public const string NAME_METHOD_INVOCATION_CAN_BE_REMOVED = "GQL002";
    public const string DIFFERENT_NAMES_DEFINED_BY_FIELD_AND_NAME_METHODS = "GQL003";
    public const string DO_NOT_USE_OBSOLETE_FIELD_METHODS = "GQL004";
    public const string ILLEGAL_RESOLVER_USAGE = "GQL005";
    public const string CAN_NOT_MATCH_INPUT_FIELD_TO_THE_SOURCE_FIELD = "GQL006";
    public const string CAN_NOT_SET_SOURCE_FIELD = "GQL007";
    public const string DO_NOT_USE_OBSOLETE_ARGUMENT_METHOD = "GQL008";
    public const string USE_ASYNC_RESOLVER = "GQL009";
    public const string CAN_NOT_RESOLVE_INPUT_OBJECT_CONSTRUCTOR = "GQL010";
}
