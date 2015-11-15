using System.Collections.Generic;
using System.Linq;
using Should;

namespace GraphQL.Tests.Execution
{
    public class InputConversionTests
    {
        public class MyInput
        {
            public int A { get; set; }
            public string B { get; set; }
            public List<string> C { get; set; }
        }

        [Test]
        public void can_convert_json_to_input_object_and_specific_object()
        {
            var json = @"{'a': 1, 'b': '2'}";

            var inputs = json.ToInputs();

            inputs.ShouldNotBeNull();
            inputs["a"].ShouldEqual((long)1);
            inputs["b"].ShouldEqual("2");

            var myInput = inputs.ToObject<MyInput>();

            myInput.ShouldNotBeNull();
            myInput.A.ShouldEqual(1);
            myInput.B.ShouldEqual("2");
        }

        [Test]
        public void can_convert_json_to_array()
        {
            var json = @"{'a': 1, 'b': '2', 'c': ['foo']}";

            var inputs = json.ToInputs();

            inputs.ShouldNotBeNull();
            inputs["a"].ShouldEqual((long)1);
            inputs["b"].ShouldEqual("2");
            inputs["c"].ShouldBeType<List<object>>();

            var myInput = inputs.ToObject<MyInput>();

            myInput.ShouldNotBeNull();
            myInput.A.ShouldEqual(1);
            myInput.B.ShouldEqual("2");
            myInput.C.ShouldNotBeNull();
            myInput.C.Count.ShouldEqual(1);
            myInput.C.First().ShouldEqual("foo");
        }
    }
}
