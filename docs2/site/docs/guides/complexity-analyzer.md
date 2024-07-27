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
| DefaultListImpactMultiplier     | Specifies the average number of items returned by list fields. | 5             |
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
    posts {            #     1         10            21                  5          2
      id               #     1         50            71                             3
      comments {       #     1         50           121                  5          3
        id             #     1        250           371                             4
      }                #
    }                  #
  }                    #
  products(id: "5") {  #     1          1           372                  1          1
    id                 #     1          1           373                             2
    name               #     1          1           374                             2
    photos {           #     1          1           375                  5          2
      id               #     1          5           380                             3
      name             #     1          5           385                             3
    }                  #
    category {         #     1          1           386                  1          2
      id               #     1          1           387                             3
      name             #     1          1           388                             3
    }
  }
}
```

The above query will have the following complexity calculation:
- Maximum Depth: 4 (users -> posts -> comments -> id)
- Total Complexity: 388

These values are calculated based on these facts demonstrated in the above query:
- The `users` field requested 10 items, so the child multiplier is set to 10.
- The `posts` field is a list field and uses the default child multiplier of 5.
- The `comments` field is a list field and uses the default child multiplier of 5.
- The `products` field has an `id` argument, so the child multiplier is set to 1.
- The `photos` field is a list field and uses the default child multiplier of 5.
- The `category` field is not a list field and so does not use the default child multiplier.
- Other fields are scalar fields and use the default scalar impact of 1, multiplied by the
  multiplier calculated for that level of the graph.

## Example Scenarios

### 1. Estimating the Total Number of Nodes and Maximum Depth

To configure the complexity analyzer to estimate the total number of nodes returned and/or the maximum depth,
you can use the default configuration, or customize the default impact multiplier, or customize the impact
multiplier used for specific fields. The default configuration assumes that list fields return an average of 5 items.

#### Configuring Child Impact Multiplier for Specific Fields

```csharp
usersField.WithComplexityImpact(1, 100); // Assume the users field returns 100 items on average
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

To ignore introspection fields from the maximum depth calculation, you can set the `MaxDepth` property to `null`
and provide your own validation of the calculated depth:

```csharp
complexityConfig.MaxDepth = null;
complexityConfig.ValidateComplexityDelegate = async (context, complexity, depth) =>
{
    // for non-introspection queries, validate the depth
    if (complexity > 0 && depth > 10)
    {
        context.ReportError(new ExecutionError("Query depth is too high."));
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
usersField.WithComplexityImpact(50);
```

#### Setting a Custom Default Object Impact
```csharp
// Set default for object fields (assumed to need to load from a database)
complexityConfig.DefaultObjectImpact = 20;
```

#### Sample GraphQL Request and Computed Complexity

```graphql
query {            #  impact   multiplier   total impact   child multiplier   depth
  users {          #    50          1            50                  5          1
    id             #     1          5            55                             2
    posts {        #    20          5           155                  5          2
      id           #     1         25           180                             3
      comments {   #    20         25           680                  5          3
        id         #     1        125           805                             4
      }
    }
  }
}
```

The above query will have the following complexity calculation:
- Maximum Depth: 4 (users -> posts -> comments -> id)
- Total Complexity: 805

### 4. Logging Complexity Results

In addition to validation, the `ValidateComplexityDelegate` property allows you to log complexity results
for monitoring or analysis. Please note that this delegate runs after the maximum depth and complexity checks,
so it cannot be used to log queries that fail these checks.

```csharp
complexityConfig.ValidateComplexityDelegate = async (context, complexity, depth) =>
{
    // RequestServices may be used to access scoped services within the DI container
    var logger = context.RequestServices!.GetRequiredService<ILogger<MySchema>>();
    logger.LogInformation($"Query Complexity: {complexity}, Depth: {depth}");
};
```

To log failed complexity checks, set the `MaxDepth` and `MaxComplexity` properties to `null` and provide
your own validation delegate:

```csharp
complexityConfig.MaxDepth = null;
complexityConfig.MaxComplexity = null;
complexityConfig.ValidateComplexityDelegate = async (context, complexity, depth) =>
{
    if (complexity > 1000 || depth > 10)
    {
        var logger = context.RequestServices!.GetRequiredService<ILogger<MySchema>>();
        logger.LogWarning($"Query Complexity: {complexity}, Depth: {depth}");
    }
};
```

### 5. Throttling Users Based on Complexity Analysis

To throttle users on a per-user basis similar to GitHub's GraphQL API limits, configure the
complexity analyzer with a custom validation delegate. As noted above, `MaxComplexity` and `MaxDepth`,
if set, are still enforced before this delegate runs.

```csharp
complexityConfig.ValidateComplexityDelegate = async (context, complexity, depth) =>
{
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
        var httpContext = context.RequestServices!.GetRequiredService<IHttpContextAccessor>().HttpContext!;
        key = "ip:" + httpContext.Connection.RemoteIpAddress.ToString();
    }

    // Pull your throttling service (e.g. Polly) from the DI container
    var throttlingService = context.RequestServices!.GetRequiredService<IThrottlingService>();

    // Throttle the request based on the complexity, subtracting the complexity from the user's limit
    var (allow, remaining) = await throttlingService.ThrottleAsync(key, complexity);

    // Report an error if the user has exceeded their limit
    if (!allow)
    {
        context.ReportError(new ExecutionError($"Query complexity of {complexity} exceeded throttling limit. Remaining: {remaining}"));
    }
};
```

### 6. Throttling Users Based on Execution Time

While the complexity analyzer does not directly measure execution time, you can write the following code
to control the maximum execution time of a query:

```csharp
services.AddGraphQL(b => b
    .ConfigureExecution(async (options, next) =>
    {
        using var cts1 = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        using var cts2 = CancellationTokenSource.CreateLinkedTokenSource(options.CancellationToken, cts1.Token);
        var oldToken = options.CancellationToken;
        try
        {
            options.CancellationToken = cts2.Token;
            await next(options).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cts1.Token.IsCancellationRequested) // when timeout is hit
        {
            return new ExecutionResult
            {
                Errors = new ExecutionErrors
                {
                    new ExecutionError("Operation timed out")
                    {
                        Code = "OPERATION_TIMED_OUT"
                    }
                }
            };
        }
        finally
        {
            options.CancellationToken = oldToken;
        }
    })
);
```
