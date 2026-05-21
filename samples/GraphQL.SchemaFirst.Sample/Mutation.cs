using GraphQL.Types;

namespace GraphQL.SchemaFirst.Sample;

/// <summary>
/// Mutation type for the Star Wars schema (Schema-First approach).
/// </summary>
public class Mutation
{
    private readonly StarWarsData _data;

    public Mutation(StarWarsData data)
    {
        _data = data;
    }

    /// <summary>
    /// Resolver for the 'createReview' field.
    /// </summary>
    [GraphQLMetadata("createReview")]
    public Review CreateReview(Episode episode, ReviewInput review)
    {
        // In a real application, you would save the review to a database
        // For this demo, we just return a new Review object
        return new Review
        {
            Episode = episode,
            Stars = review.Stars,
            Commentary = review.Commentary
        };
    }
}

/// <summary>
/// Input type for creating a review (matches SDL definition).
/// </summary>
public class ReviewInput
{
    public int Stars { get; set; }
    public string? Commentary { get; set; }
}

/// <summary>
/// Review type (matches SDL definition).
/// </summary>
public class Review
{
    public Episode? Episode { get; set; }
    public int Stars { get; set; }
    public string? Commentary { get; set; }
}
