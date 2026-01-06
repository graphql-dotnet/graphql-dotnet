using GraphQL.Types;

namespace GraphQL.DI;

/// <summary>
/// Allows configuration of a schema prior to the code in its constructor.
/// <br/><br/>
/// Typically executes during the <see cref="Schema"/> constructor,
/// which executes prior to any descendant classes' constructors.
/// </summary>
public interface IConfigureSchema
{
    /// <summary>
    /// Configures a schema prior to the code in its constructor.
    /// <br/><br/>
    /// Specifically, typically executes during the <see cref="Schema"/> constructor,
    /// which executes prior to any descendant classes' constructors.
    /// </summary>
    public void Configure(ISchema schema, IServiceProvider serviceProvider);

    /// <summary>
    /// Determines the order of the registered <see cref="IConfigureSchema"/> instances;
    /// the lowest order executes first; instances with the same value execute in the same
    /// order they were registered, assuming the dependency injection provider returns
    /// instances in the order they were registered.
    /// <para>
    /// The default sort order of configurations are as follows:
    /// </para>
    /// <list type="bullet">
    /// <item>100: Option configurations -- 'Add' calls and <see cref="GraphQLBuilderExtensions.ConfigureSchema(IGraphQLBuilder, Action{ISchema})">ConfigureSchema</see> calls</item>
    /// </list>
    /// </summary>
    public float SortOrder { get; }
}

internal sealed class ConfigureSchema : IConfigureSchema
{
    private readonly Action<ISchema, IServiceProvider> _action;

    public ConfigureSchema(Action<ISchema, IServiceProvider> action)
    {
        _action = action ?? throw new ArgumentNullException(nameof(action));
    }

    public void Configure(ISchema schema, IServiceProvider serviceProvider)
    {
        _action(schema, serviceProvider);
    }

    public float SortOrder => GraphQLBuilderExtensions.SORT_ORDER_OPTIONS;
}
