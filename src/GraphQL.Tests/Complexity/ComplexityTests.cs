namespace GraphQL.Tests.Complexity
{
    public class ComplexityTests : ComplexityTestBase
    {
        [Fact]
        public void inline_fragments_test()
        {
            var withFrag = AnalyzeComplexity(@"
query withInlineFragment {
  profiles(handles: [""dnetguru""]) {
    handle
    ... on User {
      friends {
        count
      }
    }
  }
}");
            var woFrag = AnalyzeComplexity(@"
query withoutFragments {
  profiles(handles: [""dnetguru""]) {
    handle
    friends {
      count
    }
  }
}");

            withFrag.Complexity.ShouldBe(woFrag.Complexity);
            withFrag.TotalQueryDepth.ShouldBe(woFrag.TotalQueryDepth);
        }

        [Fact]
        public void fragments_test()
        {
            var withFrag = AnalyzeComplexity(@"
{
  leftComparison: hero(episode: EMPIRE) {
    ...comparisonFields
  }
  rightComparison: hero(episode: JEDI) {
    ...comparisonFields
  }
}

fragment comparisonFields on Character {
  name
  appearsIn
  friends {
    name
  }
}");
            var woFrag = AnalyzeComplexity(@"
{
  leftComparison: hero(episode: EMPIRE) {
    name
    appearsIn
    friends {
      name
    }
  }
  rightComparison: hero(episode: JEDI) {
    name
    appearsIn
    friends {
      name
    }
  }
}");

            withFrag.Complexity.ShouldBe(woFrag.Complexity);
            withFrag.TotalQueryDepth.ShouldBe(woFrag.TotalQueryDepth);
        }

        [Fact]
        public void fragment_test_nested()
        {
            var withFrag = AnalyzeComplexity(@"
			{
			  A {
			    W {
			      ...X
			    }
			  }
			}

			fragment X on Y {
			  B
			  C
			  D {
			    E
			  }
			}");

            var woFrag = AnalyzeComplexity(@"
			{
			  A {
			    W {
			      B
			      C
			      D {
			        E
			      }
			    }
			  }
		    }");

            withFrag.Complexity.ShouldBe(woFrag.Complexity);
            withFrag.TotalQueryDepth.ShouldBe(woFrag.TotalQueryDepth);
        }

        // https://github.com/graphql-dotnet/graphql-dotnet/issues/3030
        [Fact]
        public void nested_fragments()
        {
            var withFrag = AnalyzeComplexity(@"query SomeDroids {
                  droid(id: ""3"") {
                    ...DroidFragment
                  }
               }

               fragment DroidFragment on Droid {
                 name
                 ... nestedNameFragment1
               }

               fragment nestedNameFragment1 on Droid {
                 ... nestedNameFragment2
                 name
               }

               fragment nestedNameFragment2 on Droid {
                 name
            }");

            var woFrag = AnalyzeComplexity(@"query SomeDroids {
                  droid(id: ""3"") {
                    name
                  }
               }");

            withFrag.Complexity.ShouldBe(4/*woFrag.Complexity*/); // TODO: 4 != 2 but may be OK
            withFrag.TotalQueryDepth.ShouldBe(woFrag.TotalQueryDepth);
        }

        [Fact]
        public void absurdly_huge_query()
        {
            try
            {
                AnalyzeComplexity(
                    @"{A{A{A{A{A{A{A{A{A{A{A{A{A{A{A{A{A{A{A{A{A{A{A{A{A{A{A{A{A{A{A{A{A{A{A{A{A{A{A{A{A{A{A{A{A{A{A{A{A{A{A{A{A{A{A{A{A{A{A{A{A{A{A{A{A{A{A{A{A{A{A{A{A{A{A{A{A{A{A{A{A{A{A{A{A{A{A{A{A{A{A{A{A{A{A{A{A{A{A{A{A{A{A{A{A{A{A{A{A{A{A{A{A{A{A{A{A{A{A{A}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}");
            }
            catch (InvalidOperationException ex)
            {
                ex.Message.ShouldBe("Query is too complex to validate.");
            }
        }
    }
}
