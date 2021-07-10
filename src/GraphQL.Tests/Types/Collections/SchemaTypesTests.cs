using System;
using System.Collections.Generic;
using GraphQL.DI;
using GraphQL.StarWars;
using GraphQL.StarWars.IoC;
using GraphQL.StarWars.Types;
using Moq;
using Xunit;

namespace GraphQL.Tests.Types.Collections
{
    public class SchemaTypesTests
    {
        [Fact]
        public void does_not_request_instance_more_than_once()
        {
            // configure DI provider
            var services = new SimpleContainer();
            services.Singleton<StarWarsData>();

            // mock it so we can verify behavior
            var mock = new Mock<IServiceProvider>(MockBehavior.Loose);
            mock.Setup(x => x.GetService(It.IsAny<Type>())).Returns<Type>(type => services.Get(type));

            // run test
            var schema = new StarWarsSchema(mock.Object);
            schema.Initialize();

            // verify that GetService was only called once for each schema type
            mock.Verify(x => x.GetService(typeof(StarWarsQuery)), Times.Once);
            mock.Verify(x => x.GetService(typeof(StarWarsMutation)), Times.Once);
            mock.Verify(x => x.GetService(typeof(CharacterInterface)), Times.Once);
            mock.Verify(x => x.GetService(typeof(DroidType)), Times.Once);
            mock.Verify(x => x.GetService(typeof(HumanInputType)), Times.Once);
            mock.Verify(x => x.GetService(typeof(HumanType)), Times.Once);
            mock.Verify(x => x.GetService(typeof(EpisodeEnum)), Times.Once);
            mock.Verify(x => x.GetService(typeof(IEnumerable<IConfigureSchema>)), Times.Once);
            mock.VerifyNoOtherCalls();
        }
    }
}
