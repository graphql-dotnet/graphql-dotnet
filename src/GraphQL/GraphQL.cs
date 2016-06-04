using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System.Linq.Expressions;
using GraphQL.Types;

namespace GraphQL2
{
    public interface IGraphQLOutputType
    {
    }

    public abstract class GraphQLType : IGraphQLOutputType
    {
    }

    public abstract class GraphQLScalarType : GraphQLType
    {
    }

    public class GraphQLScalarType<TType> : GraphQLScalarType
    {
        public GraphQLScalarType(string name, Func<object, TType> coerce)
        {
            Name = name;
            Coerce = coerce;
        }

        public string Name { get; private set; }

        public Func<object, TType> Coerce { get; private set; }
    }

    public class GraphQLEnumType : GraphQLScalarType
    {
        public GraphQLEnumType(Action<IGraphQLEnumBuilder> values)
        {
        }
    }

    public static class GraphQLScalarTypes
    {
        public static GraphQLScalarType<string> GraphQLString = new GraphQLScalarType<string>(
            name: "String",
            coerce: value =>
            {
                return value != null ? value.ToString().Trim('"') : null;
            });
    }

    public class GraphQLFieldDefinition
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public Func<ResolveFieldContext, object> Resolve { get; set; }
    }

    public interface IGraphQLEnumBuilder
    {
        string Name { get; set; }
        string Description { get; set; }
        string DeprecationReason { get; set; }
        void Value<T>(string name, T value, string description = null);
    }

    public interface IGraphQLFieldResolver<T, K>
    {
    }

    public interface IGraphQLFieldResolver<T> : IGraphQLFieldResolver<T, T>
    {
    }

    public interface IGraphQLFieldBuilder
    {
        string Name { get; set; }
        string Description { get; set; }
        string DeprecationReason { get; set; }

        void Field<TResolve>(
            string name,
            IGraphQLOutputType type = null,
            string description = null,
            Func<ResolveFieldContext, TResolve> resolve = null);
    }

    public interface IGraphQLFieldBuilder<T> : IGraphQLFieldBuilder
    {
        void Field<TProperty>(
            string name,
            IGraphQLOutputType type,
            Expression<Func<T, TProperty>> property);

        void Field(
            string name,
            IGraphQLOutputType type = null,
            string description = null,
            IGraphQLFieldResolver<T> resolver = null);

        void Field<TResolve>(
            string name,
            IGraphQLOutputType type = null,
            string description = null,
            IGraphQLFieldResolver<T, TResolve> resolver = null);

        void Field<TResolve>(
            string name,
            IGraphQLOutputType type = null,
            string description = null,
            Func<T, ResolveFieldContext, TResolve> resolve = null);

        void Field<TResolve>(
            string name,
            IGraphQLOutputType type = null,
            string description = null,
            Func<T, TResolve> resolve = null);
    }

    public class GraphQLObjectType : GraphQLType
    {
        private readonly List<GraphQLFieldDefinition> _fields = new List<GraphQLFieldDefinition>();

        public GraphQLObjectType(string name, Action<IGraphQLFieldBuilder> fields, string description = null)
        {
            Name = name;
            //            var fieldBuilder = new FieldBuilder();
            //            fields(fieldBuilder);
            //            Fields = fieldBuilder.Build();

            Description = description;
        }

        protected GraphQLObjectType(string name, string description)
        {
            Name = name;
            Description = description;
        }

        public string Name { get; protected set; }
        public string Description { get; protected set; }

        public IEnumerable<GraphQLFieldDefinition> Fields
        {
            get { return _fields; }
            protected set
            {
                _fields.Clear();
                _fields.AddRange(value);
            }
        }
    }

    public class GraphQLObjectType<TModel> : GraphQLObjectType
    {
        public GraphQLObjectType(
            string name,
            Action<IGraphQLFieldBuilder<TModel>> fields,
            IEnumerable<GraphQLInterfaceType> interfaces = null,
            string description = null)
            : base(name, description)
        {
        }
    }

    public class GraphQLList : GraphQLType
    {
        public GraphQLList(GraphQLType ofType)
        {
            OfType = ofType;
        }

        public GraphQLType OfType { get; private set; }
    }

    public class GraphQLNonNull : GraphQLType
    {
        public GraphQLNonNull(GraphQLType ofType)
        {
            OfType = ofType;
        }

        public GraphQLType OfType { get; private set; }
    }

    public class GraphQLInterfaceType : GraphQLType
    {
    }

    public class GraphQLInterfaceType<TModel> : GraphQLInterfaceType
    {
        public GraphQLInterfaceType(
            string name,
            Action<IGraphQLFieldBuilder<TModel>> fields,
            string description = null)
        {
        }
    }

    public class GraphQLReferenceType : GraphQLType
    {
        public GraphQLReferenceType(string typeName)
        {
            TypeName = typeName;
        }

        public string TypeName { get; set; }
    }

    public interface ICharacter
    {
        string Name { get; set; }
    }

    public class Human : ICharacter
    {
        public string Name { get; set; }
    }

    public class Droid : ICharacter
    {
        public string Name { get; set; }
    }

    public interface IStarWarsData
    {
        Droid DroidWithId(string id);

        Human HumanWithId(string id);

        IEnumerable<ICharacter> FriendsFor(string id);

        Task<IEnumerable<ICharacter>> FriendsForAsync(string id);
    }

    public class StarWarsSchema
    {
        public GraphQLObjectType Query { get; set; }
        public GraphQLObjectType Mutation { get; set; }

        public StarWarsSchema(IStarWarsData data)
        {
            var episodeEnum = new GraphQLEnumType(_ =>
            {
                _.Name = "Episode";
                _.Description = "One of the films in the Star Wars Trilogy.";
                _.Value("NEWHOPE", 4, "Released in 1977.");
                _.Value("EMPIRE", 5, "Released in 1980.");
                _.Value("JEDI", 6, "Released in 1983.");
            });

            var characterType = new GraphQLInterfaceType<ICharacter>(
                name: "Character",
                fields: _ =>
                {
                    _.Field("id", GraphQLScalarTypes.GraphQLString);
                    _.Field("name", GraphQLScalarTypes.GraphQLString);
                    _.Field(
                        name: "friends",
                        type: new GraphQLList(new GraphQLReferenceType("Character")));
                    _.Field("appearsIn", new GraphQLList(episodeEnum));
                });

            var droidType = new GraphQLObjectType<Droid>(
                name: "Droid",
                fields: _ =>
                {
                    _.Field(
                        name: "name",
                        type: GraphQLScalarTypes.GraphQLString,
                        property: x => x.Name);
                });

            var humanType = new GraphQLObjectType<Human>(
                name: "Human",
                fields: _ =>
                {
                    _.Field(
                        name: "name",
                        type: GraphQLScalarTypes.GraphQLString,
                        resolve: x => x.Name);

                    _.Field(
                        name: "friends",
                        type: new GraphQLList(characterType),
                        resolve: (human, context) => data.FriendsForAsync(context.Argument<string>("id")));
                },
                interfaces: new[] { characterType });

            var queryRoot = new GraphQLObjectType(
                name: "Root",
                fields: _ =>
                {
                    _.Field(
                        name: "hero",
                        type: droidType,
                        resolve: context => data.DroidWithId(context.Argument<string>("id")));

                    _.Field(
                        name: "human",
                        type: humanType,
                        resolve: context => data.HumanWithId(context.Argument<string>("id")));
                });

            Query = queryRoot;
        }
    }
}
