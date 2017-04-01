namespace GraphQL.Validation.PreciseComplexity
{
    using System;
    using System.Linq;

    using GraphQL.Language.AST;
    using GraphQL.Types;

    public class PreciseComplexityAnalyser
    {
        public Result Analyze(
            DocumentExecuter executer,
            PreciseComplexityContext context,
            IObjectGraphType graphType,
            SelectionSet selectionSet)
        {
            var result = new Result(0d, 0);
            selectionSet.Selections.Apply(
                selection =>
                    {
                        if (selection is Field)
                        {
                            var field = (Field)selection;
                            var fieldComplexity = this.CalculateFieldComplexity(executer, context, graphType, field);
                            result.Complexity += fieldComplexity.Complexity;
                            result.MaxDepth = Math.Max(result.MaxDepth, fieldComplexity.MaxDepth);
                        }
                        else if (selection is FragmentSpread)
                        {
                            var spread = (FragmentSpread)selection;
                            var fragment = context.Fragments.FindDefinition(spread.Name);
                            if (fragment == null)
                            {
                                throw new Exception($"Uknown fragment named {spread.Name}");
                            }

                            var fragmentType =
                                context.Schema.AllTypes.FirstOrDefault(t => t.Name == fragment.Type.Name) as
                                    IObjectGraphType;
                            if (fragmentType == null)
                            {
                                throw new Exception($"Could not find object type for fragment named {spread.Name}");
                            }

                            var fragmentComplexity = this.Analyze(
                                executer,
                                context,
                                fragmentType,
                                fragment.SelectionSet);
                            result.Complexity += fragmentComplexity.Complexity;
                            result.MaxDepth = Math.Max(result.MaxDepth, fragmentComplexity.MaxDepth);
                        }
                        else if (selection is InlineFragment)
                        {
                            var inline = (InlineFragment)selection;
                            var fragmentType = graphType;
                            if (inline.Type != null)
                            {
                                fragmentType =
                                    context.Schema.AllTypes.FirstOrDefault(t => t.Name == inline.Type.Name) as
                                        IObjectGraphType;
                                if (fragmentType == null)
                                {
                                    throw new Exception(
                                        $"Could not find object type for inline fragment named {inline.Type.Name}");
                                }

                                var fragmentComplexity = this.Analyze(
                                    executer,
                                    context,
                                    fragmentType,
                                    inline.SelectionSet);
                                result.Complexity += fragmentComplexity.Complexity;
                                result.MaxDepth = Math.Max(result.MaxDepth, fragmentComplexity.MaxDepth);
                            }
                        }

                        if (context.Configuration.MaxComplexity.HasValue
                            && result.Complexity > context.Configuration.MaxComplexity.Value)
                        {
                            // todo: display exect failure place and message
                            throw new Exception("The request is too complex");
                        }

                        if (context.Configuration.MaxDeapth.HasValue
                            && result.MaxDepth > context.Configuration.MaxDeapth.Value)
                        {
                            // todo: display exect failure place and message
                            throw new Exception("The request is too complex");
                        }
                    });

            return result;
        }

        private Result CalculateFieldComplexity(
            DocumentExecuter executer,
            PreciseComplexityContext context,
            IObjectGraphType graphType,
            Field field)
        {
            var fieldType = executer.GetFieldDefinition(context.Schema, graphType, field);
            if (fieldType == null)
            {
                throw new Exception($"Uknown field {field.Name} of type {graphType.Name}");
            }

            var getComplexity = fieldType.GetComplexity;

            Result fieldChildrenComplexity;
            var resolvedType = fieldType.ResolvedType;
            while (resolvedType is ListGraphType)
            {
                if (getComplexity == null)
                {
                    getComplexity =
                        (requestContext, arguments, childrenComplexity) =>
                            1d + (childrenComplexity * requestContext.Configuration.DefaultCollectionChildrenCount);
                }

                resolvedType = ((ListGraphType)resolvedType).ResolvedType;
            }

            if (resolvedType is ScalarGraphType)
            {
                fieldChildrenComplexity = new Result(0d, 0);
            }
            else if (resolvedType is IObjectGraphType)
            {
                fieldChildrenComplexity = this.Analyze(
                    executer,
                    context,
                    (IObjectGraphType)resolvedType,
                    field.SelectionSet);
            }
            else
            {
                throw new Exception(
                    $"Uknown field type {resolvedType?.GetType().Name} "
                    + $"for field {field.Name} of type {graphType.Name}");
            }

            if (getComplexity == null)
            {
                getComplexity = (complexityContext, arguments, childrenComplexity) => 1d + childrenComplexity;
            }

            var fieldComplexity = new Result(
                getComplexity(context, field.Arguments, fieldChildrenComplexity.Complexity),
                fieldChildrenComplexity.MaxDepth + 1);

            return fieldComplexity;
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
