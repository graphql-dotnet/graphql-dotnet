using GraphQL.Validation;
using GraphQL.Validation.Errors;

namespace GraphQL.Tests.Validation;

public class ValidationCodeTests
{
    // be sure to update the documentation if any of these codes or numbers change - see errors.md

    [Theory]
    [InlineData(typeof(ValidationError), "VALIDATION_ERROR")]
    [InlineData(typeof(ArgumentsOfCorrectTypeError), "ARGUMENTS_OF_CORRECT_TYPE")]
    [InlineData(typeof(DefaultValuesOfCorrectTypeError), "DEFAULT_VALUES_OF_CORRECT_TYPE")]
    [InlineData(typeof(FieldsOnCorrectTypeError), "FIELDS_ON_CORRECT_TYPE")]
    [InlineData(typeof(FragmentsOnCompositeTypesError), "FRAGMENTS_ON_COMPOSITE_TYPES")]
    [InlineData(typeof(KnownArgumentNamesError), "KNOWN_ARGUMENT_NAMES")]
    [InlineData(typeof(KnownDirectivesError), "KNOWN_DIRECTIVES")]
    [InlineData(typeof(DirectivesInAllowedLocationsError), "DIRECTIVES_IN_ALLOWED_LOCATIONS")]
    [InlineData(typeof(KnownFragmentNamesError), "KNOWN_FRAGMENT_NAMES")]
    [InlineData(typeof(KnownTypeNamesError), "KNOWN_TYPE_NAMES")]
    [InlineData(typeof(LoneAnonymousOperationError), "LONE_ANONYMOUS_OPERATION")]
    [InlineData(typeof(NoFragmentCyclesError), "NO_FRAGMENT_CYCLES")]
    [InlineData(typeof(NoUndefinedVariablesError), "NO_UNDEFINED_VARIABLES")]
    [InlineData(typeof(NoUnusedFragmentsError), "NO_UNUSED_FRAGMENTS")]
    [InlineData(typeof(NoUnusedVariablesError), "NO_UNUSED_VARIABLES")]
    [InlineData(typeof(OverlappingFieldsCanBeMergedError), "OVERLAPPING_FIELDS_CAN_BE_MERGED")]
    [InlineData(typeof(PossibleFragmentSpreadsError), "POSSIBLE_FRAGMENT_SPREADS")]
    [InlineData(typeof(ProvidedNonNullArgumentsError), "PROVIDED_NON_NULL_ARGUMENTS")]
    [InlineData(typeof(ScalarLeafsError), "SCALAR_LEAFS")]
    [InlineData(typeof(SingleRootFieldSubscriptionsError), "SINGLE_ROOT_FIELD_SUBSCRIPTIONS")]
    [InlineData(typeof(UniqueArgumentNamesError), "UNIQUE_ARGUMENT_NAMES")]
    [InlineData(typeof(UniqueDirectivesPerLocationError), "UNIQUE_DIRECTIVES_PER_LOCATION")]
    [InlineData(typeof(UniqueFragmentNamesError), "UNIQUE_FRAGMENT_NAMES")]
    [InlineData(typeof(UniqueInputFieldNamesError), "UNIQUE_INPUT_FIELD_NAMES")]
    [InlineData(typeof(UniqueOperationNamesError), "UNIQUE_OPERATION_NAMES")]
    [InlineData(typeof(UniqueVariableNamesError), "UNIQUE_VARIABLE_NAMES")]
    [InlineData(typeof(VariablesAreInputTypesError), "VARIABLES_ARE_INPUT_TYPES")]
    [InlineData(typeof(VariablesInAllowedPositionError), "VARIABLES_IN_ALLOWED_POSITION")]
    public void Verify_GetValidationErrorCode(Type type, string code)
        => ValidationError.GetValidationErrorCode(type).ShouldBe(code);

    [Theory]
    [InlineData(ArgumentsOfCorrectTypeError.NUMBER, "5.6.1")]
    [InlineData(DefaultValuesOfCorrectTypeError.NUMBER, "5.6.1")]
    [InlineData(FieldsOnCorrectTypeError.NUMBER, "5.3.1")]
    [InlineData(FragmentsOnCompositeTypesError.NUMBER, "5.5.1.3")]
    [InlineData(KnownArgumentNamesError.NUMBER, "5.4.1")]
    [InlineData(KnownDirectivesError.NUMBER, "5.7.1")]
    [InlineData(KnownFragmentNamesError.NUMBER, "5.5.2.1")]
    [InlineData(KnownTypeNamesError.NUMBER, "5.5.1.2")]
    [InlineData(LoneAnonymousOperationError.NUMBER, "5.2.2.1")]
    [InlineData(NoFragmentCyclesError.NUMBER, "5.5.2.2")]
    [InlineData(NoUndefinedVariablesError.NUMBER, "5.8.3")]
    [InlineData(NoUnusedFragmentsError.NUMBER, "5.5.1.4")]
    [InlineData(NoUnusedVariablesError.NUMBER, "5.8.4")]
    [InlineData(OverlappingFieldsCanBeMergedError.NUMBER, "5.3.2")]
    [InlineData(PossibleFragmentSpreadsError.NUMBER, "5.5.2.3")]
    [InlineData(ProvidedNonNullArgumentsError.NUMBER, "5.4.2.1")]
    [InlineData(ScalarLeafsError.NUMBER, "5.3.3")]
    [InlineData(SingleRootFieldSubscriptionsError.NUMBER, "5.2.3.1")]
    [InlineData(UniqueArgumentNamesError.NUMBER, "5.4.2")]
    [InlineData(UniqueDirectivesPerLocationError.NUMBER, "5.7.3")]
    [InlineData(UniqueFragmentNamesError.NUMBER, "5.5.1.1")]
    [InlineData(UniqueInputFieldNamesError.NUMBER, "5.6.3")]
    [InlineData(UniqueOperationNamesError.NUMBER, "5.2.1.1")]
    [InlineData(UniqueVariableNamesError.NUMBER, "5.8.1")]
    [InlineData(VariablesAreInputTypesError.NUMBER, "5.8.2")]
    [InlineData(VariablesInAllowedPositionError.NUMBER, "5.8.5")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "xUnit1025:InlineData should be unique within the Theory it belongs to")]
    public void Verify_Validation_Number(string actualCode, string expectedCode)
        => actualCode.ShouldBe(expectedCode);
}
