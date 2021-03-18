using System;
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
            var baseProvider = new SimpleContainer();
            baseProvider.Singleton<StarWarsData>();

            // mock it so we can verify behavior
            var testProviderMock = new Mock<IServiceProvider>(MockBehavior.Loose);
            testProviderMock.Setup(x => x.GetService(It.IsAny<Type>())).Returns<Type>(type => ((IServiceProvider)baseProvider).GetService(type));

            // run test
            var schema = new StarWarsSchema(testProviderMock.Object);
            schema.Initialize();

            // verify that GetService was only called once for each schema type
            testProviderMock.Verify(x => x.GetService(typeof(StarWarsQuery)), Times.Once);
            testProviderMock.Verify(x => x.GetService(typeof(StarWarsMutation)), Times.Once);
            testProviderMock.Verify(x => x.GetService(typeof(CharacterInterface)), Times.Once);
            testProviderMock.Verify(x => x.GetService(typeof(DroidType)), Times.Once);
            testProviderMock.Verify(x => x.GetService(typeof(HumanInputType)), Times.Once);
            testProviderMock.Verify(x => x.GetService(typeof(HumanType)), Times.Once);
            testProviderMock.Verify(x => x.GetService(typeof(EpisodeEnum)), Times.Once);
            testProviderMock.VerifyNoOtherCalls();
        }
    }
}
