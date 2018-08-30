namespace GraphQL.Language.AST
{
    public class Alias
    {
        public NameNode Al { get; }
        public NameNode Name { get; }

        public Alias(NameNode alias, NameNode name)
        {
            Al = alias;
            Name = name;
        }
    }
}
