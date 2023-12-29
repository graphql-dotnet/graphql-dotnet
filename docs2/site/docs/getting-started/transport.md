# Transport

The `GraphQL.Transport` namespace contains classes and interfaces that handle the communication between a GraphQL client and server, as well as the serialization and deserialization of GraphQL objects.

## GraphQLRequest

`GraphQLRequest` is a class that represents a [*GraphQL-over-HTTP request*](https://github.com/graphql/graphql-over-http/blob/master/spec/GraphQLOverHTTP.md#request) sent by client. It contains the following properties:

- `OperationName` - (Optional, string): The name of the Operation in the Document to execute.
- `Query` - (Required, string): The string representation of the Source Text of a GraphQL Document as specified in the Language section of the GraphQL specification.
- `Variables` - (Optional, Inputs): Values for any Variables defined by the Operation.
- `Extensions` - (Optional, Inputs): This entry is reserved for implementors to extend the protocol however they see fit.

**Note:** the *Query* property can be null in case of [persisted queries](https://www.apollographql.com/docs/apollo-server/performance/apq/) when a client sends only SHA-256 hash of the query in *Extensions* given that corresponding key-value pair has been saved on a server beforehand

## OperationMessage

`OperationMessage` represents a message typically used by the [GraphQL-WS](https://the-guild.dev/graphql/ws) or  [graphql-transport-ws](https://github.com/apollographql/subscriptions-transport-ws/blob/master/PROTOCOL.md) WebSockets-based protocols.
Both of these protocols are used by the [Apollo Client](https://www.apollographql.com/docs/react/) library. The message contains the following properties:
- Id - (Optional, string): The id of the message.
- Type - (Required, string): The type of the message. Can be one of the following:
    #### GraphQL-WS
    - `connection_init` - Client -> Server. Client sends this message to initialize the connection.
    - `connection_ack` - Server -> Client. Server sends this message to acknowledge the connection.
    - `error` - Server -> Client. Server sends this message to indicate that an error occurred.
    - `ping` - Bidirectional. Client or Server sends this message to keep the connection alive.
    - `pong` - Bidirectional. Client or Server sends this message to keep the connection alive.
    - `subscribe` - Client -> Server. Client sends this message to subscribe to a GraphQL subscription.
    - `next` - Server -> Client. Server sends this message to indicate that a new value has been received for a GraphQL subscription.
    - `complete` - Bidirectional. This message indicate that the GraphQL subscription has completed.

    #### graphql-transport-ws
    - `connection_init` - Client -> Server. Client sends this message to initialize the connection.
    - `connection_ack` - Server -> Client. Server sends this message to acknowledge the connection.
    - `connection_error` - Server -> Client. Server sends this message to indicate that an error occurred while establishing the connection.
    - `ka` - Server -> Client. Server sends this message periodically to keep the client connection alive.
    - `connection_terminate` - Client -> Server. Client sends this message to terminate the connection.
    - `start` - Client -> Server. Client sends this message to start a GraphQL operation.
    - `data` - Server -> Client. Server sends this message to send the GraphQL execution result.
    - `error` - Server -> Client. Server sends this message to indicate that an error occurred during the GraphQL operation.
    - `complete` - Server -> Client. Server sends this message to indicate that the GraphQL operation is complete.
    - `stop` - Client -> Server. Client sends this message to stop a running GraphQL operation.
- Payload - (Optional, object): The payload of the message. It is typically a `GraphQLRequest` or `ExecutionResult` instance depending on the message type.

**Note:** As mentioned in [Apollo Client](https://www.apollographql.com/docs/react/data/subscriptions/#websocket-setup) documentation, the *graphql-transport-ws* is a legacy protocol and should not be used in new applications. The *GraphQL-WS* protocol is the recommended protocol to use.

## IGraphQLSerializer & IGraphQLTextSerializer

Serialize and deserialize object hierarchies to/from a `Stream` and `string` respectively. Should include special support for `ExecutionResult` , `Inputs` and transport-specific classes as necessary. GraphQL.NET provides `Newtonsoft.Json` and `System.Text.Json` as default implementations of these interfaces. 

Simple example of `System.Text.Json` serializer in an ASP.Net Core controller is shown below:

*Program.cs*
```csharp
builder.Services.AddGraphQL(options =>
    {
        options.AddSystemTextJson();
        options.AddSchema<FooSchema>();
    });
```

*Controller.cs*
```csharp
public GraphQLController(ISchema schema, IDocumentExecuter executer, IGraphQLTextSerializer serializer)
    {
        _schema = schema;
        _executer = executer;
        _serializer = serializer;
    }

[HttpPost]
public async Task<IActionResult> Post([FromBody] GraphQLRequest query)
    {
        var result = await _executer.ExecuteAsync(options =>
            {
                options.Schema = _schema;
                options.Query = query.Query;
            }).ConfigureAwait(false);
        Response.StatusCode = (int)(result.Executed ? HttpStatusCode.OK : HttpStatusCode.BadRequest);
        return Content(_serializer.Serialize(result), "application/graphql-response+json", System.Text.Encoding.UTF8);
    }
```



