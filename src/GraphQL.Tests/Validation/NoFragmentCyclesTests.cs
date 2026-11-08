using System.Text;
using GraphQL.Validation;
using GraphQL.Validation.Errors;
using GraphQL.Validation.Rules;
using GraphQLParser;

namespace GraphQL.Tests.Validation;

public class NoFragmentCyclesTests : ValidationTestBase<NoFragmentCycles, ValidationSchema>
{
    private const int DeepFragmentCount = 100_000;

    [Fact]
    public async Task reports_disconnected_cycles_without_returning_a_visitor()
    {
        var context = new ValidationContext
        {
            Document = Parser.Parse("""
                fragment unused on Dog { name }
                fragment fragA on Dog { ...fragB }
                fragment fragB on Dog { ...fragA }
                fragment fragC on Dog { ...fragC }
                """),
        };

        var visitor = await NoFragmentCycles.Instance.GetPreNodeVisitorAsync(context);

        visitor.ShouldBeNull();
        var errors = context.Errors.ToArray();
        errors.Select(x => x.Message).ShouldBe([
            CycleErrorMessage("fragA", ["fragB"]),
            CycleErrorMessage("fragC", [])
        ]);
        errors[0].Locations!.Select(x => (x.Line, x.Column)).ShouldBe([(2, 25), (3, 25)]);
        errors[1].Locations!.Select(x => (x.Line, x.Column)).ShouldBe([(4, 25)]);
    }

    [Fact]
    public async Task no_fragments_returns_no_visitor_or_errors()
    {
        var context = new ValidationContext { Document = Parser.Parse("query { name }") };

        var visitor = await NoFragmentCycles.Instance.GetPreNodeVisitorAsync(context);

        visitor.ShouldBeNull();
        context.Errors.ShouldBeEmpty();
    }

    [Fact]
    public async Task canceled_context_does_not_evaluate_cycles()
    {
        var context = new ValidationContext
        {
            Document = Parser.Parse("fragment fragA on Dog { ...fragA }"),
            CancellationToken = new CancellationToken(true),
        };

        var exception = await Should.ThrowAsync<OperationCanceledException>(
            () => NoFragmentCycles.Instance.GetPreNodeVisitorAsync(context).AsTask());

        exception.CancellationToken.ShouldBe(context.CancellationToken);
        context.Errors.ShouldBeEmpty();
    }

    [Fact]
    public void deep_acyclic_chain_is_valid()
    {
        ShouldPassRule(CreateDeepFragmentChain(cycle: false));
    }

    [Fact]
    public void deep_chain_reports_terminal_cycle()
    {
        ShouldFailRule(_ =>
        {
            _.Query = CreateDeepFragmentChain(cycle: true);
            _.Error(e =>
            {
                e.Message = CycleErrorMessage($"frag{DeepFragmentCount - 2}", [$"frag{DeepFragmentCount - 1}"]);
                e.Loc(DeepFragmentCount - 1, 29);
                e.Loc(DeepFragmentCount, 29);
            });
        });
    }

    [Fact]
    public void single_reference_is_valid()
    {
        ShouldPassRule("""
              fragment fragA on Dog { ...fragB }
              fragment fragB on Dog { name }
            """);
    }

    [Fact]
    public void spread_twice_is_not_circular()
    {
        ShouldPassRule("""
              fragment fragA on Dog { ...fragB, ...fragB }
              fragment fragB on Dog { name }
            """);
    }

    [Fact]
    public void spread_twice_indirectly_is_not_circular()
    {
        ShouldPassRule("""
              fragment fragA on Dog { ...fragB, ...fragC }
              fragment fragB on Dog { ...fragC }
              fragment fragC on Dog { name }
            """);
    }

    [Fact]
    public void double_spread_within_abstract_types()
    {
        ShouldPassRule("""
              fragment nameFragment on Pet {
                ... on Dog { name }
                ... on Cat { name }
              }
              fragment spreadsInAnon on Pet {
                ... on Dog { ...nameFragment }
                ... on Cat { ...nameFragment }
              }
            """);
    }

    [Fact]
    public void does_not_false_positive_on_unknown_fragment()
    {
        ShouldPassRule("""
              fragment nameFragment on Pet {
                ...UnknownFragment
              }
            """);
    }

    [Fact]
    public void duplicate_names_use_first_fragment_definition()
    {
        ShouldPassRule("""
              fragment fragA on Dog { ...fragB }
              fragment fragB on Dog { name }
              fragment fragB on Dog { ...fragA }
            """);
    }

    [Fact]
    public void spreading_recursively_within_fields_fails()
    {
        ShouldFailRule(_ =>
        {
            _.Query = """
                  fragment fragA on Human { relatives { ...fragA } },
                """;
            _.Error(CycleErrorMessage("fragA", Array.Empty<string>()), 1, 41);
        });
    }

    [Fact]
    public void no_spreading_itself_directly()
    {
        ShouldFailRule(_ =>
        {
            _.Query = """
                  fragment fragA on Dog { ...fragA }
                """;
            _.Error(CycleErrorMessage("fragA", Array.Empty<string>()), 1, 27);
        });
    }

    [Fact]
    public void no_spreading_itself_directly_within_inline_fragment()
    {
        ShouldFailRule(_ =>
        {
            _.Query = """
                  fragment fragA on Pet {
                    ... on Dog {
                      ...fragA
                    }
                  }
                """;
            _.Error(CycleErrorMessage("fragA", Array.Empty<string>()), 3, 7);
        });
    }

