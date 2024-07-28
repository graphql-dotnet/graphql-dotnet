# Complexity Analyzer

## Overview

The Complexity Analyzer in GraphQL.NET is a powerful tool designed to manage the complexity and depth of GraphQL queries.
It ensures that queries remain within acceptable bounds to prevent excessive load on the server. This documentation will
guide you through the basic and advanced configuration of the complexity analyzer.

### Key Features

- Fields can define the impact for the execution of that field (e.g., how long it will take to execute the ResolveAsync
  method) separate from a multiplier applied to the children fields (e.g., how many rows it returns for list fields).
- Default configuration allows for setting three default values:
  - Scalar impact (impact to use for scalar fields)
  - Object impact (impact to use for object fields)
  - Default child multiplier for list fields (multiplier to use for list fields)
- Per-field configurable behavior to determine field impact and child impact multipliers; by default it uses the
  scalar impact for scalar fields, object impact for object fields, and the default child multiplier for list fields.
- Default behavior considers connection semantics, such as `first`, `last`, and `id` arguments, to adjust the child
  multiplier accordingly.
- Configuring a schema to ignore complexity for introspection fields is straightforward.
- Easy to write asynchronous code to implement per-user, per-IP, or throttling limits.

## Basic Configuration

### Setting Up Complexity Analyzer

```csharp
services.AddGraphQL(b => b
    .AddSchema<MySchema>()
    .AddComplexityAnalyzer(c => {
        c.MaxDepth = 10;
        c.MaxComplexity = 100;
    })
);
```

### Configurable Options

| Option                          | Description                                                    | Default Value |
|---------------------------------|----------------------------------------------------------------|---------------|
| MaxDepth                        | Limits the maximum depth of a query.                           | null          |
| MaxComplexity                   | Limits the total complexity of a query.                        | null          |
| DefaultScalarImpact             | Specifies the default complexity impact for scalar fields.     | 1             |
| DefaultObjectImpact             | Specifies the default complexity impact for object fields.     | 1             |
| DefaultListImpactMultiplier     | Specifies the average number of items returned by list fields. | 20            |
| ValidateComplexityDelegate      | Allows for custom validation and logging based on query complexity and depth. | null |
| DefaultComplexityImpactDelegate | Provides a default mechanism to calculate field impact and child impact multipliers. | see below |

### Default Complexity Impact Delegate

The `DefaultComplexityImpactDelegate` is a built-in mechanism in GraphQL.NET that provides a default way to calculate
the complexity impact of fields within a query. By default, this delegate assigns a complexity impact based on the type
of the field being resolved. Scalar fields are given a default impact defined by `DefaultScalarImpact`, while object
fields are assigned an impact defined by `DefaultObjectImpact`. For list fields, the delegate multiplies the impact
by the `DefaultListImpactMultiplier`, unless a specific argument like `first`, `last`, or `id` is provided, which
then adjusts the multiplier accordingly (set to 1 if the `id` argument is present). The delegate also considers
connection semantics, ensuring that the impact is accurately reflected based on parent and child relationships
within the query. This default behavior ensures a logical and consistent calculation of query complexity, making
it easier to manage and limit query depth and execution cost.

#### Sample GraphQL Request and Computed Complexity

The below sample assumes that the complexity analyzer is configured with the default values.

```graphql
query {                #  impact   multiplier   total impact   child multiplier   depth
  users(first: 10) {   #     1          1             1                 10          1
    id                 #     1         10            11                             2
    posts {            #     1         10            21                 20          2
      id               #     1        200           221                             3
      comments {       #     1        200           421                 20          3
        id             #     1       4000          4421                             4
      }                #
    }                  #
  }                    #
  products(id: "5") {  #     1          1          4422                  1          1
    id                 #     1          1          4423                             2
    name               #     1          1          4424                             2
    photos {           #     1          1          4425                 20          2
      id               #     1         20          4445                             3
      name             #     1         20          4465                             3
    }                  #
    category {         #     1          1          4466                  1          2
      id               #     1          1          4467                             3
      name             #     1          1          4468                             3
    }
  }
}
```

The above query will have the following complexity calculation:
- Maximum Depth: 4 (users -> posts -> comments -> id)
- Total Complexity: 4468

