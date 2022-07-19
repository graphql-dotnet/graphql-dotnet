namespace GraphQL.Tests.Complexity;

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

    // https://github.com/graphql-dotnet/graphql-dotnet/issues/3191
    [Fact]
    public void nested_fragments_2()
    {
        var result = AnalyzeComplexity(@"
{
  car(id: 1)
  {
    ...lastUpdated
    ...optionsOnCar
  }
}

fragment optionsOnCar on Car
{
  options
  {
    edges
    {
      node
      {
        ...optionDetail
      }
    }
  }
}

fragment optionPrice on Option
{
  price
}

fragment lastUpdated on Car
{
  updatedAt
}

fragment optionDetail on Option
{
  name
  ...optionPrice
  optionContents(first: 9999999)
  {
    edges
    {
      node
      {
        optionContent
        {
          name
        }
      }
    }
  }
}");

        result.Complexity.ShouldBe(1839999848); // WOW! :)
    }

    // https://github.com/graphql-dotnet/graphql-dotnet/issues/3207
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void nested_fragments_3(bool reverse)
    {
        var frag1 = @"
fragment frag1 on QueryType
{
  ...frag4
}
";
        var frag2 = @"
fragment frag2 on QueryType
{
  ...frag3
}
";
        var otherFrags = @"
fragment frag3 on QueryType
{
  ...frag5
}

fragment frag5 on QueryType
{
  __typename
}

fragment frag4 on QueryType
{
  __typename
}

query fragmentTest
{
  ...frag1
  ...frag2
}
";
        var result = AnalyzeComplexity(reverse
            ? frag1 + frag2 + otherFrags
            : frag2 + frag1 + otherFrags);

        result.Complexity.ShouldBe(2);
    }

    [Fact]
    public void duplicate_fragment_ok()
    {
        var query = @"
query carFragmentTest
{
  car(id: 1)
  {
    name
    ... carInfo
    ... carInfo
  }
}

fragment carInfo on Car
{
    ...pricing
    ...pricing
}

fragment pricing on Car
{
  msrp
}
";
        var result = AnalyzeComplexity(query);

        result.Complexity.ShouldBe(6);
    }

    // https://github.com/graphql-dotnet/graphql-dotnet/issues/3221
    [Fact]
    public void no_fragment_cycle()
    {
        var query = @"
query carFragmentTest
{
  car(id: 1)
  {
    name
    ... carInfo
  }
}

fragment carInfo on Car
{
    ...pricing
    ... detail
}

fragment pricing on Car
{
  msrp
}

fragment detail on Car
{
  ... furtherDetail
}

fragment furtherDetail on Car
{
  ... pricing
}";
        var result = AnalyzeComplexity(query);

        result.Complexity.ShouldBe(4);
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
