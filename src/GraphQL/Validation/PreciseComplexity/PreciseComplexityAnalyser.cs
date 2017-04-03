using System;
using System.Linq;

using GraphQL.Language.AST;
using GraphQL.Types;

namespace GraphQL.Validation.PreciseComplexity
{
    using System.Collections.Generic;

    public class PreciseComplexityAnalyser
    {
        private DocumentExecuter executer;

        private PreciseComplexityContext context;

        private readonly Dictionary<string, Result> NamedFragmentComplexity = new Dictionary<string, Result>();

        public PreciseComplexityAnalyser(DocumentExecuter executer, PreciseComplexityContext context)
        {
            this.executer = executer;
            this.context = context;
        }

        public delegate double GetComplexity(
            PreciseComplexityContext context,
            Func<string, object> getArgumentValue,
            double childComplexity);

        public Result Analyze(IComplexGraphType graphType, SelectionSet selectionSet)
        {
            return selectionSet.Selections.Aggregate(
                new Result(0d, 0),
                (previousResult, selection) =>
                    {
                        Result complexity;
                        if (selection is Field)
                        {
                            var field = (Field)selection;
                            complexity = this.CalculateFieldComplexity(this.executer, this.context, graphType, field);
                        }
                        else if (selection is FragmentSpread)
                        {
                            var spread = (FragmentSpread)selection;

                            if (!this.NamedFragmentComplexity.TryGetValue(spread.Name, out complexity))
                            {
                                var fragment = context.Fragments.FindDefinition(spread.Name);
                                if (fragment == null)
                                {
                                    throw new InvalidOperationException($"Uknown fragment named {spread.Name}");
                                }

                                var fragmentType =
                                    context.Schema.AllTypes.FirstOrDefault(t => t.Name == fragment.Type.Name) as
                                        IObjectGraphType;
                                if (fragmentType == null)
                                {
                                    throw new InvalidOperationException($"Could not find object type for fragment named {spread.Name}");
                                }

                                complexity = this.Analyze(fragmentType, fragment.SelectionSet);
                                this.NamedFragmentComplexity[spread.Name] = complexity;
                            }
                        }
                        else if (selection is InlineFragment)
                        {
                            var inline = (InlineFragment)selection;
                            var fragmentType = graphType;
                            var inlineType = inline.Type;
                            if (inlineType != null)
                            {
                                fragmentType =
                                    context.Schema.AllTypes.FirstOrDefault(t => t.Name == inlineType.Name) as
                                        IComplexGraphType;

                                if (fragmentType == null)
                                {
                                    throw new InvalidOperationException(
                                        $"Could not find object type for inline fragment named {inlineType.Name}");
                                }
                            }
                            else
                            {
                                fragmentType = graphType;
                            }

                            complexity = this.Analyze(fragmentType, inline.SelectionSet);
                        }
                        else
                        {
                            throw new InvalidOperationException($"Unknown selection type {selection.GetType().Name}");
                        }

                        var result = new Result(
                            previousResult.Complexity + complexity.Complexity,
                            Math.Max(previousResult.MaxDepth, complexity.MaxDepth));

                        if (context.Configuration.MaxComplexity.HasValue
                            && result.Complexity > context.Configuration.MaxComplexity.Value)
                        {
                            // todo: display exect failure place and message
                            throw new InvalidOperationException("Query is too complex to execute.");
                        }

                        if (context.Configuration.MaxDepth.HasValue
                            && result.MaxDepth > context.Configuration.MaxDepth.Value)
                        {
                            // todo: display exect failure place and message
                            throw new InvalidOperationException("Query is too nested to execute.");
                        }

                        return result;
                    });
        }

        private Result CalculateFieldComplexity(
            DocumentExecuter executer,
            PreciseComplexityContext context,
            IComplexGraphType graphType,
            Field field)
        {
            var fieldType = executer.GetFieldDefinition(context.Schema, graphType, field);
            if (fieldType == null)
            {
                throw new InvalidOperationException($"Uknown field {field.Name} of type {graphType.Name}");
            }

            var getComplexity = fieldType.GetComplexity;

            Result fieldChildrenComplexity;
            var resolvedType = fieldType.ResolvedType;
            while (resolvedType is ListGraphType)
            {
                if (getComplexity == null)
                {
                    getComplexity =
                        (requestContext, getArgumentValue, childrenComplexity) =>
                            1d + (childrenComplexity * requestContext.Configuration.DefaultCollectionChildrenCount);
                }

                resolvedType = ((ListGraphType)resolvedType).ResolvedType;
            }

            if (resolvedType is ScalarGraphType)
            {
                fieldChildrenComplexity = new Result(0d, 0);
            }
            else if (resolvedType is IComplexGraphType)
            {
                fieldChildrenComplexity = this.Analyze((IComplexGraphType)resolvedType, field.SelectionSet);
            }
            else
            {
                throw new InvalidOperationException(
                    $"Uknown field type {resolvedType?.GetType().Name} "
                    + $"for field {field.Name} of type {graphType.Name}");
            }

            if (getComplexity == null)
            {
                getComplexity = (complexityContext, getArgumentValue, childrenComplexity) => 1d + childrenComplexity;
            }

            return
                new Result(
                    getComplexity(
                        context,
                        argumentName => this.GetArgumentValue(argumentName, executer, context, fieldType, field),
                        fieldChildrenComplexity.Complexity),
                    fieldChildrenComplexity.MaxDepth + 1);
        }

        private object GetArgumentValue(
            string argumentName,
            DocumentExecuter executer,
            PreciseComplexityContext context,
            FieldType fieldType,
            Field field)
        {
            if (fieldType.Arguments == null || !fieldType.Arguments.Any())
            {
                return null;
            }

            var arg = fieldType.Arguments.FirstOrDefault(a => a.Name == argumentName);
            if (arg == null)
            {
                return null;
            }

            var value = field.Arguments.ValueFor(argumentName);
            var type = arg.ResolvedType;

            var coercedValue = executer.CoerceValue(context.Schema, type, value, context.Variables);
            return coercedValue ?? arg.DefaultValue;
        }

        public class Result
        {
            /// <inheritdoc />
            public Result(double complexity, int maxDepth)
            {
                this.Complexity = complexity;
                this.MaxDepth = maxDepth;
            }

            public double Complexity { get; set; }

            public int MaxDepth { get; set; }
        }
    }
}
