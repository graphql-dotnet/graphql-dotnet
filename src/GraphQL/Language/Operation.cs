namespace GraphQL.Language
{
    public class Operation
    {
        public Operation()
        {
            OperationType = OperationType.Query;
            Directives = new Directives();
            Variables = new Variables();
        }

        public string Name { get; set; }

        public OperationType OperationType { get; set; }

        public Directives Directives { get; set; }

        public Variables Variables { get; set; }

        public Selections Selections { get; set; }
    }
}