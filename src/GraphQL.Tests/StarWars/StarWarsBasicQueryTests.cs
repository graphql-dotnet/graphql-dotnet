using System.Collections.Generic;
using Xunit;

namespace GraphQL.Tests.StarWars
{
    public class StarWarsBasicQueryTests : StarWarsTestBase
    {
        [Fact]
        public void identifies_r2_as_the_hero()
        {
            var query = @"
                query HeroNameQuery {
                  hero {
                    name
                  }
                }
            ";

            var expected = @"{
              ""hero"": {
                ""name"": ""R2-D2""
              }
            }";

            AssertQuerySuccess(query, expected);
        }

        [Fact]
        public void can_query_without_query_name()
        {
            var query = @"
               {
                  hero {
                    name
                  }
               }
            ";

            var expected = @"{
              ""hero"": {
                ""name"": ""R2-D2""
              }
            }";

            AssertQuerySuccess(query, expected);
        }

        [Fact]
        public void can_query_for_the_id_and_friends_of_r2()
        {
            var query = @"
                query HeroNameAndFriendsQuery {
                  hero {
                    id
                    name
                    friends {
                      name
                    }
                  }
                }
            ";

            var expected = @"{
              ""hero"": {
                ""id"": ""3"",
                ""name"": ""R2-D2"",
                ""friends"": [
                  {
                    ""name"": ""Luke""
                  },
                  {
                    ""name"": ""C-3PO""
                  }
                ]
              }
            }";

            AssertQuerySuccess(query, expected);
        }

        [Fact]
        public void can_query_for_humans()
        {
            var query = @"
               {
                  human(id: ""1"") {
                    name
                    homePlanet
                  }
               }
            ";

            var expected = @"{
              ""human"": {
                ""name"": ""Luke"",
                ""homePlanet"": ""Tatooine""
              }
            }";

            AssertQuerySuccess(query, expected);
        }

        [Fact]
        public void can_query_for_friends_of_humans()
        {
            var query = @"
               {
                  human(id: ""1"") {
                    name
                    friends {
                      name
                      appearsIn
                    }
                  }
               }
            ";

            var expected = @"{
              ""human"": {
                ""name"": ""Luke"",
                ""friends"": [
                  {""name"":""R2-D2"", ""appearsIn"":[""NEWHOPE"",""EMPIRE"",""JEDI""]},
                  {""name"":""C-3PO"", ""appearsIn"":[""NEWHOPE"",""EMPIRE"",""JEDI""]}
                ]
              }
            }";

            AssertQuerySuccess(query, expected);
        }

        [Fact]
        public void can_query_for_connected_friends_of_humans()
        {
            var query = @"
               {
                  human(id: ""1"") {
                    name
                    friendsConnection {
                      totalCount
                      edges {
                        node {
                          name
                          appearsIn
                        }
                        cursor
                      },
                      pageInfo {
                        endCursor
                        hasNextPage
                      }
                    }
                  }
               }
            ";

            var expected = @"{
                ""human"": {
                  ""name"": ""Luke"",
                  ""friendsConnection"": {
                    ""totalCount"": 2,
                    ""edges"": [
                      {
                        ""node"": {
                          ""name"": ""R2-D2"",
                          ""appearsIn"": [
                            ""NEWHOPE"",
                            ""EMPIRE"",
                            ""JEDI""
                          ]
                        },
                        ""cursor"": ""Mw==""
                      },
                      {
                        ""node"": {
                          ""name"": ""C-3PO"",
                          ""appearsIn"": [
                            ""NEWHOPE"",
                            ""EMPIRE"",
                            ""JEDI""
                          ]
                        },
                        ""cursor"": ""NA==""
                      }
                    ],
                    ""pageInfo"": {
                      ""endCursor"": ""NA=="",
                      ""hasNextPage"": false
                    }
                  }
                }
              }";

            AssertQuerySuccess(query, expected);
        }

        [Fact]
        public void can_query_for_droids()
        {
            var query = @"
               {
                  droid(id: ""4"") {
                    name
                  }
               }
            ";

            var expected = @"{
              ""droid"": {
                ""name"": ""C-3PO""
              }
            }";

            AssertQuerySuccess(query, expected);
        }

        [Fact]
        public void can_query_for_connected_friends_of_droids_second_page()
        {
            var query = @"
               {
                  droid(id: ""3"") {
                    name
                    friendsConnection(first: 1, after: ""NA=="") {
                      totalCount
                      edges {
                        node {
                          name
                          appearsIn
                        }
                        cursor
                      }
                      pageInfo {
                        endCursor
                        hasNextPage
                      }
                    }
                  }
               }
            ";

            var expected = @"{
                ""droid"": {
                  ""name"": ""R2-D2"",
                  ""friendsConnection"": {
                    ""totalCount"": 1,
                    ""edges"": [
                      {
                        ""node"": {
                          ""name"": ""C-3PO"",
                          ""appearsIn"": [
                            ""NEWHOPE"",
                            ""EMPIRE"",
                            ""JEDI""
                          ]
                        },
                        ""cursor"": ""NA==""
                      }
                    ],
                    ""pageInfo"": {
                      ""endCursor"": ""NA=="",
                      ""hasNextPage"": false
                    }
                  }
                }
              }";

            AssertQuerySuccess(query, expected);
        }

        [Fact]
        public void create_generic_query_that_fetches_luke()
        {
            var query = @"
                query humanQuery($id: String!) {
                  human(id: $id) {
                    name
                  }
                }
            ";

            var expected = @"{
              ""human"": {
                ""name"": ""Luke""
              }
            }
            ";

            var inputs = new Inputs(new Dictionary<string, object> { { "id", "1" } });

            AssertQuerySuccess(query, expected, inputs);
        }

        [Fact]
        public void query_same_root_field_using_alias()
        {
            var query = @"
               query SomeDroids {
                  r2d2: droid(id: ""3"") {
                    name
                  }

                  c3po: droid(id: ""4"") {
                    name
                  }
               }
            ";

            var expected = @"{
              ""r2d2"": {
                ""name"": ""R2-D2""
              },
              ""c3po"": {
                ""name"": ""C-3PO""
              }
            }";

            AssertQuerySuccess(query, expected);
        }

        [Fact]
        public void can_add_new_human()
        {
            var mutation = @"mutation ($human:HumanInput!){ createHuman(human: $human) { name homePlanet } }";

            var expected = @"{
              ""createHuman"": {
                ""name"": ""Boba Fett"",
                ""homePlanet"": ""Kamino""
              }
            }";

            var data = new Dictionary<string, object>
            {
                {
                    "human",
                    new Dictionary<string, object>
                    {
                        {"name", "Boba Fett"},
                        {"homePlanet", "Kamino"}
                    }
                }
            };

            var variables = new Inputs(data);

            AssertQuerySuccess(mutation, expected, variables);
        }
    }
}
