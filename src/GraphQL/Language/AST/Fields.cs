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

