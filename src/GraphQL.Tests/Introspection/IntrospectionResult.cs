namespace GraphQL.Tests.Introspection
{
    public class IntrospectionResult
    {
        public static readonly string Data =
@"{
  ""data"": {
    ""__schema"": null
  },
  ""errors"": [
    {
      ""message"": ""Cannot return null for non-null type. Field: queryType, Type: __Type!."",
      ""locations"": [
        {
          ""line"": 4,
          ""column"": 7
        }
      ],
      ""path"": [
        ""__schema"",
        ""queryType""
      ]
    }
  ]
}";
    }
}
