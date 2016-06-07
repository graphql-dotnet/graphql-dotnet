namespace GraphQL.Tests.Validation
{
    public class ValidationErrorAssertion
    {
        public string Message { get; set; }
        public int? Line { get; set; }
        public int? Column { get; set; }
    }
}