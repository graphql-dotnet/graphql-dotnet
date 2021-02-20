namespace GraphQL.Validation.Complexity
{
    /// <summary>
    /// Configuration parameters for a complexity analyzer.
    /// </summary>
    public class ComplexityConfiguration
    {
        /// <summary>
        /// Gets or sets the allowed maximum depth of the query.
        /// <see langword="null"/> if the depth does not need to be limited.
        /// </summary>
        public int? MaxDepth { get; set; }

        /// <summary>
        /// Gets or sets the maximum calculated document complexity factor.
        /// <see langword="null"/> if the complexity does not need to be limited.
        /// </summary>
        public int? MaxComplexity { get; set; }

        /// <summary>
        /// Hardcoded maximum number of objects returned by each field.
        /// If there is no hardcoded maximum then use the average number of rows/objects returned by each field.
        /// </summary>
        public double? FieldImpact { get; set; }

        /// <summary>
        /// Max number of times to traverse tree nodes. GraphiQL queries take ~95 iterations, adjust as needed.
        /// </summary>
        public int MaxRecursionCount { get; set; } = 250;
    }
}
