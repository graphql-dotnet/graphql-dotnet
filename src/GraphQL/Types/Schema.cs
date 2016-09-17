using System;
using System.Collections.Generic;
using System.Linq;

namespace GraphQL.Types
{   

    public interface ISchema : IDisposable
    {
        IObjectGraphType Query { get; set; }

        IObjectGraphType Mutation { get; set; }

        IObjectGraphType Subscription { get; set; }

        IEnumerable<DirectiveGraphType> Directives { get; set; }

        void RegisterType(IGraphType type);
        void RegisterTypes(params IGraphType[] types);
    }

    public class Schema : ISchema
    {
        private readonly List<IGraphType> _additionalTypes;
        private readonly List<DirectiveGraphType> _directives;

        public Schema()
        {
            _additionalTypes = new List<IGraphType>();
            _directives = new List<DirectiveGraphType>
            {
                DirectiveGraphType.Include,
                DirectiveGraphType.Skip,
                DirectiveGraphType.Deprecated
            };
        }

        public IObjectGraphType Query { get; set; }

        public IObjectGraphType Mutation { get; set; }

        public IObjectGraphType Subscription { get; set; }

        public IEnumerable<DirectiveGraphType> Directives
        {
            get
            {
                return _directives;
            }
            set
            {
                if (value == null)
                {
                    return;
                }

                _directives.Clear();
                _directives.Fill(value);
            }
        }

        public void RegisterType(IGraphType type)
        {
            _additionalTypes.Add(type);
        }

        public void RegisterTypes(params IGraphType[] types)
        {
            if (types == null)
            {
                throw new ArgumentNullException(nameof(types));
            }

            types.Apply(RegisterType);
        }

        public void Dispose()
        {
            Query = null;
            Mutation = null;
            Subscription = null;
        }

    }
}
