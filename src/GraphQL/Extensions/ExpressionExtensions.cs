using System.Linq.Expressions;

namespace GraphQL
{
    internal static class ExpressionExtensions
    {
        /// <summary>
        /// Replaces all occurrences of <paramref name="oldParameter"/> with <paramref name="newBody"/> within <paramref name="expression"/>.
        /// </summary>
        public static Expression Replace(this Expression expression, ParameterExpression oldParameter, Expression newBody)
        {
            if (expression == null)
                throw new ArgumentNullException(nameof(expression));
            if (oldParameter == null)
                throw new ArgumentNullException(nameof(oldParameter));
            if (newBody == null)
                throw new ArgumentNullException(nameof(newBody));
            if (expression is LambdaExpression)
                throw new InvalidOperationException("The search & replace operation must be performed on the body of the lambda.");
            if (oldParameter.Type != newBody.Type)
                throw new InvalidOperationException("The old parameter and its replacement expression must be of the same type.");

            return new ParameterReplacerVisitor(oldParameter, newBody).Visit(expression);
        }

        private class ParameterReplacerVisitor : ExpressionVisitor
        {
            private readonly ParameterExpression _source;
            private readonly Expression _target;

            public ParameterReplacerVisitor(ParameterExpression source, Expression target)
            {
                _source = source;
                _target = target;
            }

            protected override Expression VisitParameter(ParameterExpression node)
            {
                // Replace the source with the target, visit other params as usual.
                return node.Equals(_source) ? _target : base.VisitParameter(node);
            }
        }
    }
}