These values are calculated based on these facts demonstrated in the above query:
- The `users` field requested 10 items, so the child multiplier is set to 10.
- The `posts` field is a list field and uses the default child multiplier of 20.
- The `comments` field is a list field and uses the default child multiplier of 20.
- The `products` field has an `id` argument, so the child multiplier is set to 1.
- The `photos` field is a list field and uses the default child multiplier of 20.
- The `category` field is not a list field and so does not use the default child multiplier.
- Other fields are scalar fields and use the default scalar impact of 1, multiplied by the
  multiplier calculated for that level of the graph.

## Example Scenarios

### 1. Estimating the Total Number of Nodes and Maximum Depth

To configure the complexity analyzer to estimate the total number of nodes returned and/or the maximum depth,
you can use the default configuration, or customize the default impact multiplier, or customize the impact
multiplier used for specific fields. The default configuration assumes that list fields return an average of 20 items.

#### Configuring Child Impact Multiplier for Specific Fields

```csharp
usersField.WithComplexityImpact(
    fieldImpact: 1,
    childImpactMultiplier: 100); // Assume the users field returns 100 items on average
```

#### Setting a Global Default Multiplier

```csharp
complexityConfig.DefaultListImpactMultiplier = 7; // Assume that other list fields return 7 items on average
```

#### Sample GraphQL Request and Computed Complexity

```graphql
query {            #  impact   multiplier   total impact   child multiplier   depth
  users {          #     1          1             1                100          1
    id             #     1        100           101                             2
    posts {        #     1        100           201                  7          2
      id           #     1        700           901                             3
      comments {   #     1        700          1601                  7          3
        id         #     1       4900          6501                             4
      }
    }
  }
}
```

The above query will have the following complexity calculation:
- Maximum Depth: 4 (users -> posts -> comments -> id)
- Total Complexity: 6501

