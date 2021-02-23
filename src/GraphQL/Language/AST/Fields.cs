using System;
using System.Collections.Generic;
using GraphQL.Execution;
using GraphQL.Types;

namespace GraphQL.Language.AST
{
    /// <summary>
    /// Represents a list of field nodes within a document.
    /// </summary>
    public class Fields : Dictionary<string, Field>
    {
        /// <summary>
        /// Before execution, the selection set is converted to a grouped field set by calling CollectFields().
        /// Each entry in the grouped field set is a list of fields that share a response key (the alias if defined,
        /// otherwise the field name). This ensures all fields with the same response key included via referenced
        /// fragments are executed at the same time.
        /// <br/><br/>
        /// <see href="http://spec.graphql.org/June2018/#sec-Field-Collection"/> and <see href="http://spec.graphql.org/June2018/#CollectFields()"/>
        /// </summary>
        public Fields CollectFrom(ExecutionContext context, IGraphType specificType, SelectionSet selectionSet)
        {
            List<string> visitedFragmentNames = null;
            CollectFields(context, specificType.GetNamedType(), selectionSet, context.ExecutionStrategy ?? ParallelExecutionStrategy.Instance, ref visitedFragmentNames);
            return this;
        }

        private void CollectFields(ExecutionContext context, IGraphType specificType, SelectionSet selectionSet, IExecutionStrategy strategy, ref List<string> visitedFragmentNames) //TODO: can be completely eliminated? see Fields.Add
        {
            if (selectionSet != null)
            {
                foreach (var selection in selectionSet.SelectionsList)
                {
                    if (selection is Field field)
                    {
                        if (strategy.ShouldIncludeNode(context, field))
                            Add(field);
                    }
                    else if (selection is FragmentSpread spread)
                    {
                        if (visitedFragmentNames?.Contains(spread.Name) != true && strategy.ShouldIncludeNode(context, spread))
                        {
                            (visitedFragmentNames ??= new List<string>()).Add(spread.Name);

                            var fragment = context.Fragments.FindDefinition(spread.Name);
                            if (fragment != null && strategy.ShouldIncludeNode(context, fragment) && DoesFragmentConditionMatch(context, fragment.Type.Name, specificType))
                                CollectFields(context, specificType, fragment.SelectionSet, strategy, ref visitedFragmentNames);
                        }
                    }
                    else if (selection is InlineFragment inline)
                    {
                        // inline.Type may be null
                        // See [2.8.2] Inline Fragments: If the TypeCondition is omitted, an inline fragment is considered to be of the same type as the enclosing context.
                        if (strategy.ShouldIncludeNode(context, inline) && DoesFragmentConditionMatch(context, inline.Type?.Name ?? specificType.Name, specificType))
                            CollectFields(context, specificType, inline.SelectionSet, strategy, ref visitedFragmentNames);
                    }
                }
            }
        }

        /// <summary>
        /// This method calculates the criterion for matching fragment definition (spread or inline) to a given graph type.
        /// This criterion determines the need to fill the resulting selection set with fields from such a fragment.
        /// <br/><br/>
        /// <see href="http://spec.graphql.org/June2018/#DoesFragmentTypeApply()"/>
        /// </summary>
        private static bool DoesFragmentConditionMatch(ExecutionContext context, string fragmentName, IGraphType type /* should be named type*/)
        {
            if (fragmentName == null)
                throw new ArgumentNullException(nameof(fragmentName));

            var conditionalType = context.Schema.AllTypes[fragmentName];

            if (conditionalType == null)
            {
                return false;
            }

            if (conditionalType.Equals(type))
            {
                return true;
            }

            if (conditionalType is IAbstractGraphType abstractType)
            {
                return abstractType.IsPossibleType(type);
            }

            return false;
        }

        /// <summary>
        /// Adds a field node to the list.
        /// </summary>
        public void Add(Field field)
        {
            string name = field.Alias ?? field.Name;

            if (TryGetValue(name, out Field original))
            {
                // Sets a new field selection node with the child field selection nodes merged with another field's child field selection nodes.
                this[name] = new Field(original.AliasNode, original.NameNode)
                {
                    Arguments = original.Arguments,
                    SelectionSet = original.SelectionSet.Merge(field.SelectionSet),
                    Directives = original.Directives,
                    SourceLocation = original.SourceLocation,
                };
            }
            else
            {
                this[name] = field;
            }
        }
    }
}

