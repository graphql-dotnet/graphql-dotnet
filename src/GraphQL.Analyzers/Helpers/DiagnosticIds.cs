namespace GraphQL.Analyzers.Helpers;

public static class DiagnosticIds
{
    public const string DEFINE_THE_NAME_IN_FIELD_METHOD = "GQL001";
    public const string NAME_METHOD_INVOCATION_CAN_BE_REMOVED = "GQL002";
    public const string DIFFERENT_NAMES_DEFINED_BY_FIELD_AND_NAME_METHODS = "GQL003";
    public const string DO_NOT_USE_OBSOLETE_FIELD_METHODS = "GQL004";
    // placeholder for ILLEGAL_RESOLVER_USAGE = "GQL005";
    public const string CAN_NOT_MATCH_INPUT_FIELD_TO_THE_SOURCE_FIELD = "GQL006";
    public const string CAN_NOT_SET_SOURCE_FIELD = "GQL007";
    public const string DO_NOT_USE_OBSOLETE_ARGUMENT_METHOD = "GQL008";
    public const string USE_ASYNC_RESOLVER = "GQL009";
    public const string CAN_NOT_RESOLVE_INPUT_SOURCE_TYPE_CONSTRUCTOR = "GQL010";
    public const string MUST_NOT_BE_CONVERTIBLE_TO_GRAPH_TYPE = "GQL011";
    public const string ILLEGAL_METHOD_USAGE = "GQL012";
    public const string ONE_OF_FIELDS_MUST_BE_NULLABLE = "GQL013";
    public const string ONE_OF_FIELDS_MUST_NOT_HAVE_DEFAULT_VALUE = "GQL014";
    public const string CAN_NOT_INFER_FIELD_NAME_FROM_EXPRESSION = "GQL015";
    public const string REQUIRE_PARAMETERLESS_CONSTRUCTOR = "GQL016";
    public const string COULD_NOT_FIND_METHOD = "GQL017";
    public const string PARSER_METHOD_MUST_BE_VALID = "GQL018";
    public const string VALIDATOR_METHOD_MUST_BE_VALID = "GQL019";
    public const string VALIDATE_ARGUMENTS_METHOD_MUST_BE_VALID = "GQL020";
    public const string NULLABLE_REFERENCE_TYPE_ARGUMENT_SHOULD_SPECIFY_NULLABLE = "GQL021";

    public const string KEY_FIELD_DOES_NOT_EXIST = "GQLFED001";
    public const string KEY_MUST_NOT_BE_NULL_OR_EMPTY = "GQLFED002";
    public const string DUPLICATE_KEY = "GQLFED003";
    public const string REDUNDANT_KEY = "GQLFED004";
    public const string KEY_FIELD_MUST_NOT_HAVE_ARGUMENTS = "GQLFED005";
}
