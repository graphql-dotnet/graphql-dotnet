using System.Text.Json;

namespace GraphQL.Federation.Tests;

internal static class StringExtensions
{
    public static string GetSdl(this string response)
    {
        var ret = JsonSerializer.Deserialize<Response>(response);
        return ret.ShouldNotBeNull().data.ShouldNotBeNull()._service.ShouldNotBeNull().sdl.ShouldNotBeNull();
    }

    public class Response
    {
        public Data data { get; set; } = null!;

        public class Data
        {
            public Service _service { get; set; } = null!;

            public class Service
            {
                public string sdl { get; set; } = null!;
            }
        }
    }
}
