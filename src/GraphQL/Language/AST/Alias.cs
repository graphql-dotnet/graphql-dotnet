namespace GraphQL.Language.AST
{
    public class Alias
    {
        public NameNode Al { get; set; }
        public NameNode Name { get; set; }

        public Alias(NameNode alias, NameNode name)
        {
            Al = alias;
            Name = name;
        }
    }
}
