namespace GraphQL.Validation.Complexity
{
    public class ComplexityConfiguration
    {
        public int? MaxDepth { get; set; }
        public int? MaxComplexity { get; set; }
        public double? AverageImpact { get; set; }
    }
}