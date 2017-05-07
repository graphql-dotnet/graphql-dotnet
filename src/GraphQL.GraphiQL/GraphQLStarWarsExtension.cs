using System;
using Autofac;
using GraphQL.Http;
using GraphQL.StarWars;
using GraphQL.StarWars.Types;
using GraphQL.Types;

namespace GraphQL.GraphiQL
{
    public static class GraphQLStarWarsExtension
    {
        public static void RegisterGraphQLTypes(this ContainerBuilder builder)
        {
            builder.RegisterInstance(new DocumentExecuter()).As<IDocumentExecuter>();
            builder.RegisterInstance(new DocumentWriter()).As<IDocumentWriter>();
            builder.RegisterInstance(new StarWarsData()).As<StarWarsData>();

            builder.RegisterType<StarWarsQuery>().AsSelf();
            builder.RegisterType<HumanType>().AsSelf();
            builder.RegisterType<DroidType>().AsSelf();
            builder.RegisterType<EpisodeEnum>().AsSelf();

            builder.RegisterType<CharacterInterface>().AsSelf();
            builder.RegisterType<StarWarsQuery>().AsSelf();
            builder.RegisterType<StarWarsSchema>().AsSelf();
            builder.Register<Func<Type, GraphType>>(c =>
            {
                var context = c.Resolve<IComponentContext>();
                return t => {
                                var res = context.Resolve(t);
                                return (GraphType)res;
                };
            });

        }
    }
}