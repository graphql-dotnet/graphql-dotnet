using GraphQL.Validation.Errors;
using GraphQL.Validation.Rules;

namespace GraphQL.Tests.Validation;

public class PossibleFragmentSpreadsTests : ValidationTestBase<PossibleFragmentSpreads, ValidationSchema>
{
    [Fact]
    public void of_the_same_object()
    {
        ShouldPassRule(@"
                fragment objectWithinObject on Dog { ...dogFragment }
                fragment dogFragment on Dog { barkVolume }
            ");
    }

    [Fact]
    public void of_the_same_object_with_inline_fragment()
    {
        ShouldPassRule(@"
                fragment objectWithinObjectAnon on Dog { ... on Dog { barkVolume } }
            ");
    }

    [Fact]
    public void object_into_an_implemented_interface()
    {
        ShouldPassRule(@"
                fragment objectWithinInterface on Pet { ...dogFragment }
                fragment dogFragment on Dog { barkVolume }
            ");
    }

    [Fact]
    public void object_into_containing_union()
    {
        ShouldPassRule(@"
                fragment objectWithinUnion on CatOrDog { ...dogFragment }
                fragment dogFragment on Dog { barkVolume }
            ");
    }

    [Fact]
    public void union_into_contained_object()
    {
        ShouldPassRule(@"
                fragment unionWithinObject on Dog { ...catOrDogFragment }
                fragment catOrDogFragment on CatOrDog { __typename }
            ");
    }

    [Fact]
    public void union_into_overlapping_interface()
    {
        ShouldPassRule(@"
                fragment unionWithinInterface on Pet { ...catOrDogFragment }
                fragment catOrDogFragment on CatOrDog { __typename }
            ");
    }

    [Fact]
    public void union_into_overlapping_union()
    {
        ShouldPassRule(@"
                fragment unionWithinUnion on DogOrHuman { ...catOrDogFragment }
                fragment catOrDogFragment on CatOrDog { __typename }
            ");
    }

    [Fact]
    public void interface_into_implemented_object()
    {
        ShouldPassRule(@"
                fragment interfaceWithinObject on Dog { ...petFragment }
                fragment petFragment on Pet { name }
            ");
    }

    [Fact]
    public void interface_into_overlapping_interface()
    {
        ShouldPassRule(@"
                fragment interfaceWithinInterface on Pet { ...beingFragment }
                fragment beingFragment on Being { name }
            ");
    }

    [Fact]
    public void interface_into_overlapping_interface_in_inline_fragment()
    {
        ShouldPassRule(@"
                fragment interfaceWithinInterface on Pet { ... on Being { name } }
            ");
    }

    [Fact]
    public void interface_into_overlapping_union()
    {
        ShouldPassRule(@"
                fragment interfaceWithinUnion on CatOrDog { ...petFragment }
                fragment petFragment on Pet { name }
            ");
    }

    [Fact]
    public void different_object_into_object()
    {
        ShouldFailRule(_ =>
        {
            _.Query = @"
                    fragment invalidObjectWithinObject on Cat { ...dogFragment }
                    fragment dogFragment on Dog { barkVolume }
                ";
            error(_, "dogFragment", "Cat", "Dog", 2, 65);
        });
    }

    [Fact]
    public void different_object_into_object_in_inline_fragment()
    {
        ShouldFailRule(_ =>
        {
            _.Query = @"
                  fragment invalidObjectWithinObjectAnon on Cat {
                    ... on Dog { barkVolume }
                  }
                ";
            errorAnon(_, "Cat", "Dog", 3, 21);
        });
    }

    [Fact]
    public void object_into_no_implementing_interface()
    {
        ShouldFailRule(_ =>
        {
            _.Query = @"
                  fragment invalidObjectWithinInterface on Pet { ...humanFragment }
                  fragment humanFragment on Human { pets { name } }
                ";
            error(_, "humanFragment", "Pet", "Human", 2, 66);
        });
    }

    [Fact]
    public void object_into_not_containing_union()
    {
        ShouldFailRule(_ =>
        {
            _.Query = @"
                  fragment invalidObjectWithinUnion on CatOrDog { ...humanFragment }
                  fragment humanFragment on Human { pets { name } }
                ";
            error(_, "humanFragment", "CatOrDog", "Human", 2, 67);
        });
    }

    [Fact]
    public void union_into_not_contained_object()
    {
        ShouldFailRule(_ =>
        {
            _.Query = @"
                  fragment invalidUnionWithinObject on Human { ...catOrDogFragment }
                  fragment catOrDogFragment on CatOrDog { __typename }
                ";
            error(_, "catOrDogFragment", "Human", "CatOrDog", 2, 64);
        });
    }

    [Fact]
    public void union_into_non_overlapping_interface()
    {
        ShouldFailRule(_ =>
        {
            _.Query = @"
                  fragment invalidUnionWithinInterface on Pet { ...humanOrAlienFragment }
                  fragment humanOrAlienFragment on HumanOrAlien { __typename }
                ";
            error(_, "humanOrAlienFragment", "Pet", "HumanOrAlien", 2, 65);
        });
    }

    [Fact]
    public void union_into_non_overlapping_union()
    {
        ShouldFailRule(_ =>
        {
            _.Query = @"
                  fragment invalidUnionWithinUnion on CatOrDog { ...humanOrAlienFragment }
                  fragment humanOrAlienFragment on HumanOrAlien { __typename }
                ";
            error(_, "humanOrAlienFragment", "CatOrDog", "HumanOrAlien", 2, 66);
        });
    }

    [Fact]
    public void interface_into_non_implementing_object()
    {
        ShouldFailRule(_ =>
        {
            _.Query = @"
                  fragment invalidInterfaceWithinObject on Cat { ...intelligentFragment }
                  fragment intelligentFragment on Intelligent { iq }
                ";
            error(_, "intelligentFragment", "Cat", "Intelligent", 2, 66);
        });
    }

    [Fact]
    public void interface_into_non_overlapping_interface()
    {
        ShouldFailRule(_ =>
        {
            _.Query = @"
                  fragment invalidInterfaceWithinInterface on Pet {
                    ...intelligentFragment
                  }
                  fragment intelligentFragment on Intelligent { iq }
                ";
            error(_, "intelligentFragment", "Pet", "Intelligent", 3, 21);
        });
    }

    [Fact]
    public void interface_into_non_overlapping_interface_in_inline_fragment()
    {
        ShouldFailRule(_ =>
        {
            _.Query = @"
                  fragment invalidInterfaceWithinInterfaceAnon on Pet {
                    ...on Intelligent { iq }
                  }
                ";
            errorAnon(_, "Pet", "Intelligent", 3, 21);
        });
    }

    [Fact]
    public void interface_into_non_overlapping_union()
    {
        ShouldFailRule(_ =>
        {
            _.Query = @"
                  fragment invalidInterfaceWithinUnion on HumanOrAlien { ...petFragment }
                  fragment petFragment on Pet { name }
                ";
            error(_, "petFragment", "HumanOrAlien", "Pet", 2, 74);
        });
    }

    private void error(ValidationTestConfig _, string fragName, string parentType, string fragType, int line, int column)
    {
        _.Error(PossibleFragmentSpreadsError.TypeIncompatibleSpreadMessage(fragName, parentType, fragType), line, column);
    }

    private void errorAnon(ValidationTestConfig _, string parentType, string fragType, int line, int column)
    {
        _.Error(PossibleFragmentSpreadsError.TypeIncompatibleAnonSpreadMessage(parentType, fragType), line, column);
    }
}
