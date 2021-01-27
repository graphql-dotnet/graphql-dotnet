using System;
using GraphQL.Types;

namespace GraphQL.Utilities
{
    internal sealed class AppliedDirectivesVisitor : BaseSchemaNodeVisitor
    {
        public AppliedDirectivesVisitor(ISchema schema)
        {
            Schema = schema;
        }

        public ISchema Schema { get; }

        public override void VisitFieldArgumentDefinition(QueryArgument argument) => ValidateAppliedDirectives(argument);

        public override void VisitDirectiveArgumentDefinition(QueryArgument argument) => ValidateAppliedDirectives(argument);

        public override void VisitEnum(EnumerationGraphType type) => ValidateAppliedDirectives(type);

        public override void VisitDirective(DirectiveGraphType directive) => ValidateAppliedDirectives(directive);

        public override void VisitEnumValue(EnumValueDefinition value) => ValidateAppliedDirectives(value);

        public override void VisitFieldDefinition(FieldType field) => ValidateAppliedDirectives(field);

        public override void VisitInputFieldDefinition(FieldType field) => ValidateAppliedDirectives(field);

        public override void VisitInputObject(IInputObjectGraphType type) => ValidateAppliedDirectives(type);

        public override void VisitInterface(IInterfaceGraphType iface) => ValidateAppliedDirectives(iface);

        public override void VisitObject(IObjectGraphType type) => ValidateAppliedDirectives(type);

        public override void VisitScalar(ScalarGraphType scalar) => ValidateAppliedDirectives(scalar);

        public override void VisitSchema(Schema schema) => ValidateAppliedDirectives(schema);

        public override void VisitUnion(UnionGraphType union) => ValidateAppliedDirectives(union);

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
