using System.Threading.Tasks;

namespace GraphQL.Validation
{
    /// <summary>
    /// Represents a validation rule for a document.
    /// </summary>
    public interface IValidationRule
    {
        /// <summary>
        /// Validates a document against this validation rule. Validation failures are added
        /// to a list stored within <see cref="ValidationContext.Errors"/>.
        /// </summary>
        Task<INodeVisitor> ValidateAsync(ValidationContext context);
    }
}
