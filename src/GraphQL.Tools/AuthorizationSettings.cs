using System;
using System.Collections.Generic;

namespace GraphQL.Tools
{
    public class AuthorizationSettings
    {
        private readonly IDictionary<string, IAuthorizationPolicy> _policies;

        public AuthorizationSettings()
        {
            _policies = new Dictionary<string, IAuthorizationPolicy>(StringComparer.OrdinalIgnoreCase);
        }

        public IEnumerable<IAuthorizationPolicy> Policies => _policies.Values;

        public IEnumerable<IAuthorizationPolicy> GetPolicies(IEnumerable<string> policies)
        {
            var found = new List<IAuthorizationPolicy>();

            policies.Apply(name =>
            {
                if (_policies.ContainsKey(name))
                {
                    found.Add(_policies[name]);
                }
            });

            return found;
        }

        public IAuthorizationPolicy GetPolicy(string name)
        {
            return _policies.ContainsKey(name) ? _policies[name] : null;
        }

        public void AddPolicy(string name, IAuthorizationPolicy policy)
        {
            _policies[name] = policy;
        }

        public void AddPolicy(string name, Action<AuthorizationPolicyBuilder> configure)
        {
            var builder = new AuthorizationPolicyBuilder();
            configure(builder);

            var policy = builder.Build();
            _policies[name] = policy;
        }
    }
}