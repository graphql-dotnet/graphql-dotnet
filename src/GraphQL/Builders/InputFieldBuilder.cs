using GraphQL.Types;

namespace GraphQL.Builders
{
    /// <summary>
    /// Builds an input field for an Input Object graph type.
    /// </summary>
    public class InputFieldBuilder
    {
        /// <summary>
        /// Returns the generated field.
        /// </summary>
        public InputFieldType FieldType { get; }

        /// <summary>
        /// Initializes a new instance for the specified <see cref="InputFieldType"/>.
        /// </summary>
        protected InputFieldBuilder(InputFieldType fieldType)
        {
            FieldType = fieldType;
        }

        /// <summary>
        /// Returns a builder for a new field.
        /// </summary>
        /// <param name="type">The graph type of the field.</param>
        /// <param name="name">The name of the field.</param>
        public static InputFieldBuilder Create(IGraphType type, string name = "default")
        {
            var fieldType = new InputFieldType
            {
                Name = name,
                ResolvedType = type,
            };
            return new InputFieldBuilder(fieldType);
        }

        /// <inheritdoc cref="Create(IGraphType, string)"/>
        public static InputFieldBuilder Create([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type? type = null, string name = "default")
        {
            var fieldType = new InputFieldType
            {
                Name = name,
                Type = type,
            };
            return new InputFieldBuilder(fieldType);
        }

        /// <summary>
        /// Sets the graph type of the field.
        /// </summary>
        /// <param name="type">The graph type of the field.</param>
        public virtual InputFieldBuilder Type(IGraphType type)
        {
            FieldType.ResolvedType = type;
            return this;
        }

        /// <summary>
        /// Sets the name of the field.
        /// </summary>
        public virtual InputFieldBuilder Name(string name)
        {
            FieldType.Name = name;
            return this;
        }

        /// <summary>
        /// Sets the description of the field.
        /// </summary>
        public virtual InputFieldBuilder Description(string? description)
        {
            FieldType.Description = description;
            return this;
        }

        /// <summary>
        /// Sets the deprecation reason of the field.
        /// </summary>
        public virtual InputFieldBuilder DeprecationReason(string? deprecationReason)
        {
            FieldType.DeprecationReason = deprecationReason;
            return this;
        }

        /// <summary>
        /// Sets the default value of the field.
        /// </summary>
        public virtual InputFieldBuilder DefaultValue(object? defaultValue = default)
        {
            FieldType.DefaultValue = defaultValue;
            return this;
        }

        /// <summary>
        /// Runs a configuration delegate for the field.
        /// </summary>
        public virtual InputFieldBuilder Configure(Action<InputFieldType> configure)
        {
            configure(FieldType);
            return this;
        }

        /// <summary>
        /// Apply directive to field without specifying arguments. If the directive declaration has arguments,
        /// then their default values (if any) will be used.
        /// </summary>
        /// <param name="name">Directive name.</param>
        public virtual InputFieldBuilder Directive(string name)
        {
            FieldType.ApplyDirective(name);
            return this;
        }

        /// <summary>
        /// Apply directive to field specifying one argument. If the directive declaration has other arguments,
        /// then their default values (if any) will be used.
        /// </summary>
        /// <param name="name">Directive name.</param>
        /// <param name="argumentName">Argument name.</param>
        /// <param name="argumentValue">Argument value.</param>
        public virtual InputFieldBuilder Directive(string name, string argumentName, object? argumentValue)
        {
            FieldType.ApplyDirective(name, argumentName, argumentValue);
            return this;
        }

        /// <summary>
        /// Apply directive specifying two arguments. If the directive declaration has other arguments,
        /// then their default values (if any) will be used.
        /// </summary>
        /// <param name="name">Directive name.</param>
        /// <param name="argument1Name">First argument name.</param>
        /// <param name="argument1Value">First argument value.</param>
        /// <param name="argument2Name">Second argument name.</param>
        /// <param name="argument2Value">Second argument value.</param>
        public virtual InputFieldBuilder Directive(string name, string argument1Name, object? argument1Value, string argument2Name, object? argument2Value)
        {
            FieldType.ApplyDirective(name, argument1Name, argument1Value, argument2Name, argument2Value);
            return this;
        }

        /// <summary>
        /// Apply directive to field specifying configuration delegate.
        /// </summary>
        /// <param name="name">Directive name.</param>
        /// <param name="configure">Configuration delegate.</param>
        public virtual InputFieldBuilder Directive(string name, Action<AppliedDirective> configure)
        {
            FieldType.ApplyDirective(name, configure);
            return this;
        }
    }
}
