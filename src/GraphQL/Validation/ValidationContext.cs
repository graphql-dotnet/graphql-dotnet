using System.Collections.Generic;
using GraphQL.Language;
using GraphQL.Types;
using GraphQL.Utilities;

namespace GraphQL.Validation
{
    public class ValidationContext
    {
        private readonly List<ValidationError> _errors = new List<ValidationError>();

        public string OperationName { get; set; }
        public ISchema Schema { get; set; }
        public Document Document { get; set; }

        public TypeInfo TypeInfo { get; set; }

        public IEnumerable<ValidationError> Errors
        {
            get { return _errors; }
        }

        public void ReportError(ValidationError error)
        {
            _errors.Add(error);
        }

        public IEnumerable<VariableUsage> GetVariables(IDefinition node)
        {
            var usages = new List<VariableUsage>();
            var info = new TypeInfo(Schema);
            var listener = new EnterLeaveFuncListener(_ =>
            {
                _.Add<VariableReference>(
                    n => n is VariableReference,
                    enter: varRef =>
                    {
                        usages.Add(new VariableUsage { Node = varRef, Type = info.GetInputType()});
                    });
            });
            var visitor = new BasicVisitor(info, listener);
            visitor.Visit(node);

            return usages;
        }

        public string Print(INode node)
        {
            return AstPrinter.Print(node);
        }

        public string Print(GraphType type)
        {
            var printer = new SchemaPrinter(Schema);
            return printer.ResolveName(type);
        }
    }

    public struct VariableUsage
    {
        public VariableReference Node;
        public GraphType Type;
    }
}
