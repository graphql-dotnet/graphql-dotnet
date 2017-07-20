using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using GraphQL.Builders;
using GraphQL.Types;

namespace GraphQL.Tools
{
    public static class MetadataExtensions
    {
        public static readonly string PolicyKey = "Policies";

        public static bool RequiresAuthorization(this IProvideMetadata type)
        {
            return GetPolicies(type).Any();
        }

        public static Task<AuthorizationResult> Authorize(this IProvideMetadata type,
            ClaimsPrincipal principal,
            object userContext,
            IAuthorizationEvaluator evaluator)
        {
            var list = GetPolicies(type);
            return evaluator.Evaluate(principal, userContext, list);
        }

        public static void AuthorizeWith(this IProvideMetadata type, string policy)
        {
            var list = GetPolicies(type);
            list.Fill(policy);
            type.Metadata[PolicyKey] = list;
        }

        public static FieldBuilder<TSourceType, TReturnType> AuthorizeWith<TSourceType, TReturnType>(
            this FieldBuilder<TSourceType, TReturnType> builder, string policy)
        {
            builder.FieldType.AuthorizeWith(policy);
            return builder;
        }

        public static List<string> GetPolicies(this IProvideMetadata type)
        {
            return type.GetMetadata(PolicyKey, new List<string>());
        }
    }
}
