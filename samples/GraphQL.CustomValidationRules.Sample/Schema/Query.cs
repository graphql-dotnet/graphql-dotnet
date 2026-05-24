namespace GraphQL.CustomValidationRules.Sample.Schema;

/// <summary>
/// Query root resolver. Public methods map to SDL fields automatically.
/// </summary>
public class Query
{
    public IEnumerable<Post> Posts([FromServices] BlogData data) => data.GetPosts();

    public Post? Post([FromServices] BlogData data, string id) => data.GetPostById(id);
}

/// <summary>
/// Mutation root resolver.
/// </summary>
public class Mutation
{
    public Post AddPost([FromServices] BlogData data, string title, string content, string author)
        => data.AddPost(title, content, author);
}