    [Fact]
    public void no_spreading_itself_indirectly()
    {
        ShouldFailRule(_ =>
        {
            _.Query = """
                  fragment fragA on Dog { ...fragB }
                  fragment fragB on Dog { ...fragA }
                """;
            _.Error(e =>
            {
                e.Message = CycleErrorMessage("fragA", new[] { "fragB" });
                e.Loc(1, 27);
                e.Loc(2, 27);
            });
        });
    }

    [Fact]
    public void no_spreading_itself_indirectly_reports_opposite_order()
    {
        ShouldFailRule(_ =>
        {
            _.Query = """
                  fragment fragB on Dog { ...fragA }
                  fragment fragA on Dog { ...fragB }
                """;
            _.Error(e =>
            {
                e.Message = CycleErrorMessage("fragB", new[] { "fragA" });
                e.Loc(1, 27);
                e.Loc(2, 27);
            });
        });
    }

    [Fact]
    public void no_spreading_itself_indirectly_within_inline_fragment()
    {
        ShouldFailRule(_ =>
        {
            _.Query = """
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
                """;
            _.Error(e =>
            {
                e.Message = CycleErrorMessage("fragA", new[] { "fragB" });
                e.Loc(3, 7);
                e.Loc(8, 7);
            });
        });
    }

    [Fact]
    public void no_spreading_itself_deeply()
    {
        ShouldFailRule(_ =>
        {
            _.Query = """
                  fragment fragA on Dog { ...fragB }
                  fragment fragB on Dog { ...fragC }
                  fragment fragC on Dog { ...fragO }
                  fragment fragX on Dog { ...fragY }
                  fragment fragY on Dog { ...fragZ }
                  fragment fragZ on Dog { ...fragO }
                  fragment fragO on Dog { ...fragP }
                  fragment fragP on Dog { ...fragA, ...fragX }
                """;
            _.Error(e =>
            {
                e.Message = CycleErrorMessage("fragA", new[] { "fragB", "fragC", "fragO", "fragP" });
                e.Loc(1, 27);
                e.Loc(2, 27);
                e.Loc(3, 27);
                e.Loc(7, 27);
                e.Loc(8, 27);
            });
            _.Error(e =>
            {
                e.Message = CycleErrorMessage("fragO", new[] { "fragP", "fragX", "fragY", "fragZ" });
                e.Loc(7, 27);
                e.Loc(8, 37);
                e.Loc(4, 27);
                e.Loc(5, 27);
                e.Loc(6, 27);
            });
        });
    }

    [Fact]
    public void no_spreading_itself_deeply_two_paths()
    {
        ShouldFailRule(_ =>
        {
            _.Query = """
                  fragment fragA on Dog { ...fragB, ...fragC }
                  fragment fragB on Dog { ...fragA }
                  fragment fragC on Dog { ...fragA }
                """;
            _.Error(e =>
            {
                e.Message = CycleErrorMessage("fragA", new[] { "fragB" });
                e.Loc(1, 27);
                e.Loc(2, 27);
            });
            _.Error(e =>
            {
                e.Message = CycleErrorMessage("fragA", new[] { "fragC" });
                e.Loc(1, 37);
                e.Loc(3, 27);
            });
        });
    }

    [Fact]
    public void no_spreading_itself_deeply_two_paths_alt_traverse_order()
    {
        ShouldFailRule(_ =>
        {
            _.Query = """
                  fragment fragA on Dog { ...fragC }
                  fragment fragB on Dog { ...fragC }
                  fragment fragC on Dog { ...fragA, ...fragB }
                """;
            _.Error(e =>
            {
                e.Message = CycleErrorMessage("fragA", new[] { "fragC" });
                e.Loc(1, 27);
                e.Loc(3, 27);
            });
            _.Error(e =>
            {
                e.Message = CycleErrorMessage("fragC", new[] { "fragB" });
                e.Loc(3, 37);
                e.Loc(2, 27);
            });
        });
    }

    [Fact]
    public void no_spreading_itself_deeply_and_immediately()
    {
        ShouldFailRule(_ =>
        {
            _.Query = """
                  fragment fragA on Dog { ...fragB }
                  fragment fragB on Dog { ...fragB, ...fragC }
                  fragment fragC on Dog { ...fragA, ...fragB }
                """;
            _.Error(e =>
            {
                e.Message = CycleErrorMessage("fragB", Array.Empty<string>());
                e.Loc(2, 27);
            });
            _.Error(e =>
            {
                e.Message = CycleErrorMessage("fragA", new[] { "fragB", "fragC" });
                e.Loc(1, 27);
                e.Loc(2, 37);
                e.Loc(3, 27);
            });
            _.Error(e =>
            {
                e.Message = CycleErrorMessage("fragB", new[] { "fragC" });
                e.Loc(2, 37);
                e.Loc(3, 37);
            });
        });
    }

    private static string CreateDeepFragmentChain(bool cycle)
    {
        // Each definition is shallow; only following references creates a deep traversal.
        // A fragments-only document isolates this rule from operation validation.
        var query = new StringBuilder();
        for (int i = 0; i < DeepFragmentCount - 1; ++i)
            query.AppendLine($"fragment frag{i} on Dog {{ ...frag{i + 1} }}");
        query.Append($"fragment frag{DeepFragmentCount - 1} on Dog {{ ")
            .Append(cycle ? $"...frag{DeepFragmentCount - 2}" : "name")
            .Append(" }");
        return query.ToString();
    }

    private static string CycleErrorMessage(string fragName, string[] spreadNames)
        => NoFragmentCyclesError.CycleErrorMessage(fragName, spreadNames);
}
