#nullable enable

using System.Linq.Expressions;
using System.Reflection;
using GraphQL.Types;

namespace GraphQL.Tests.Types
{
    public class ArgumentInformationTests
    {
        [Fact]
        public void Expression_Accepts_Null()
        {
            var info = new ArgumentInformation(_testParameterInfo, typeof(object), new FieldType(), new TypeInformation(_testParameterInfo))
            {
                Expression = null
            };
            info.Expression.ShouldBeNull();
        }

        [Fact]
        public void Expression_Parses_Int()
        {
            var info = new ArgumentInformation(_testParameterInfo, typeof(object), new FieldType(), new TypeInformation(_testParameterInfo))
            {
                Expression = (Expression<Func<IResolveFieldContext, int>>)(context => 23)
            };
            info.Expression.Compile().DynamicInvoke(new object?[] { null }).ShouldBeOfType<int>().ShouldBe(23);
        }

        [Fact]
        public void Expression_Parses_Object()
        {
            var info = new ArgumentInformation(_testParameterInfo, typeof(object), new FieldType(), new TypeInformation(_testParameterInfo))
            {
                Expression = (Expression<Func<IResolveFieldContext, object>>)(context => 23)
            };
            info.Expression.Compile().DynamicInvoke(new object?[] { null }).ShouldBeOfType<int>().ShouldBe(23);
        }

        [Fact]
        public void Expression_Throws_For_Invalid_ObjectType()
        {
            var info = new ArgumentInformation(_testParameterInfo, typeof(object), new FieldType(), new TypeInformation(_testParameterInfo));
            var error = Should.Throw<ArgumentException>(() => info.Expression = (Expression<Func<IResolveFieldContext, object>>)(context => "hello"));
            error.Message.ShouldBe("Value must be a lambda expression delegate of type Func<IResolveFieldContext, Int32>.");
        }

        [Fact]
        public void Expression_Throws_For_Invalid_Type()
        {
            var info = new ArgumentInformation(_testParameterInfo, typeof(object), new FieldType(), new TypeInformation(_testParameterInfo));
            var error = Should.Throw<ArgumentException>(() => info.Expression = (Expression<Func<IResolveFieldContext, string>>)(context => "hello"));
            error.Message.ShouldBe("Value must be a lambda expression delegate of type Func<IResolveFieldContext, Int32>.");
        }

        [Fact]
        public void SetExpression_Parses_Int()
        {
            var info = new ArgumentInformation(_testParameterInfo, typeof(object), new FieldType(), new TypeInformation(_testParameterInfo));
            info.SetExpression(context => 23);
            info.Expression!.Compile().DynamicInvoke(new object?[] { null }).ShouldBeOfType<int>().ShouldBe(23);
        }

        [Fact]
        public void SetExpression_Throws_For_Invalid_Type()
        {
            var info = new ArgumentInformation(_testParameterInfo, typeof(object), new FieldType(), new TypeInformation(_testParameterInfo));
            var error = Should.Throw<ArgumentException>(() => info.SetExpression(context => "hello"));
            error.Message.ShouldStartWith("Delegate must be of type Func<IResolveFieldContext, Int32>.");
        }

        [Fact]
        public void SetExpression_Throws_For_Null()
        {
            var info = new ArgumentInformation(_testParameterInfo, typeof(object), new FieldType(), new TypeInformation(_testParameterInfo));
            var error = Should.Throw<ArgumentNullException>(() => info.SetExpression<int>(null!));
        }

        private readonly MethodInfo _testMethodInfo = typeof(ArgumentInformationTests).GetMethod(nameof(TestMethod), BindingFlags.NonPublic | BindingFlags.Instance)!;
        private readonly ParameterInfo _testParameterInfo = typeof(ArgumentInformationTests).GetMethod(nameof(TestMethod), BindingFlags.NonPublic | BindingFlags.Instance)!.GetParameters()[0]!;
        private void TestMethod(int arg)
        {
        }
    }
}
