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
    }
}