# GQL009: Use async resolver

|                        | Value  |
| ---------------------- | ------ |
| **Rule ID**            | GQL009 |
| **Category**           | Usage  |
| **Default severity**   | Error  |
| **Enabled by default** | Yes    |
| **Code fix provided**  | Yes    |

## Cause

This rule triggers when the sync version of the `Resolve` or `ResolveScoped`
methods is used with awaitable delegate.

## Rule description

`ResolveAsync` and `ResolveScopedAsync` methods should be used to register
awaitable delegates. The most common awaitable types are
`System.Threading.Tasks.Task` and `System.Threading.Tasks.ValueTask`, but any
type that defines a valid `GetAwaiter()` method, which returns a valid awaiter
with a `GetResult()` method is considered awaitable and is detected by the
analyzer.  
The rule is useful when `FieldBuilder` is defined without return type argument
or the return type argument is defined as `object` or `dynamic` and compiler
allows returning `Task<T>` and other awaitables from the delegate.

## How to fix violations

Replace the sync `Resolve` or `ResolveScoped` method with matching async version
and await the delegate when needed.

## Example of a violation

```c#
public class MyGraphType : ObjectGraphType<Person>
{
    public MyGraphType()
    {
        // 1. no return type defined
        Field<StringGraphType>("Title")
            .Resolve(ctx => Task.FromResult("developer"));

        // 2. the method returns ValueTask<T>
        Field<StringGraphType>("Title")
            .Resolve(ctx => GetTitleAsync());

        // 3. method group, return type defined in Field method
        Field<StringGraphType, object>("Title")
            .Resolve(ResolveTitleAsync);

        // 4. field builder created with awaitable return type
        Field<StringGraphType, Task<string>>("Title")
            .ResolveScoped(ctx => Task.FromResult("developer"));

        // 5. the method returns ValueTask<T>
        Field<StringGraphType>("Title")
            .ResolveScoped(ctx => GetTitleAsync());

        // 6. object or dynamic is used as return type in Returns method
        Field<StringGraphType>("Title").Returns<dynamic>()
            .ResolveScoped(ctx => ResolveTitleAsync(ctx));
    }

    private ValueTask<string> GetTitleAsync() => ValueTask.FromResult("developer");

    private Task<string> ResolveTitleAsync(IResolveFieldContext<Person> ctx) =>
        Task.FromResult("developer");
}
```

## Example of how to fix

```c#
public class MyGraphType : ObjectGraphType<Person>
{
    public MyGraphType()
    {
        // 1. no return type defined
        //    await the delegate
        Field<StringGraphType>("Title")
            .ResolveAsync(async ctx => await Task.FromResult("developer"));

        // 2. the method returns ValueTask<T>
        //    await the delegate
        Field<StringGraphType>("Title")
            .ResolveAsync(async ctx => await GetTitleAsync());

        // 3. method group, return type defined in Field method
        //    await the delegate
        Field<StringGraphType, object>("Title")
            .ResolveAsync(async ctx => await ResolveTitleAsync(ctx));

        // 4. field builder created with awaitable return type
        //    unwrap the return type
        Field<StringGraphType, string>("Title")
            .ResolveScopedAsync(ctx => Task.FromResult("developer"));

        // 5. the method returns ValueTask<T>
        //    await the delegate and defined the return type
        Field<StringGraphType, string>("Title")
            .ResolveScopedAsync(async ctx => await GetTitleAsync());

        // 6. object or dynamic is used as return type in Returns method
        //    await the delegate and define the source type and return type
        //    on the ResolveScopedAsync method
        Field<StringGraphType>("Title").Returns<dynamic>()
            .ResolveScopedAsync<Person, dynamic>(async ctx =>
                await ResolveTitleAsync(ctx));
    }

    private ValueTask<string> GetTitleAsync() => ValueTask.FromResult("developer");

    private Task<string> ResolveTitleAsync(IResolveFieldContext<Person> ctx) =>
        Task.FromResult("developer");
}
```

## Suppress a warning

If you just want to suppress a single violation, add preprocessor directives to
your source file to disable and then re-enable the rule.

```csharp
#pragma warning disable GQL009
// The code that's violating the rule is on this line.
#pragma warning restore GQL009
```

To disable the rule for a file, folder, or project, set its severity to `none`
in the
[configuration file](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/configuration-files).

```ini
[*.cs]
dotnet_diagnostic.GQL009.severity = none
```

For more information, see
[How to suppress code analysis warnings](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/suppress-warnings).

## Related rules
