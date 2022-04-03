using GraphQL.Validation.Errors;
using GraphQL.Validation.Rules;

namespace GraphQL.Tests.Validation;

public class NoFragmentCyclesTests : ValidationTestBase<NoFragmentCycles, ValidationSchema>
{
    [Fact]
    public void single_reference_is_valid()
    {
        ShouldPassRule(@"
              fragment fragA on Dog { ...fragB }
              fragment fragB on Dog { name }
            ");
    }

    [Fact]
    public void spread_twice_is_not_circular()
    {
        ShouldPassRule(@"
              fragment fragA on Dog { ...fragB, ...fragB }
              fragment fragB on Dog { name }
            ");
    }

    [Fact]
    public void spread_twice_indirectly_is_not_circular()
    {
        ShouldPassRule(@"
              fragment fragA on Dog { ...fragB, ...fragC }
              fragment fragB on Dog { ...fragC }
              fragment fragC on Dog { name }
            ");
    }

    [Fact]
    public void double_spread_within_abstract_types()
    {
        ShouldPassRule(@"
              fragment nameFragment on Pet {
                ... on Dog { name }
                ... on Cat { name }
              }
              fragment spreadsInAnon on Pet {
                ... on Dog { ...nameFragment }
                ... on Cat { ...nameFragment }
              }
            ");
    }

    [Fact]
    public void does_not_false_positive_on_unknown_fragment()
    {
        ShouldPassRule(@"
              fragment nameFragment on Pet {
                ...UnknownFragment
              }
            ");
    }

    [Fact]
    public void spreading_recursively_within_fields_fails()
    {
        ShouldFailRule(_ =>
        {
            _.Query = @"
                  fragment fragA on Human { relatives { ...fragA } },
                ";
            _.Error(CycleErrorMessage("fragA", Array.Empty<string>()), 2, 57);
        });
    }

    [Fact]
    public void no_spreading_itself_directly()
    {
        ShouldFailRule(_ =>
        {
            _.Query = @"
                  fragment fragA on Dog { ...fragA }
                ";
            _.Error(CycleErrorMessage("fragA", Array.Empty<string>()), 2, 43);
        });
    }

    [Fact]
    public void no_spreading_itself_directly_within_inline_fragment()
    {
        ShouldFailRule(_ =>
        {
            _.Query = @"
                  fragment fragA on Pet {
                    ... on Dog {
                      ...fragA
                    }
                  }
                ";
            _.Error(CycleErrorMessage("fragA", Array.Empty<string>()), 4, 23);
        });
    }

    [Fact]
    public void no_spreading_itself_indirectly()
    {
        ShouldFailRule(_ =>
        {
            _.Query = @"
                  fragment fragA on Dog { ...fragB }
                  fragment fragB on Dog { ...fragA }
                ";
            _.Error(e =>
            {
                e.Message = CycleErrorMessage("fragA", new[] { "fragB" });
                e.Loc(2, 43);
                e.Loc(3, 43);
            });
        });
    }

    [Fact]
    public void no_spreading_itself_indirectly_reports_opposite_order()
    {
        ShouldFailRule(_ =>
        {
            _.Query = @"
                  fragment fragB on Dog { ...fragA }
                  fragment fragA on Dog { ...fragB }
                ";
            _.Error(e =>
            {
                e.Message = CycleErrorMessage("fragB", new[] { "fragA" });
                e.Loc(2, 43);
                e.Loc(3, 43);
            });
        });
    }

    [Fact]
    public void no_spreading_itself_indirectly_within_inline_fragment()
    {
        ShouldFailRule(_ =>
        {
            _.Query = @"
                  fragment fragA on Pet {
                    ... on Dog {
                      ...fragB
                    }
                  }
                  fragment fragB on Pet {
                    ... on Dog {
                      ...fragA
                    }
                  }
                ";
            _.Error(e =>
            {
                e.Message = CycleErrorMessage("fragA", new[] { "fragB" });
                e.Loc(4, 23);
                e.Loc(9, 23);
            });
        });
    }

    [Fact]
    public void no_spreading_itself_deeply()
    {
        ShouldFailRule(_ =>
        {
            _.Query = @"
                  fragment fragA on Dog { ...fragB }
                  fragment fragB on Dog { ...fragC }
                  fragment fragC on Dog { ...fragO }
                  fragment fragX on Dog { ...fragY }
                  fragment fragY on Dog { ...fragZ }
                  fragment fragZ on Dog { ...fragO }
                  fragment fragO on Dog { ...fragP }
                  fragment fragP on Dog { ...fragA, ...fragX }
                ";
            _.Error(e =>
            {
                e.Message = CycleErrorMessage("fragA", new[] { "fragB", "fragC", "fragO", "fragP" });
                e.Loc(2, 43);
                e.Loc(3, 43);
                e.Loc(4, 43);
                e.Loc(8, 43);
                e.Loc(9, 43);
            });
            _.Error(e =>
            {
                e.Message = CycleErrorMessage("fragO", new[] { "fragP", "fragX", "fragY", "fragZ" });
                e.Loc(8, 43);
                e.Loc(9, 53);
                e.Loc(5, 43);
                e.Loc(6, 43);
                e.Loc(7, 43);
            });
        });
    }

    [Fact]
    public void no_spreading_itself_deeply_two_paths()
    {
        ShouldFailRule(_ =>
        {
            _.Query = @"
                  fragment fragA on Dog { ...fragB, ...fragC }
                  fragment fragB on Dog { ...fragA }
                  fragment fragC on Dog { ...fragA }
                ";
            _.Error(e =>
            {
                e.Message = CycleErrorMessage("fragA", new[] { "fragB" });
                e.Loc(2, 43);
                e.Loc(3, 43);
            });
            _.Error(e =>
            {
                e.Message = CycleErrorMessage("fragA", new[] { "fragC" });
                e.Loc(2, 53);
                e.Loc(4, 43);
            });
        });
    }

    [Fact]
    public void no_spreading_itself_deeply_two_paths_alt_traverse_order()
    {
        ShouldFailRule(_ =>
        {
            _.Query = @"
                  fragment fragA on Dog { ...fragC }
                  fragment fragB on Dog { ...fragC }
                  fragment fragC on Dog { ...fragA, ...fragB }
                ";
            _.Error(e =>
            {
                e.Message = CycleErrorMessage("fragA", new[] { "fragC" });
                e.Loc(2, 43);
                e.Loc(4, 43);
            });
            _.Error(e =>
            {
                e.Message = CycleErrorMessage("fragC", new[] { "fragB" });
                e.Loc(4, 53);
                e.Loc(3, 43);
            });
        });
    }

    [Fact]
    public void no_spreading_itself_deeply_and_immediately()
    {
        ShouldFailRule(_ =>
        {
            _.Query = @"
                  fragment fragA on Dog { ...fragB }
                  fragment fragB on Dog { ...fragB, ...fragC }
                  fragment fragC on Dog { ...fragA, ...fragB }
                ";
            _.Error(e =>
            {
                e.Message = CycleErrorMessage("fragB", Array.Empty<string>());
                e.Loc(3, 43);
            });
            _.Error(e =>
            {
                e.Message = CycleErrorMessage("fragA", new[] { "fragB", "fragC" });
                e.Loc(2, 43);
                e.Loc(3, 53);
                e.Loc(4, 43);
            });
            _.Error(e =>
            {
                e.Message = CycleErrorMessage("fragB", new[] { "fragC" });
                e.Loc(3, 53);
                e.Loc(4, 53);
            });
        });
    }

    private static string CycleErrorMessage(string fragName, string[] spreadNames)
        => NoFragmentCyclesError.CycleErrorMessage(fragName, spreadNames);
}
