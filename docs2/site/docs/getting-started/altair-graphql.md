# Altair GraphQL Client

[Altair GraphQL Client](https://altair.sirmuel.design/) is a beautiful feature-rich GraphQL Client IDE that enables you interact with any GraphQL server you are authorized to access from any platform you are on.
You can easily test and optimize your GraphQL implementations. You also have several features to make your GraphQL development process much easier including subscriptions, query scaffolding, formatting, multiple languages, themes, and many more.

![altair-graphql](https://i.imgur.com/h63OBPA.png)

The easiest way to add Altair into your ASP.NET Core app is to use the [GraphQL.Server.Ui.Altair](https://www.nuget.org/packages/GraphQL.Server.Ui.Altair) package.
All you need to do after installing nuget is to append one extra line in your `Startup.cs`:
```c#
public void Configure(IApplicationBuilder app, IHostingEnvironment env)
{
    app.UseGraphQLAltair();
}
```
If you do not explicitly specify an endpoints through the optional `options` argument then
Altair by default will run on `/ui/altair` endpoint and will send requests to `/graphql`
GraphQL API endpoint.
