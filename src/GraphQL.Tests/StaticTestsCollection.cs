namespace GraphQL.Tests;

// decorate any test classes containing tests that modify global variables with
//   [Collection("StaticTests")]
// these tests will run sequentially after all other tests are complete
// be sure to restore the global variables to their initial state with a finally block

[CollectionDefinition("StaticTests", DisableParallelization = true)]
public class StaticTestsCollection
{
}
