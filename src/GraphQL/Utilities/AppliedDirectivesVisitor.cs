using System;
using GraphQL.Types;

namespace GraphQL.Utilities
{
    internal sealed class AppliedDirectivesVisitor : ISchemaNodeVisitor
    {
        public AppliedDirectivesVisitor(ISchema schema)
        {
            Schema = schema;
        }

        public ISchema Schema { get; }

        public void VisitFieldArgumentDefinition(QueryArgument argument) => ValidateAppliedDirectives(argument);

        public void VisitDirectiveArgumentDefinition(QueryArgument argument) => ValidateAppliedDirectives(argument);

        public void VisitEnum(EnumerationGraphType type) => ValidateAppliedDirectives(type);

        public void VisitDirective(DirectiveGraphType directive) => ValidateAppliedDirectives(directive);

        public void VisitEnumValue(EnumValueDefinition value) => ValidateAppliedDirectives(value);

        public void VisitFieldDefinition(FieldType field) => ValidateAppliedDirectives(field);

        public void VisitInputFieldDefinition(FieldType field) => ValidateAppliedDirectives(field);

        public void VisitInputObject(IInputObjectGraphType type) => ValidateAppliedDirectives(type);

        public void VisitInterface(IInterfaceGraphType iface) => ValidateAppliedDirectives(iface);

        public void VisitObject(IObjectGraphType type) => ValidateAppliedDirectives(type);

        public void VisitScalar(ScalarGraphType scalar) => ValidateAppliedDirectives(scalar);

        public void VisitSchema(Schema schema) => ValidateAppliedDirectives(schema);

        public void VisitUnion(UnionGraphType union) => ValidateAppliedDirectives(union);

        private void ValidateAppliedDirectives(IProvideMetadata provider)
        {
            if (provider.HasAppliedDirectives())
            {
                foreach (var appliedDirective in provider.GetAppliedDirectives())
                {
                    var schemaDirective = Schema.Directives.Find(appliedDirective.Name);
                    if (schemaDirective == null)
                        throw new InvalidOperationException($"Unknown directive '{appliedDirective.Name}'.");

                    if (schemaDirective.Arguments?.Count > 0)
                    {
                        foreach (var arg in schemaDirective.Arguments.List)
                        {
                            if (arg.DefaultValue == null && appliedDirective.Find(arg.Name) == null)
                                throw new InvalidOperationException($"Directive '{appliedDirective.Name}' must specify required argument '{arg.Name}'.");
                        }
                    }

                    if (appliedDirective.Arguments?.Count > 0)
                    {
                        foreach (var arg in appliedDirective.Arguments)
                        {
                            if (schemaDirective.Arguments.Find(arg.Name) == null)
                                throw new InvalidOperationException($"Unknown directive argument '{arg.Name}' for directive '{appliedDirective.Name}'.");
                        }
                    }
                }
            }
        }
    }
}
