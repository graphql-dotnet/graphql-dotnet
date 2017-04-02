namespace GraphQL.Validation.PreciseComplexity
{
    public class PreciseComplexityConfiguration
    {
        public int? MaxComplexity { get; set; }

        public int? MaxDepth { get; set; }

        public double DefaultCollectionChildrenCount { get; set; }
    }
}
