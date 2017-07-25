using System.Collections.Generic;

namespace GraphQL.Authorization
{
    public interface IAuthorizationPolicy
    {
        IEnumerable<IAuthorizationRequirement> Requirements { get; }
    }

    public class AuthorizationPolicy : IAuthorizationPolicy
    {
        private readonly List<IAuthorizationRequirement> _requirements;

        public AuthorizationPolicy(IEnumerable<IAuthorizationRequirement> requirements)
        {
            _requirements = new List<IAuthorizationRequirement>(
                requirements ?? new List<IAuthorizationRequirement>());
        }

        public IEnumerable<IAuthorizationRequirement> Requirements => _requirements;
    }
}