using System.Collections.Generic;

namespace GraphQL.Authorization
{
    public class AuthorizationResult
    {
        public bool Succeeded { get; private set; }

        public IEnumerable<string> Errors { get; private set; }

        public static AuthorizationResult Success()
        {
            return new AuthorizationResult {Succeeded = true};
        }

        public static AuthorizationResult Fail(IEnumerable<string> errors)
        {
            return new AuthorizationResult
            {
                Succeeded = false,
                Errors = errors
            };
        }
    }
}