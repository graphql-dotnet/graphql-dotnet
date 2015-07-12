namespace GraphQL.Tests
{
    public class StarWarsBasicQueryTests : QueryTestBase<StarWarsSchema>
    {
        [Test]
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
  hero: {
    name: 'R2-D2'
  }
}";

            AssertQuerySuccess(query, expected);
        }

        [Test]
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
  hero: {
    name: 'R2-D2'
  }
}";

            AssertQuerySuccess(query, expected);
        }

        [Test]
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
  hero: {
    id: '3',
    name: 'R2-D2',
    friends: [
      {
        name: 'Luke',
      },
      {
        name: 'C-3PO',
      },
    ]
  }
}";

            AssertQuerySuccess(query, expected);
        }

        [Test]
        public void can_query_for_humans()
        {
            var query = @"
               {
                  human(id: ""1"") {
                    name
                  }
               }
            ";

            var expected = @"{
  human: {
    name: 'Luke'
  }
}";

            AssertQuerySuccess(query, expected);
        }

        [Test]
        public void can_query_for_friends_of_humans()
        {
            var query = @"
               {
                  human(id: ""1"") {
                    name
                    friends {
                      name
                    }
                  }
               }
            ";

            var expected = @"{
  human: {
    name: 'Luke',
    friends: [{name:'R2-D2'}, {name:'C-3PO'}]
  }
}";

            AssertQuerySuccess(query, expected);
        }

        [Test]
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
  droid: {
    name: 'C-3PO'
  }
}";

            AssertQuerySuccess(query, expected);
        }

        [Test]
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
              human: {
                name: 'Luke'
              }
            }
            ";

            var inputs = new Inputs {["id"] = "1"};

            AssertQuerySuccess(query, expected, inputs);
        }
    }
}
