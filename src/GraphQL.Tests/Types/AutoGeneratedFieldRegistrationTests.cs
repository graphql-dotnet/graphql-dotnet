﻿using GraphQL.Types;
using Shouldly;
using Xunit;

namespace GraphQL.Tests.Types
{
    public class AutoGeneratedFieldRegistrationTests
    {
        private class TestPoco
        {
            public int UserId { get; set; } = 123;

            public string UserName { get; set; } = "Sally";

            public double UserRating { get; set; } = 1.0;
        }

        [Fact]
        public void autogenerated_fields_should_exist()
        {
            var graphType = new AutoRegisteringObjectGraphType<TestPoco>();

            graphType.HasField("UserId").ShouldBeTrue();
            graphType.HasField("UserName").ShouldBeTrue();
            graphType.HasField("UserRating").ShouldBeTrue();
        }


        [Fact]
        public void should_not_throw_error_when_trying_to_register_field_with_same_name()
        {
            var graphType = new AutoRegisteringObjectGraphType<TestPoco>();

            graphType.HasField("UserName").ShouldBeTrue();
            
            Should.NotThrow(() =>
            {
                var ff = graphType.Field(f => f.UserName);
            });
        }

        [Fact]
        public void overridden_field_should_be_new_instance()
        {
            var graphType = new AutoRegisteringObjectGraphType<TestPoco>();

            var originalField = graphType.GetField("UserName");

            originalField.ShouldNotBeNull();

            int originalHashCode = originalField.GetHashCode();

            var overridenField = graphType.Field(poco => poco.UserName);


            overridenField.GetHashCode().ShouldNotBe(originalHashCode);


        }

        [Fact]
        public void renamed_field_should_replace_old_one()
        {

            var graphType = new AutoRegisteringObjectGraphType<TestPoco>();

            graphType.HasField("UserRating").ShouldBeTrue();


            graphType.Field(f => f.UserRating)
                .Name("renamed");

            graphType.HasField("UserRating").ShouldBeFalse();

            graphType.GetField("renamed").ShouldNotBeNull();

            
        }

        [Fact]
        public void removed_field_should_not_exist()
        {

            var graphType = new AutoRegisteringObjectGraphType<TestPoco>();
            graphType.HasField("UserName").ShouldBeTrue();

            graphType.RemoveField(poco => poco.UserName);    

            graphType.HasField("UserName").ShouldBeFalse();
            
        }

        [Fact]
        public void can_register_field_of_compatible_type()
        {
            var graphType = new AutoRegisteringObjectGraphType<TestPoco>();
            graphType.Field(typeof(BooleanGraphType), "isValid").Type.ShouldBe(typeof(BooleanGraphType));
        }

        
    }
}