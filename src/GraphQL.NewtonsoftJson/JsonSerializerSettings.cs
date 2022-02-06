namespace GraphQL.NewtonsoftJson
{
    /// <inheritdoc cref="Newtonsoft.Json.JsonSerializerSettings"/>
    public class JsonSerializerSettings : Newtonsoft.Json.JsonSerializerSettings
    {
        /// <inheritdoc cref="Newtonsoft.Json.JsonSerializerSettings.JsonSerializerSettings"/>
        public JsonSerializerSettings()
        {
            DateParseHandling = Newtonsoft.Json.DateParseHandling.None;
        }
    }
}
