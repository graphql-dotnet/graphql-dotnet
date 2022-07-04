namespace GraphQL.Tests.Complexity;

public class ComplexityBasicTests : ComplexityTestBase
{
    [Fact]
    public void empty_query_complexity()
    {
        var res = AnalyzeComplexity("");
        res.ComplexityMap.Count.ShouldBe(0);
        res.Complexity.ShouldBe(0);
        res.TotalQueryDepth.ShouldBe(0);
    }

    [Fact]
    public void zero_depth_query()
    {
        var res = AnalyzeComplexity(@"
query {
  A #1
}");
        res.TotalQueryDepth.ShouldBe(0);
        res.Complexity.ShouldBe(1);
    }

    [Fact]
    public void one_depth_query_A()
    {
        var res = AnalyzeComplexity(@"
query {
  A { #2
    B #2
  }
}");
        res.TotalQueryDepth.ShouldBe(1);
        res.Complexity.ShouldBe(4);
    }

    [Fact]
    public void one_depth_query_B()
    {
        var res = AnalyzeComplexity(@"
query {
  A { #2
    B #2
    C #2
    D #2
  }
}");
        res.TotalQueryDepth.ShouldBe(1);
        res.Complexity.ShouldBe(8);
    }

    [Fact]
    public void two_depth_query_A()
    {
        var res = AnalyzeComplexity(@"
query {
  A {   #2
    B { #4
      C #4
    }
  }
}");
        res.TotalQueryDepth.ShouldBe(2);
        res.Complexity.ShouldBe(10);
    }

    [Fact]
    public void two_depth_query_B()
    {
        var res = AnalyzeComplexity(@"
query {
  F     #1
  A {   #2
    B   #2
    D { #4
      C #4
      E #4
    }
  }
}");
        res.TotalQueryDepth.ShouldBe(2);
        res.Complexity.ShouldBe(17);
    }

    [Fact]
    public void three_depth_query()
    {
        var res = AnalyzeComplexity(@"
query {
  A {     #2
    B {   #4
      C { #8
        D #8
      }
    }
  }
}");
        res.TotalQueryDepth.ShouldBe(3);
        res.Complexity.ShouldBe(22);
    }

    [Fact]
    public void three_depth_query_wide()
    {
        var res = AnalyzeComplexity(@"
query {
  A { #2
    B #2
  }
  C { #2
    D #2
  }
  E { #2
    F #2
  }
}");
        res.TotalQueryDepth.ShouldBe(3);
        res.Complexity.ShouldBe(12);
    }
}
