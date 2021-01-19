using System;
using System.Collections.Generic;
using GraphQL.Types;
using Shouldly;
using Xunit;

namespace GraphQL.Tests.Bugs
{
    public class Bug1715
    {
        [Fact]
        public void Register_Should_Throw_If_Schema_Initialized()
        {
            var schema = new Schema();

            schema.Initialized.ShouldBeFalse();
            var test = schema.FindType("ID");
            schema.Initialized.ShouldBeTrue();

            Should.Throw<InvalidOperationException>(() => schema.RegisterType(new ObjectGraphType { Name = "Oops" }));
            Should.Throw<InvalidOperationException>(() => schema.RegisterTypes(new[] { new ObjectGraphType { Name = "Oops" } }));
            Should.Throw<InvalidOperationException>(() => schema.RegisterTypes(new[] { typeof(ObjectGraphType) }));
            Should.Throw<InvalidOperationException>(() => schema.RegisterType<ObjectGraphType>());
            Should.Throw<InvalidOperationException>(() => schema.RegisterDirective(new DirectiveGraphType("Oops", new DirectiveLocation[] { DirectiveLocation.Field })));
            Should.Throw<InvalidOperationException>(() => schema.RegisterDirectives((IEnumerable<DirectiveGraphType>)new[] { new DirectiveGraphType("Oops", new DirectiveLocation[] { DirectiveLocation.Field }) }));
            Should.Throw<InvalidOperationException>(() => schema.RegisterDirectives(new DirectiveGraphType("Oops1", new DirectiveLocation[] { DirectiveLocation.Field }), new DirectiveGraphType("Oops2", new DirectiveLocation[] { DirectiveLocation.Field })));
        }
    }
}
