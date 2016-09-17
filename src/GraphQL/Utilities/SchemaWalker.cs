using GraphQL.Types;
using GraphQL.Introspection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GraphQL.Utilities
{
    public class SchemaWalker
    {
        private readonly Dictionary<string, IReferenceTarget> _references = 
            new Dictionary<string, IReferenceTarget>();


        public Dictionary<string, IGraphType> Walk(Schema schema)
        {
            var types = new Dictionary<string, IGraphType>(StringComparer.OrdinalIgnoreCase);

            AddType(types, schema.Query);
            AddType(types, schema.Mutation);
            AddType(types, schema.Subscription);
            AddType(types, new __Schema());

            // replace any GraphQLReferenceTypes with real types
            _references.Apply(x =>
            {
                x.Value.Type = types[x.Key];
            });

            return types;
        }


        private void AddType(Dictionary<string, IGraphType> types, IGraphType type)
        {
            if (type == null)
            {
                return;
            }

            if (type is OuterGraphType)
            {
                var outerType = type as OuterGraphType;
                type = outerType.Type;

                if (MaybeAddReference(type, outerType)) {
                    return;
                };               
            }

            if (type is ReferenceGraphType || IsCoreType(type))
            {
                return;
            }

            Invariant.Check(!types.ContainsKey(type.Name),
                $"Schema must contain unique named types but contains multiple types named {type.Name}"
            );


            types[type.Name] = type;

            if (type is IComplexGraphType)
            {
                var complexType = (IComplexGraphType)type;

                complexType.Fields.Apply(x =>
                {
                    x.Arguments?.Apply(a => AddType(types, a.Type));

                    if (!MaybeAddReference(x.Type, x))
                    {
                        AddType(types, x.Type);
                    }
                });
            }

            if (type is IObjectGraphType)
            {
                var objType = (IObjectGraphType)type;

                objType.Interfaces.Apply(x =>
                {
                    x.AddPossibleType(objType);
                    AddType(types, x);
                });
            }

            if (type is UnionGraphType)
            {
                var union = (UnionGraphType)type;
                union.Types.Apply(x =>
                {
                    AddType(types, x);
                });
            }
        }

        private bool MaybeAddReference(IGraphType type, IReferenceTarget target)
        {
            if (type is ReferenceGraphType)
            {
                _references.Add(((ReferenceGraphType)type).TypeName, target);
                return true;
            }

            return false;
        }

        private static bool IsCoreType(IGraphType type)
        {
            IGraphType[] scalars = {
                ScalarGraphTypes.Id,
                ScalarGraphTypes.Boolean,
                ScalarGraphTypes.Float,
                ScalarGraphTypes.Int,
                ScalarGraphTypes.String
            };

            IGraphType[] introspection = {
            };

            return scalars.Any(s => s.Name == type.Name) || introspection.Any(i => i.Equals(type));
        }
    }
}
