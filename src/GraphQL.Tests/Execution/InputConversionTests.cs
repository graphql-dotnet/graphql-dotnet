using Should;

namespace GraphQL.Tests.Execution
{
    public class InputConversionTests
    {
        public class MyInput
        {
            public int A { get; set; }
            public string B { get; set; }
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
    }
}
