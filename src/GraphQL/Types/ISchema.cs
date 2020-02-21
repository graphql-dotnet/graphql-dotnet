using GraphQL.Conversion;
using GraphQL.Introspection;
using System;
using System.Collections.Generic;

namespace GraphQL.Types
{
    public interface ISchema
    {
        bool Initialized { get; }

        void Initialize();

        INameConverter NameConverter { get; set; }

        IObjectGraphType Query { get; set; }

        IObjectGraphType Mutation { get; set; }

        IObjectGraphType Subscription { get; set; }

        IEnumerable<DirectiveGraphType> Directives { get; set; }

        IEnumerable<IGraphType> AllTypes { get; }

        IGraphType FindType(string name);

        DirectiveGraphType FindDirective(string name);

        IEnumerable<Type> AdditionalTypes { get; }

        void RegisterType(IGraphType type);

        void RegisterTypes(params IGraphType[] types);

        void RegisterTypes(params Type[] types);

        void RegisterType<T>() where T : IGraphType;

        void RegisterDirective(DirectiveGraphType directive);

        void RegisterDirectives(params DirectiveGraphType[] directives);

        void RegisterValueConverter(IAstFromValueConverter converter);

        IAstFromValueConverter FindValueConverter(object value, IGraphType type);

        /// <summary>
        /// Provides the ability to filter the schema upon introspection to hide types.
        /// </summary>
        ISchemaFilter Filter { get; set; }
    }
}
