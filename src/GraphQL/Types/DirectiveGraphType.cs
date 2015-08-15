namespace GraphQL.Types
{
    public abstract class DirectiveGraphType
    {
        public static IncludeDirective Include = new IncludeDirective();
        public static SkipDirective Skip = new SkipDirective();

        public string Name { get; set; }

        public string Description { get; set; }

        public QueryArguments Arguments { get; set; }

        public bool OnOperation { get; set; }

        public bool OnFragment { get; set; }

        public bool OnField { get; set; }
    }

    public class IncludeDirective : DirectiveGraphType
    {
        public IncludeDirective()
        {
            Name = "include";
            Description = "Directs the executor to include this field or fragment only when the 'if' argument is true.";
            Arguments = new QueryArguments(new [] { new QueryArgument<NonNullGraphType<BooleanGraphType>> { Name = "if" } });
            OnOperation = false;
            OnFragment = true;
            OnField = true;
        }
    }

    public class SkipDirective : DirectiveGraphType
    {
        public SkipDirective()
        {
            Name = "skip";
            Description = "Directs the executor to skip this field or fragment when the 'if' argument is true.";
            Arguments = new QueryArguments(new [] { new QueryArgument<NonNullGraphType<BooleanGraphType>>  { Name = "if" } });
            OnOperation = false;
            OnFragment = true;
            OnField = true;
        }
    }
}
