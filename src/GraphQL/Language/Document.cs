namespace GraphQL.Language
{
    public class Document
    {
        public Document()
        {
            Operations = new Operations();
            Fragments = new Fragments();
        }

        public Operations Operations { get; set; }

        public Fragments Fragments { get; set; }
    }
}