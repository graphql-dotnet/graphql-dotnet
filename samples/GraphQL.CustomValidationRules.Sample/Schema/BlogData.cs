namespace GraphQL.CustomValidationRules.Sample.Schema;

public class BlogData
{
    private readonly List<Post> _posts =
    [
        new Post { Id = "1", Title = "Introduction to GraphQL", Content = "GraphQL is a query language for APIs.", Author = "Alice" },
        new Post { Id = "2", Title = "Advanced GraphQL.NET", Content = "Deep dive into GraphQL.NET validation rules.", Author = "Bob" },
        new Post { Id = "3", Title = "Schema-First Approach", Content = "Building schemas using SDL definitions.", Author = "Alice" },
    ];

    private readonly List<Comment> _comments =
    [
        new Comment { Id = "101", PostId = "1", Text = "Great introduction!", Author = "Charlie" },
        new Comment { Id = "102", PostId = "1", Text = "Very helpful, thanks!", Author = "Dave" },
        new Comment { Id = "103", PostId = "2", Text = "Loved the deep dive.", Author = "Charlie" },
    ];

    public IEnumerable<Post> GetPosts() => _posts;

    public Post? GetPostById(string id) => _posts.FirstOrDefault(p => p.Id == id);

    public IEnumerable<Comment> GetCommentsForPost(string postId) =>
        _comments.Where(c => c.PostId == postId);

    public Post AddPost(string title, string content, string author)
    {
        var post = new Post
        {
            Id = (_posts.Count + 100).ToString(),
            Title = title,
            Content = content,
            Author = author,
        };
        _posts.Add(post);
        return post;
    }
}

public class Post
{
    public string Id { get; set; } = "";
    public string Title { get; set; } = "";
    public string Content { get; set; } = "";
    public string Author { get; set; } = "";
}

public class Comment
{
    public string Id { get; set; } = "";
    public string PostId { get; set; } = "";
    public string Text { get; set; } = "";
    public string Author { get; set; } = "";
}
