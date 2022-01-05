namespace GraphQL.Reflection
{
    /// <summary>
    /// Nullable reference annotation for a member.
    /// </summary>
    public enum Nullability
    {
        /// <summary>
        /// The member is a reference type not marked with nullable reference annotations.
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// The member is a non-nullable value type or is a nullable reference type marked as non-nullable.
        /// </summary>
        NonNullable = 1,

        /// <summary>
        /// The member is a nullable value type or is a nullable reference type marked as nullable.
        /// </summary>
        Nullable = 2,
    }
}
