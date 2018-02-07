namespace GraphQL.Validation.Complexity
{
    public class ComplexityConfiguration
    {
        public int? MaxDepth { get; set; }
        public int? MaxComplexity { get; set; }

        /// <summary>
        /// Hard-coded maximum number of objects returned by each field.
        /// If there is no hard-coded maximum then use the average number of rows/objects returned by each field.
        /// </summary>
        public double? FieldImpact { get; set; }
    }
}
