using System;
using System.Reflection;
using GraphQL.Types;
using Shouldly;
using Xunit;

namespace GraphQL.Tests.Types
{
    public class TypeInformationTests
    {
        private static readonly PropertyInfo _member = typeof(Type).GetProperty(nameof(Type.FullName))!;

        [Fact]
        public void GraphType_Output_VerifyErrors()
        {
            var typeInfo = new TypeInformation(_member, false);
            typeInfo.GraphType.ShouldBeNull();
            typeInfo.GraphType = typeof(ObjectGraphType);
            typeInfo.GraphType = typeof(IdGraphType);
            Should.Throw<ArgumentException>(() => typeInfo.GraphType = typeof(InputObjectGraphType));
            Should.Throw<ArgumentException>(() => typeInfo.GraphType = typeof(ListGraphType<ObjectGraphType>));
            typeInfo.GraphType = null;
        }

        [Fact]
        public void GraphType_Input_VerifyErrors()
        {
            var typeInfo = new TypeInformation(_member, true);
            typeInfo.GraphType.ShouldBeNull();
            typeInfo.GraphType = typeof(InputObjectGraphType);
            typeInfo.GraphType = typeof(IdGraphType);
            Should.Throw<ArgumentException>(() => typeInfo.GraphType = typeof(ObjectGraphType));
            Should.Throw<ArgumentException>(() => typeInfo.GraphType = typeof(ListGraphType<InputObjectGraphType>));
            typeInfo.GraphType = null;
        }
    }
}
