using System;
using System.Reflection;
using GraphQL.Resolvers;
using GraphQL.Types;

namespace GraphQL.Utilities
{
    public class TypeConfig : MetadataProvider
    {
        public TypeConfig(string name)
        {
            Name = name;
        }

        public Type Type { get; set; }

        public string Name { get; }
        public string Description { get; set; }
        public Func<object, IObjectGraphType> ResolveType { get; set; }
        public Func<object, bool> IsTypeOfFunc { get; set; }

        public void IsTypeOf<T>()
        {
            IsTypeOfFunc = obj => obj?.GetType() == typeof(T);
        }

        public MethodInfo MethodForField(string field)
        {
            return Type?.MethodForField(field);
        }

        public IFieldResolver ResolverFor(string field, IDependencyResolver dependencyResolver)
        {
            if (Type == null)
            {
                return null;
            }

            var method = MethodForField(field);

            var resolverType = typeof(MethodModelBinderResolver<>).MakeGenericType(Type);

            var args = new object[] { method, dependencyResolver };
            var resolver = (IFieldResolver) Activator.CreateInstance(resolverType, args);

            return resolver;
        }
    }
}