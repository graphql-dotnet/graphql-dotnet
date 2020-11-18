using System;
using System.Collections.Generic;
using System.Text;
using GraphQL.Language.AST;
using GraphQL.Types;

namespace GraphQL.Compilation
{
    public class CompiledField
    {
        public FieldType Definition { get; }
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
