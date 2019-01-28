using GraphQL.Language.AST;
using GraphQL.Types;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GraphQL.Validation.Rules
{
    public class OverlappingFieldsCanBeMerged : IValidationRule
    {
        public INodeVisitor Validate(ValidationContext context)
        {

            return new EnterLeaveListener(config =>
            {
                config.Match<SelectionSet>(selectionSet =>
                {
                });
            });
        }

        // Given a reference to a fragment, return the represented collection of fields
        // as well as a list of nested fragment names referenced via fragment spreads.
        public void GetReferencedFieldsAndFragmentNames(
            ValidationContext context,
            ArrayList cachedFieldsAndFragmentNames,
            FragmentDefinition fragmentDefinition)
        {
            // Short-circuit building a type from the node if possible.

        }


        public void CollectionFieldsAndFragments(
            ValidationContext context,
            IGraphType parentType,
            SelectionSet selectionSet,
            Dictionary<string, List<FieldDefPair>> nodeAndDefs,
            Dictionary<string, bool> fragments
            )
        {
            for (int i = 0; i < selectionSet.Selections.Count; i++)
            {
                var selection = selectionSet.Selections[i];

                if (selection is Field field)
                {
                    var fieldName = field.Name;
                    Field fieldDef = null;
                    if (isObjectType(parentType) || isInterfaceType(parentType))
                    {
                        fieldDef = ((NamedType)parentType).Children.OfType<Field>().FirstOrDefault(x => x.Name == fieldName);
                    }

                    var responseName = field.Alias ?? fieldName;

                    if(nodeAndDefs[responseName] == null)
                    {
                        nodeAndDefs[responseName] = new List<FieldDefPair>();
                    }

                    nodeAndDefs[responseName].Add(new FieldDefPair
                    {
                        ParentType = parentType,
                        Field = field,
                        FieldDef = fieldDef
                    });

                } else if (selection is FragmentSpread fragmentSpread)
                {
                    fragments[fragmentSpread.Name] = true;

                } else if (selection is InlineFragment inlineFragment)
                {
                    var typeCondition = inlineFragment.Type;
                    var inlineFragmentType =
                        typeCondition == null
                            ? typeCondition.GraphTypeFromType(context.Schema)
                            : parentType;

                    CollectionFieldsAndFragments(
                        context,
                        inlineFragmentType,
                        inlineFragment.SelectionSet,
                        nodeAndDefs,
                        fragments);
                }
            }
        }

        public class FieldDefPair
        {
            public IGraphType ParentType { get; set; }
            public ISelection Field { get; set; }
            public Field FieldDef { get; set; }
        }

        private bool isInterfaceType(IGraphType parentType)
        {
            throw new NotImplementedException();
        }

        private bool isObjectType(IGraphType parentType)
        {
            throw new NotImplementedException();
        }

        // Given a series of Conflicts which occurred between two sub-fields,
        // generate a single Conflict.
        public Conflict SubfieldConflicts(
            List<Conflict> conflicts,
            string responseName,
            Field node1,
            Field node2)
        {
            if (conflicts.Count > 0)
            {
                var conflictReasons = new List<ConflictReason>();
                var conflictFieldsLeft = new List<Field>();
                var conflictFieldsRight = new List<Field>();

                return new Conflict
                {
                    Reason = new ConflictReason
                    {
                        Name = responseName,
                        Message = new Message
                        {
                            Msgs = conflicts.Select(c => c.Reason).ToList()
                        }
                    },
                    FieldsLeft = conflicts.Aggregate(new List<Field>() { node1 }, (allfields, conflict) =>
                    {
                        allfields.AddRange(conflict.FieldsLeft);
                        return allfields;
                    }),
                    FieldsRight = conflicts.Aggregate(new List<Field> { node2 }, (allfields, conflict) => {
                        allfields.AddRange(conflict.FieldsRight);
                        return allfields;
                    })
                };
            }

            return null;
        }

        public class Conflict
        {
            public ConflictReason Reason { get; set; }
            public List<Field> FieldsLeft { get; set; }
            public List<Field> FieldsRight { get; set; }
        }

        public class ConflictReason
        {
            public string Name { get; set; }
            public Message Message { get; set; }
        }

        public class Message
        {
            public ConflictReason Msg { get; set; }
            public List<ConflictReason> Msgs { get; set; }
        }

        public class PairSet
        {
            private ObjMap<ObjMap<bool>> _data;

            public PairSet()
            {
                _data = new ObjMap<ObjMap<bool>>();
            }

            public bool Has(string a, string b, bool areMutuallyExclusive)
            {
                var first = _data[a];
                var result = first?[b];

                if (result == null)
                {
                    return false;
                }

                if(areMutuallyExclusive == false)
                {
                    return result == false;
                }

                return false;
            }

            public void Add(string a, string b, bool areMutuallyExclusive)
            {
                PairSetAdd(a, b, areMutuallyExclusive);
                PairSetAdd(b, a, areMutuallyExclusive);
            }

            private void PairSetAdd(string a, string b, bool areMutuallyExclusive)
            {
                var map = _data[a];
                if(map == null)
                {
                    map = new ObjMap<bool>();
                    _data[a] = map;
                }
                map[b] = areMutuallyExclusive;
            }
        }

        public class ObjMap<T> : Dictionary<string, T>
        {
        }
    }
}