Since the number of rows returned from list fields can vary, it is recommended to use connection fields
and to require the `first` or `last` argument to allow the complexity analzyer to properly estimate the
child multiplier for list fields (or have the default page size set very small). You can also choose to
set the scalar and object impact to zero if you prefer to only consider the number of nodes and maximum
depth, similar to the [GitHub GraphQL API rate limits](https://docs.github.com/en/graphql/overview/rate-limits-and-node-limits-for-the-graphql-api).

### 2. Ignoring or Reducing Impact of Introspection Requests

To prevent introspection requests from affecting the complexity calculation, you can configure the introspection
fields' impact and child multiplier. An extension method is provided to simplify this configuration as shown below:

```csharp
schema.WithIntrospectionComplexityImpact(0); // Ignore introspection fields
// or
schema.WithIntrospectionComplexityImpact(0.1); // Reduce impact to 10%
```

The above method sets the complexity impact and child multiplier for the three meta-fields to the provided value,
effectively ignoring or reducing the impact of introspection requests on the complexity calculation.

#### Sample GraphQL Request and Computed Complexity

```graphql
{
  __schema {
    types {
      name
      fields {
        name
      }
    }
  }
}
```

The above query will have the following complexity calculation:
- Maximum Depth: 4 (schema -> types -> fields -> name)
- Total Complexity: 0

Please note that the maximum depth calculation will still include introspection fields.

To ignore introspection fields from the maximum depth calculation, you can write a custom
complexity validation delegate to ignore depth limits for introspection requests:

```csharp
complexityConfig.ValidateComplexityDelegate = async (context) =>
{
    if (IsIntrospectionRequest(context.ValidationContext))
    {
        context.Error = null; // ignore complexity errors
    }

    static bool IsIntrospectionRequest(ValidationContext validationContext)
    {
        return validationContext.Document.Definitions.OfType<GraphQLOperationDefinition>().All(
            op => op.Operation == OperationType.Query && op.SelectionSet.Selections.All(
                node => node is GraphQLField field && (field.Name.Value == "__schema" || field.Name.Value == "__type")));
    }
};
```

### 3. Estimating Computing Power (Database Processing Time)

Another use case for the complexity analyzer is to estimate the computing power required to process a query.
You can configure the impact for object fields to estimate the database processing time by setting a custom default
object impact or configuring the impact for specific fields. The below examples assume that the scalar impact is 1,
but you may wish to adjust this to zero if scalar fields do not require consequential processing time.

#### Configuring Impact for Object Fields
```csharp
// Set higher impact for field resolvers that require more processing time
usersField.WithComplexityImpact(fieldImpact: 50);
```

#### Setting a Custom Default Object Impact
```csharp
// Set default for object fields (assumed to need to load from a database)
complexityConfig.DefaultObjectImpact = 20;
```

#### Sample GraphQL Request and Computed Complexity

```graphql
query {            #  impact   multiplier   total impact   child multiplier   depth
  users {          #    50          1            50                 20          1
    id             #     1         20            70                             2
    posts {        #    20         20           470                 20          2
      id           #     1        400           870                             3
      comments {   #    20        400          8870                 20          3
        id         #     1       8000         16870                             4
      }
    }
  }
}
```

The above query will have the following complexity calculation:
- Maximum Depth: 4 (users -> posts -> comments -> id)
- Total Complexity: 16870

### 4. Logging Complexity Results

In addition to validation, the `ValidateComplexityDelegate` property allows you to log complexity results
for monitoring or analysis.

```csharp
complexityConfig.ValidateComplexityDelegate = async (context) =>
{
    // RequestServices may be used to access scoped services within the DI container
    var logger = context.ValidationContext.RequestServices!.GetRequiredService<ILogger<MySchema>>();
    if (context.Error != null) // failed complexity limits
        logger.LogWarning($"Query Complexity: {context.TotalComplexity}, Depth: {context.MaxDepth}");
    else
        logger.LogInformation($"Query Complexity: {context.TotalComplexity}, Depth: {context.MaxDepth}");
};
```

### 5. Throttling Users Based on Complexity Analysis

To throttle users on a per-user basis similar to GitHub's GraphQL API limits, configure the
complexity analyzer with a custom validation delegate. As noted above, `MaxComplexity` and `MaxDepth`,
if set, are still enforced before this delegate runs.

```csharp
complexityConfig.ValidateComplexityDelegate = async (context) =>
{
    // Skip throttling if the query has already exceeded complexity limits
    if (context.Error != null)
        return;

    var services = context.ValidationContext.RequestServices!;

    // Get the authenticated user, or use the IP address if unauthenticated
    var user = context.User;
    string key;
    if (user?.Identity?.IsAuthenticated == true)
    {
        // For authenticated users, use the user ID
        key = "name:" + user.Identity.Name;
    }
    else
    {
        // For unauthenticated users, use the IP address
        var httpContext = services.GetRequiredService<IHttpContextAccessor>().HttpContext!;
        key = "ip:" + httpContext.Connection.RemoteIpAddress.ToString();
    }

    // Pull your throttling service (e.g. Polly) from the DI container
    var throttlingService = services.GetRequiredService<IThrottlingService>();

    // Throttle the request based on the complexity, subtracting the complexity from the user's limit
    var (allow, remaining) = await throttlingService.ThrottleAsync(key, context.TotalComplexity);

    // Get the current HttpContext
    var httpContext = services.GetRequiredService<IHttpContextAccessor>().HttpContext!;

    // Add a header indicating the remaining throttling limit
    httpContext.Response.Headers["X-RateLimit-Remaining"] = remaining.ToString();

    // Report an error if the user has exceeded their limit
    if (!allow)
    {
        context.Error = new ValidationError($"Query complexity of {context.TotalComplexity} exceeded throttling limit. Remaining: {remaining}");
    }
};
```

### 6. Throttling Users Based on Execution Time

While the complexity analyzer does not directly measure execution time, you can use
`ExecutionOptions.Timeout` / `WithTimeout` to control the maximum execution time of a query.
See the following documentation for more information:

https://graphql-dotnet.github.io/docs/migrations/migration8/#24-execution-timeout-support

## Advanced configurations

### Defining Custom Complexity Calculations

To set custom complexity calculations for specific fields, you can use the `WithComplexityImpact` overload
that defines a calculation delegate as demonstrated in the following example:

```csharp
Field<ListGraphType<ProductGraphType>>("products")
    .Argument<IntGraphType>("offset")
    .Argument<IntGraphType>("limit")
    .WithComplexityImpact(context =>
    {
        var fieldImpact = 1;
        var childImpactModifier = context.GetArgument<int>("limit", 20); // use 20 if unspecified
        return new(fieldImpact, childImpactModifier);
    });
```
