# Protection Against Malicious Queries

GraphQL allows the client to bundle and nest many queries into a single request. While this
is quite convenient it also makes GraphQL endpoints susceptible to Denial of Service attacks.

To mitigate this graphql-dotnet provides a few options that can be tweaked to set the upper
bound of nesting and complexity of incoming queries so that the endpoint would only try to
resolve queries that meet the set criteria and discard any overly complex and possibly
malicious query that you don't expect your clients to make thus protecting your server
resources against depletion by a denial of service attacks.

`GraphQL.Validation.Complexity.ComplexityConfiguration` class represents these options
that are used by `ComplexityValidationRule`. The available options are the following:

```csharp
public class ComplexityConfiguration
{
    public int? MaxDepth { get; set; }
    public int? MaxComplexity { get; set; }
    public double? FieldImpact { get; set; }
    public int MaxRecursionCount { get; set; }
}
```

The easiest way to configure complexity checks for your schema is the following:

```csharp
IServiceCollection services = ...;
services.AddGraphQL(builder => builder
    .AddSchema<ComplexitySchema>()
    .AddComplexityAnalyzer(opt => opt.MaxComplexity = 200));
```

`MaxDepth` will enforce the total maximum nesting across all queries in a request.
For example the following query will have a query depth of 2. Note that fragments
are taken into consideration when making these calculations.

```graphql
{
  Product {  # This query has a depth of 2 which loosely translates to two distinct queries
  			 # to the datasource, first one to return the list of products and second
             # one (which will be executed once for each returned product) to grab
             # the product's first 3 locations.
    Title
    ...X  # The depth of this fragment is calculated first and added to the total.
  }
}

fragment X on Product { # This fragment has a depth of only 1.
  Location(first: 3) {
    lat
    long
  }
}
```

The query depth setting is a good estimation of complexity for most use cases and it loosely
translates to the number of unique queries sent to the datastore (however it does not look
at how many times each query might get executed). Keep in mind that the calculation of complexity
needs to be FAST otherwise it can impose a significant overhead.

One step further would be specifying `MaxComplexity` and `FieldImpact` to look at the estimated
number of entities (or cells in a database) that are expected to be returned by each query.
Obviously this depends on the size of your database (i.e. number of records per entity) so you
will need to find the average number of records per database entity and input that into `FieldImpact`.
For example if I have 3 tables with 100, 120 and 98 rows and I know I will be querying the first
table twice as much then a good estimation for `avgImpact` would be 105.

Note: I highly recommend setting a higher bound on the number of returned entities by each
resolve function in your code. if you use this approach already in your code then you can
input that upper bound (which would be the maximum possible items returned per entity) as
your avgImpact. It is also possible to use a theoretical value for this (for example 2.0)
to asses the query's impact on a theoretical database hence decoupling this calculation
from your actual database.

Imagine if we had a simple test database for the query in the previous example and we assume
an average impact of 2.0 (each entity will return ~2 results) then we can calculate the complexity
as following:

```math
2 Products returned + 2 * (1 * Title per Product) + 2 * [ (3 * Locations) + (3 * lat entries) + (3 * long entries) ] = **22**
```

Or simply put on average we will have **2x Products** each will have 1 Title for a total of
**2x Titles** plus per each Product entry we will have 3 locations overridden by `first`
argument (we follow relay's spec for `first`, `last` and `id` arguments) and each of these
3 locations have a lat and a long totalling **6x Locations** having **6x lat**s and **6x longs**.

Now if we set the `avgImpact` to 2.0 and set the `MaxComplexity` to 23 (or higher) the query
will execute correctly. If we change the `MaxComplexity` to something like 20 the DocumentExecutor
will fail right after parsing the AST tree and will not attempt to resolve any of the fields
(or talk to the database).
