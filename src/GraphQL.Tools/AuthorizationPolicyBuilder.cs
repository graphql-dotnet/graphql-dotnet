using System.Collections.Generic;

namespace GraphQL.Tools
{
    public class AuthorizationPolicyBuilder
    {
        private readonly List<IAuthorizationRequirement> _requirements;

        public AuthorizationPolicyBuilder()
        {
            _requirements = new List<IAuthorizationRequirement>();
        }

        public AuthorizationPolicy Build()
        {
            var policy = new AuthorizationPolicy(_requirements);
            return policy;
        }

        public AuthorizationPolicyBuilder RequireClaim(string claimType)
        {
            var requirement = new ClaimAuthorizationRequirement(claimType);
            _requirements.Add(requirement);
            return this;
        }

        public AuthorizationPolicyBuilder RequireClaim(string claimType, params string[] requiredValues)
        {
            var requirement = new ClaimAuthorizationRequirement(claimType, requiredValues);
            _requirements.Add(requirement);
            return this;
        }

        public AuthorizationPolicyBuilder AddRequirement(IAuthorizationRequirement requirement)
        {
            _requirements.Add(requirement);
            return this;
        }
    }
}