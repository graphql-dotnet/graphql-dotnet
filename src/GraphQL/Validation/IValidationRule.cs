using System.Threading.Tasks;

namespace GraphQL.Validation
{
    public interface IValidationRule
    {
        Task<INodeVisitor> ValidateAsync(ValidationContext context);
    }
}
