using GraphQL.Language.AST;
using GraphQL.Validation;

namespace GraphQL.Authorization
{
    public class AuthorizationValidationRule : IValidationRule
    {
        private readonly IAuthorizationEvaluator _evaluator;

        public AuthorizationValidationRule(IAuthorizationEvaluator evaluator)
        {
            _evaluator = evaluator;
        }

        public INodeVisitor Validate(ValidationContext context)
        {
            var userContext = context.UserContext as IProvideClaimsPrincipal;

            return new EnterLeaveListener(_ =>
            {
                // this could leak info about hidden fields in error messages
                // it would be better to implement a filter on the Schema so it
                // acts as if they just don't exist vs. an auth denied error
                // - filtering the Schema is not currently supported
                _.Match<Field>(fieldAst =>
                {
                    var fieldDef = context.TypeInfo.GetFieldDef();

                    if (!fieldDef.RequiresAuthorization()) return;

                    var result = fieldDef
                        .Authorize(userContext?.Principal, context.UserContext, _evaluator)
                        .GetAwaiter()
                        .GetResult();

                    if (result.Succeeded) return;

                    var errors = string.Join("\n", result.Errors);

                    context.ReportError(new ValidationError(
                        context.OriginalQuery,
                        "authorization",
                        $"You are not authorized to run this query.\n{errors}",
                        fieldAst));
                });
            });
        }
    }
}
