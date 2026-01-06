#if NET8_0_OR_GREATER
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using GraphQL.Execution;

namespace GraphQL.SystemTextJson;

/// <summary>
/// Serializes an <see cref="ExecutionResult"/> (or any other object) to a stream using
/// the <see cref="System.Text.Json"/> library with AOT (Ahead-of-Time) compilation support
/// via <see cref="JsonSerializerContext"/>.
/// </summary>
public class GraphQLAotSerializer : GraphQLSerializer
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GraphQLAotSerializer"/> class using an internally
    /// provided <see cref="JsonSerializerContext"/> and the default <see cref="ErrorInfoProvider"/>.
    /// </summary>
    public GraphQLAotSerializer()
        : this(null!, null!)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="GraphQLAotSerializer"/> class using an internally
    /// provided <see cref="JsonSerializerContext"/>.
    /// </summary>
    /// <param name="errorInfoProvider">The error information provider.</param>
    public GraphQLAotSerializer(IErrorInfoProvider errorInfoProvider)
        : this(errorInfoProvider, null!)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="GraphQLAotSerializer"/> class with the specified
    /// <see cref="JsonSerializerContext"/> and the default <see cref="ErrorInfoProvider"/>.
    /// Types not included in the provided context but required during serialization will be
    /// handled by the default serialization context.
    /// </summary>
    /// <param name="context">The JSON serializer context that provides metadata for serialization.</param>
    public GraphQLAotSerializer(JsonSerializerContext context)
        : this(null!, context)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="GraphQLAotSerializer"/> class with the specified
    /// <see cref="IErrorInfoProvider"/> and <see cref="JsonSerializerContext"/>. Types not included
    /// in the provided context but required during serialization will be handled by the default
    /// serialization context.
    /// </summary>
    /// <param name="context">The JSON serializer context that provides metadata for serialization.</param>
    /// <param name="errorInfoProvider">The error information provider.</param>
    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code")]
    [UnconditionalSuppressMessage("Trimming", "IL3050: Avoid calling members annotated with 'RequiresDynamicCodeAttribute' when publishing as Native AOT")]
    public GraphQLAotSerializer(IErrorInfoProvider errorInfoProvider, JsonSerializerContext context)
        : base(CreateOptions(errorInfoProvider, context))
    {
    }

    private static JsonSerializerOptions CreateOptions(IErrorInfoProvider? errorInfoProvider, JsonSerializerContext? context)
    {
        // Ensure the serializer has an error info provider available for the GraphQL-specific converters.
        errorInfoProvider ??= new ErrorInfoProvider();

        // No caller context: use only GraphQL's source-generated options and resolvers.
        if (context == null)
        {
            return new JsonSerializerOptions(GraphQLJsonSerializerContext.Default.Options)
            {
                TypeInfoResolver = JsonTypeInfoResolver.Combine(
                    GraphQLJsonSerializerContext.Default,
                    new GraphQLCustomJsonSerializerContext(errorInfoProvider))
            };
        }

        // Caller supplies a context: prefer its options and put it first in the resolver chain,
        // while using GraphQL's resolvers as a fallback for types not covered by the caller's context.
        return new JsonSerializerOptions(context.Options)
        {
            TypeInfoResolver = JsonTypeInfoResolver.Combine(
                context,
                GraphQLJsonSerializerContext.Default,
                new GraphQLCustomJsonSerializerContext(errorInfoProvider))
        };
    }
}
#endif
