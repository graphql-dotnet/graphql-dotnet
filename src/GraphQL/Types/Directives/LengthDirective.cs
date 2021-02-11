using System;

namespace GraphQL.Types
{
    /// <summary>
    /// Used to specify the minimum and/or maximum length for an input field or argument.
    /// </summary>
    public class LengthDirective : DirectiveGraphType
    {
        /// <inheritdoc/>
        public override bool? Introspectable => true;

        /// <summary>
        /// Initializes a new instance of the 'length' directive.
        /// </summary>
        public LengthDirective()
            : base("length", DirectiveLocation.InputFieldDefinition, DirectiveLocation.ArgumentDefinition)
        {
            Description = "Used to specify the minimum and/or maximum length for an input field or argument.";
            Arguments = new QueryArguments(
                new QueryArgument<IntGraphType>
                {
                    Name = "min",
                    Description = "If specified, specifies the minimum length that the input field or argument must have."
                },
                new QueryArgument<IntGraphType>
                {
                    Name = "max",
                    Description = "If specified, specifies the maximum length that the input field or argument must have."
                }
            );
        }

        /// <inheritdoc/>
        public override void Validate(AppliedDirective applied)
        {
            var min = applied.FindArgument("min")?.Value;
            var max = applied.FindArgument("max")?.Value;

            if (min == null && max == null)
                throw new ArgumentException("Either 'min' or 'max' argument must be specified for @length directive.");

            if (min != null && (!(min is int minV) || minV < 0))
                throw new ArgumentOutOfRangeException("min", $"Argument 'min' for @length directive must be of type int and greater or equal 0. Current: {min}, {min.GetType().Name}");

            if (max != null && (!(max is int maxV) || maxV < 0))
                throw new ArgumentOutOfRangeException("max", $"Argument 'max' for @length directive must be of type int and greater or equal 0. Current: {max}, {max.GetType().Name}");

            if (min != null && max != null && (int)min > (int)max)
                throw new ArgumentOutOfRangeException($"Argument 'max' must be equal or greater than 'min' argument for @length directive; min={min}, max={max}");
        }
    }
}
