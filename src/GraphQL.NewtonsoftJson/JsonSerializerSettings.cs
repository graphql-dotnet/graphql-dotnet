namespace GraphQL.NewtonsoftJson
{
    public class JsonSerializerSettings : Newtonsoft.Json.JsonSerializerSettings
    {
        public JsonSerializerSettings()
        {
            DateParseHandling = Newtonsoft.Json.DateParseHandling.None;
        }
    }
}
