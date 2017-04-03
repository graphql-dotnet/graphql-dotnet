namespace GraphQL.Validation.PreciseComplexity
{
    public class PreciseComplexityConfiguration
    {
        public double? MaxComplexity { get; set; }

        public int? MaxDepth { get; set; }

        public double DefaultCollectionChildrenCount { get; set; }
    }
}
