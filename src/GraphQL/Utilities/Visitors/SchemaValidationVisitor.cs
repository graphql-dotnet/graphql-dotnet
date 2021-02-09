using System;
using System.Linq;
using GraphQL.Types;

namespace GraphQL.Utilities
{
    internal sealed class SchemaValidationVisitor : BaseSchemaNodeVisitor
    {
        internal static readonly SchemaValidationVisitor Instance = new SchemaValidationVisitor();

        // See 'Type Validation' section in https://spec.graphql.org/June2018/#sec-Objects
        // Object types have the potential to be invalid if incorrectly defined.
        // This set of rules must be adhered to by every Object type in a GraphQL schema.
        public override void VisitObject(IObjectGraphType type, ISchema schema)
        {
            // 1
            if (type.Fields.Count == 0)
                throw new InvalidOperationException($"An Object type '{type.Name}' must define one or more fields.");

            // 2.1
            foreach (var item in type.Fields.List.ToLookup(f => f.Name))
            {
                if (item.Count() > 1)
                    throw new InvalidOperationException($"The field '{item.Key}' must have a unique name within Object type '{type.Name}'; no two fields may share the same name.");
            }

            foreach (var field in type.Fields.List)
            {
                // 2.2
                if (field.Name.StartsWith("__"))
                    throw new InvalidOperationException($"The field '{field.Name}' of an Object type '{type.Name}' must not have a name which begins with the __ (two underscores).");

                // 2.3
                if (field.ResolvedType != null ? field.ResolvedType.IsOutputType() == false : field.Type?.IsOutputType() == false)
                    throw new InvalidOperationException($"The field '{field.Name}' of an Object type '{type.Name}' must be an output type.");

                if (field.Arguments?.Count > 0)
                {
                    foreach (var argument in field.Arguments.List)
                    {
                        // 2.4.1
                        if (argument.Name.StartsWith("__"))
                            throw new InvalidOperationException($"The argument '{argument.Name}' of field '{type.Name}.{field.Name}' must not have a name which begins with the __ (two underscores).");

                        // 2.4.2
                        if (argument.ResolvedType != null ? argument.ResolvedType.IsInputType() == false : argument.Type?.IsInputType() == false)
                            throw new InvalidOperationException($"The argument '{argument.Name}' of field '{type.Name}.{field.Name}' must be an input type.");
                    }
                }
            }

            // 3
            // TODO: ? An object type may declare that it implements one or more unique interfaces.

            // 4
            // TODO: An object type must be a super‐set of all interfaces it implements
        }

        // See 'Type Validation' section in https://spec.graphql.org/June2018/#sec-Interfaces
        // Interface types have the potential to be invalid if incorrectly defined.
        public override void VisitInterface(IInterfaceGraphType iface, ISchema schema)
        {
            // 1
            if (iface.Fields.Count == 0)
                throw new InvalidOperationException($"An Interface type '{iface.Name}' must define one or more fields.");

            // 2.1
            foreach (var item in iface.Fields.List.ToLookup(f => f.Name))
            {
                if (item.Count() > 1)
                    throw new InvalidOperationException($"The field '{item.Key}' must have a unique name within Interface type '{iface.Name}'; no two fields may share the same name.");
            }

            foreach (var field in iface.Fields.List)
            {
                // 2.2
                if (field.Name.StartsWith("__"))
                    throw new InvalidOperationException($"The field '{field.Name}' of an Interface type '{iface.Name}' must not have a name which begins with the __ (two underscores).");

                // 2.3
                if (field.ResolvedType != null ? field.ResolvedType.IsOutputType() == false : field.Type?.IsOutputType() == false)
                    throw new InvalidOperationException($"The field '{field.Name}' of an Interface type '{iface.Name}' must be an output type.");

                if (field.Arguments?.Count > 0)
                {
                    foreach (var argument in field.Arguments.List)
                    {
                        // 2.4.1
                        if (argument.Name.StartsWith("__"))
                            throw new InvalidOperationException($"The argument '{argument.Name}' of field '{iface.Name}.{field.Name}' must not have a name which begins with the __ (two underscores).");

                        // 2.4.2
                        if (argument.ResolvedType != null ? argument.ResolvedType.IsInputType() == false : argument.Type?.IsInputType() == false)
                            throw new InvalidOperationException($"The argument '{argument.Name}' of field '{iface.Name}.{field.Name}' must be an input type.");
                    }
                }
            }
        }

        // See 'Type Validation' section in https://spec.graphql.org/June2018/#sec-Unions
        // Union types have the potential to be invalid if incorrectly defined.
        public override void VisitUnion(UnionGraphType union, ISchema schema)
        {
            // 1
            if (union.PossibleTypes.Count == 0)
                throw new InvalidOperationException($"A Union type '{union.Name}' must include one or more unique member types.");

            // 2 [requirement met by design]
            // The member types of a Union type must all be Object base types;
            // Scalar, Interface and Union types must not be member types of a Union.
            // Similarly, wrapping types must not be member types of a Union.
        }

        // See 'Type Validation' section in https://spec.graphql.org/June2018/#sec-Enums
        // Enum types have the potential to be invalid if incorrectly defined.
        public override void VisitEnum(EnumerationGraphType type, ISchema schema)
        {
            // 1
            if (type.Values.Count == 0)
                throw new InvalidOperationException($"An Enum type '{type.Name}' must define one or more unique enum values.");
        }

        // See 'Type Validation' section in https://spec.graphql.org/June2018/#sec-Input-Objects
        // Input Object types have the potential to be invalid if incorrectly defined.
        public override void VisitInputObject(IInputObjectGraphType type, ISchema schema)
        {
            // 1
            if (type.Fields.Count == 0)
                throw new InvalidOperationException($"An Input Object type '{type.Name}' must define one or more input fields.");

            // 2.1
            foreach (var item in type.Fields.List.ToLookup(f => f.Name))
            {
                if (item.Count() > 1)
                    throw new InvalidOperationException($"The inpit field '{item.Key}' must have a unique name within Input Object type '{type.Name}'; no two fields may share the same name.");
            }

            if (type.Fields?.Count > 0)
            {
                foreach (var field in type.Fields.List)
                {
                    // 2.2
                    if (field.Name.StartsWith("__"))
                        throw new InvalidOperationException($"The input field '{field.Name}' of an Input Object '{type.Name}' must not have a name which begins with the __ (two underscores).");

                    // 2.3
                    if (field.ResolvedType != null ? field.ResolvedType.IsInputType() == false : field.Type?.IsInputType() == false)
                        throw new InvalidOperationException($"The input field '{field.Name}' of an Input Object '{type.Name}' must be an input type.");
                }
            }
        }

        // See 'Type Validation' section in https://spec.graphql.org/June2018/#sec-Type-System.Directives
        // Directive types have the potential to be invalid if incorrectly defined.
        public override void VisitDirective(DirectiveGraphType directive, ISchema schema)
        {
            //TODO:
        }
    }
}
