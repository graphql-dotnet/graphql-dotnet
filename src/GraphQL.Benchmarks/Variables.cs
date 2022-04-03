namespace GraphQL.Benchmarks;

public static class Variables
{
    public static readonly string VariablesVariable = @"
{ ""in"":
  [
    {
      ""ints"": [[1,2],[3,4,5],[6,7,8]],
      ""widgets"": [
        {""name"":""bolts1"", ""description"":""this is a test"", ""amount"":2.99, ""quantity"":42},
        {""name"":""bolts2"", ""description"":""this is a test"", ""amount"":2.99, ""quantity"":42}
      ]
    },
    {
      ""ints"": [[11,12],[13,14,15],[16,17,18]],
      ""widgets"": [
        {""name"":""bolts3"", ""description"":""this is a test"", ""amount"":2.99, ""quantity"":42},
        {""name"":""bolts4"", ""description"":""this is a test"", ""amount"":2.99, ""quantity"":42}
      ]
    },
    {
      ""ints"": [[21,12],[13,14,15],[16,17,18]],
      ""widgets"": [
        {""name"":""bolts5"", ""description"":""this is a test"", ""amount"":2.99, ""quantity"":42},
        {""name"":""bolts6"", ""description"":""this is a test"", ""amount"":2.99, ""quantity"":42}
      ]
    }
  ]
}
";
}
