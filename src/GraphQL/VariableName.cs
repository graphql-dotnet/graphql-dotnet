namespace GraphQL
{
    /// <summary>
    /// Represents the full path to a property of a variable, typically for
    /// use in error messages, such as <c>myVariable[1].name</c>.
    /// Use the <see cref="ToString"/> method to return the serialized name
    /// of the variable property.
    /// </summary>
    public struct VariableName
    {
        /// <summary>
        /// Initialize an instance with the specified parent variable name and specified child index.
        /// </summary>
        public VariableName(VariableName variableName, int index)
        {
            Name = variableName;
            Index = index;
            ChildName = null;
        }

        /// <summary>
        /// Initialize an instance with the specified parent variable name and specified child property.
        /// </summary>
        public VariableName(VariableName variableName, string childName)
        {
            if (variableName.ChildName == null)
            {
                Name = variableName.Name;
                Index = variableName.Index;
            }
            else
            {
                Name = variableName;
                Index = default;
            }
            ChildName = childName;
        }

        /// <summary>
        /// Gets or sets the variable name.
        /// Does not include the index or child property name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the index of the variable.
        /// </summary>
        public int? Index { get; set; }

        /// <summary>
        /// Gets or sets the child property name of the variable.
        /// </summary>
        public string? ChildName { get; set; }

        /// <summary>
        /// Returns the full path of the variable property represented by this instance.
        /// </summary>
        public override string ToString() => (!Index.HasValue ? Name : Name + "[" + Index.Value + "]") + (ChildName != null ? '.' + ChildName : null);

        /// <summary>
        /// Returns a new instance with the specified name.
        /// </summary>
        public static implicit operator VariableName(string name) => new VariableName { Name = name };

        /// <inheritdoc cref="ToString"/>
        public static implicit operator string(VariableName variableName) => variableName.ToString();
    }
}
