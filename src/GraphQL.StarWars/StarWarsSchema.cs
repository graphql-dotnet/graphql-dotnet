using System;
using System.Linq;
using GraphQL.Types;

namespace GraphQL.StarWars
{
    public class StarWarsSchema : Schema
    {
        public StarWarsSchema(IServiceProvider resolver)
            : base(resolver)
        {
            Query = resolver.Resolve<StarWarsQuery>();
            Mutation = resolver.Resolve<StarWarsMutation>();
        }
    }

    public static class ServiceProviderExtensions
    {
        public static T Resolve<T>(this IServiceProvider services)
        {
            return (T)services.GetService(typeof(T));
        }
    }
}
