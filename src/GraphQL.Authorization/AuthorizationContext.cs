using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;

namespace GraphQL.Authorization
{
    public class AuthorizationContext
    {
        private readonly List<string> _errors = new List<string>();

        public ClaimsPrincipal User { get; set; }

        public object UserContext { get; set; }

        public IEnumerable<string> Errors => _errors;

        public bool HasErrors => _errors.Any<string>();

        public void ReportError(string error)
        {
            _errors.Add(error);
        }
    }
}