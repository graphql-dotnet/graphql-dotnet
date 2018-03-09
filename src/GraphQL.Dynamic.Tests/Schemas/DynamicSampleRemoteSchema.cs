using System;
using System.Collections.Generic;
using GraphQL.Types;
using Newtonsoft.Json.Linq;

namespace GraphQL.Dynamic.Tests.Schemas
{
    public class DynamicSampleRemoteSchema : Schema
    {
        public static string GithubMoniker = "github";

        public DynamicSampleRemoteSchema(IEnumerable<Type> remoteTypes)
        {
            Query = new SampleRemoteSchemaRoot(remoteTypes);
        }

        public class SampleRemoteSchemaRoot : ObjectGraphType
        {
            public SampleRemoteSchemaRoot(IEnumerable<Type> remoteTypes)
            {
                this.RemoteField(remoteTypes, GithubMoniker, "GithubAPI", "githubDynamic", resolve: ctx =>
                {
                    // This is where we'd hypothetically return a JObject result from the remote server

                    return JObject.FromObject(new
                    {
                        user = new
                        {
                            login = "foo@bar.com",
                            id = 12,
                            company = "some company",
                            repos = new[]
                            {
                                new
                                {
                                    id = 22,
                                    name = "qux"
                                },
                                new
                                {
                                    id = 52,
                                    name = "baz"
                                }
                            }
                        }
                    });
                });
            }
        }
    }
}
