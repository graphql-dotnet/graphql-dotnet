using System;
using GraphQL.Language.AST;
using GraphQL.Types;

namespace GraphQL.Compilation
{
    public class CompiledField
    {
        /// <summary>
        /// Returns the graph's field type of this node.
        /// </summary>
        public FieldType Definition { get; }

        /// <summary>
        /// Returns the AST field of this node.
        /// </summary>
        public Field Field { get; }

        public Func<object, bool, CompiledNode> Resolve { get; }

        public CompiledField(FieldType definition, Field field, Func<object, bool, CompiledNode> resolve = null)
        {
            Definition = definition;
            Field = field;
            Resolve = resolve;
        }
    }
}
