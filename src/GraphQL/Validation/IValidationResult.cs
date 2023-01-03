using GraphQL.Execution;
using GraphQLParser.AST;

namespace GraphQL.Validation
{
    /// <summary>
    /// Contains a list of the validation errors found after validating a document against a set of validation rules.
    /// If the document passes validation, this will also contain the set of parsed variables and argument values.
    /// </summary>
    public interface IValidationResult
    {
        /// <summary>
        /// Returns <see langword="true"/> if no errors were found during the validation of a document.
        /// </summary>
        bool IsValid { get; }

        /// <summary>
        /// Returns a list of the errors found during validation of a document.
        /// </summary>
        ExecutionErrors Errors { get; }

        /// <summary>
        /// Returns the set of variables parsed from the inputs.
        /// If the document did not pass validation, this value will be <see langword="null"/>.
        /// If the document passed validation but contained no variables, this value will be <see cref="Variables.None"/>.
        /// </summary>
        Variables? Variables { get; }

        /// <summary>
        /// Returns a dictionary of fields with supplied arguments.
        /// If the document did not pass validation, or if no fields having arguments were found, this value will be <see langword="null"/>.
        /// </summary>
        IReadOnlyDictionary<GraphQLField, IDictionary<string, ArgumentValue>>? ArgumentValues { get; }

        /// <summary>
        /// Returns a dictionary of directives with supplied arguments.
        /// If the document did not pass validation, or if no directives having arguments were found, this value will be <see langword="null"/>.
        /// </summary>
        IReadOnlyDictionary<GraphQLField, IDictionary<string, DirectiveInfo>>? DirectiveValues { get; }
    }
}
