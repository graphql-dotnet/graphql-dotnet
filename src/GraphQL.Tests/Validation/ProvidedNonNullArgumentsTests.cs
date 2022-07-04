using GraphQL.Validation.Errors;
using GraphQL.Validation.Rules;

namespace GraphQL.Tests.Validation;

public class ProvidedNonNullArgumentsTests : ValidationTestBase<ProvidedNonNullArguments, ValidationSchema>
{
    [Fact]
    public void ignores_unknown_arguments()
    {
        ShouldPassRule(@"
              {
                dog {
                  isHousetrained(unknownArgument: true)
                }
              }
            ");
    }

    [Fact]
    public void arg_on_optional_arg()
    {
        ShouldPassRule(@"
            {
              dog {
                isHousetrained(atOtherHomes: true)
              }
            }
            ");
    }

    [Fact]
    public void no_arg_on_optional_arg()
    {
        ShouldPassRule(@"
            {
              dog {
                isHousetrained
              }
            }
            ");
    }

    [Fact]
    public void multiple_args()
    {
        ShouldPassRule(@"
            {
              complicatedArgs {
                multipleReqs(req1: 1, req2: 2)
              }
            }
            ");
    }

    [Fact]
    public void multiple_args_reverse_order()
    {
        ShouldPassRule(@"
            {
              complicatedArgs {
                multipleReqs(req2: 2, req1: 1)
              }
            }
            ");
    }

    [Fact]
    public void no_args_on_multiple_optional()
    {
        ShouldPassRule(@"
            {
              complicatedArgs {
                multipleOpts
              }
            }
            ");
    }

    [Fact]
    public void one_arg_on_multiple_optional()
    {
        ShouldPassRule(@"
            {
              complicatedArgs {
                multipleOpts(opt1: 1)
              }
            }
            ");
    }

    [Fact]
    public void second_arg_on_multiple_optional()
    {
        ShouldPassRule(@"
            {
              complicatedArgs {
                multipleOpts(opt2: 2)
              }
            }
            ");
    }

    [Fact]
    public void multiple_reqs_on_mixed_list()
    {
        ShouldPassRule(@"
            {
              complicatedArgs {
                multipleOptAndReq(req1: 3, req2: 4)
              }
            }
            ");
    }

    [Fact]
    public void multiple_reqs_and_one_opt_on_mixed_list()
    {
        ShouldPassRule(@"
            {
              complicatedArgs {
                multipleOptAndReq(req1: 3, req2: 4, opt1: 5)
              }
            }
            ");
    }

    [Fact]
    public void all_reqs_and_opts_on_mixed_list()
    {
        ShouldPassRule(@"
            {
              complicatedArgs {
                multipleOptAndReq(req1: 3, req2: 4, opt1: 5, opt2: 6)
              }
            }
            ");
    }

    [Fact]
    public void missing_one_non_null_argument()
    {
        ShouldFailRule(_ =>
        {
            _.Query = @"{
                  complicatedArgs {
                    multipleReqs(req2: 2)
                  }
                }";

            missingFieldArg(_, "multipleReqs", "req1", "Int!", 3, 21);
        });
    }

    [Fact]
    public void missing_multiple_non_null_argument()
    {
        ShouldFailRule(_ =>
        {
            _.Query = @"{
                  complicatedArgs {
                    multipleReqs
                  }
                }";

            missingFieldArg(_, "multipleReqs", "req1", "Int!", 3, 21);
            missingFieldArg(_, "multipleReqs", "req2", "Int!", 3, 21);
        });
    }

    [Fact]
    public void incorrect_value_and_missing_argument()
    {
        ShouldFailRule(_ =>
        {
            _.Query = @"{
                  complicatedArgs {
                    multipleReqs(req1: ""one"")
                  }
                }";

            missingFieldArg(_, "multipleReqs", "req2", "Int!", 3, 21);
        });
    }

    [Fact]
    public void ignores_unknown_directives()
    {
        ShouldPassRule(@"
            {
              dog @unknown
            }
            ");
    }

    [Fact]
    public void with_directives_of_valid_types()
    {
        ShouldPassRule(@"
            {
              dog @include(if: true) {
                name
              }
              human @skip(if: false) {
                name
              }
            }
            ");
    }

    [Fact]
    public void directive_with_missing_types()
    {
        ShouldFailRule(_ =>
        {
            _.Query = @"
                {
                  dog @include {
                    name @skip
                  }
                }";

            missingDirectiveArg(_, "include", "if", "Boolean!", 3, 23);
            missingDirectiveArg(_, "skip", "if", "Boolean!", 4, 26);
        });
    }

    private void missingFieldArg(
        ValidationTestConfig _,
        string fieldName,
        string argName,
        string typeName,
        int line,
        int column)
    {
        _.Error(ProvidedNonNullArgumentsError.MissingFieldArgMessage(fieldName, argName, typeName), line, column);
    }

    private void missingDirectiveArg(
        ValidationTestConfig _,
        string directiveName,
        string argName,
        string typeName,
        int line,
        int column)
    {
        _.Error(ProvidedNonNullArgumentsError.MissingDirectiveArgMessage(directiveName, argName, typeName), line, column);
    }
}
