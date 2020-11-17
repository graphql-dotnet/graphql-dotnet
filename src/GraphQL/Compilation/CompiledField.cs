using System;
using System.Collections.Generic;
using System.Text;
using GraphQL.Language.AST;
using GraphQL.Types;

namespace GraphQL.Compilation
{
    public class CompiledField
    {
        public FieldType Definition { get; set; }
        public Field Field { get; set; }

        internal Func<object, bool, CompiledNode> Resolve { get; set; }
        //{
        //    var GraphType = Definition.ResolvedType;
        //    var objectGraphType = GraphType as IObjectGraphType;

        //    if (GraphType is IAbstractGraphType abstractGraphType && isResultSet)
        //        objectGraphType = abstractGraphType.GetObjectType(result, Schema);

        //    return new CompiledNode(objectGraphType, new Dictionary<string, CompiledField>());
        //}
    }
}
