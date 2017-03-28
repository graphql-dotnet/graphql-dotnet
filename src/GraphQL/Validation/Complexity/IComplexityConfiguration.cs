namespace GraphQL.Validation.Complexity
{
    public interface IComplexityConfiguration
    {
        double? FieldImpact { get; set; }
        int? MaxComplexity { get; set; }
        int? MaxDepth { get; set; }
    }
}