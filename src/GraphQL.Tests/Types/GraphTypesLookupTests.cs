using System;
using System.Threading;
using System.Threading.Tasks;
using GraphQL.Types;
using Shouldly;
using Xunit;

namespace GraphQL.Tests.Types
{
    public class GraphTypesLookupTests
    {
        private class TestType : GraphType { }

        private readonly ManualResetEvent _inIteration = new ManualResetEvent(false);
        private readonly ManualResetEvent _lookupModified = new ManualResetEvent(false);
        private readonly GraphTypesLookup _lookup = new GraphTypesLookup();

        [Fact]
        public void is_thread_safe()
        {
            Task.Run(() => AddAType());
            var modified = false;
            foreach (var t in _lookup.All())
            {
                if (modified) continue;
                _inIteration.Set();
                _lookupModified.WaitOne();
                modified = true;
            }
        }

        private void AddAType()
        {
            _inIteration.WaitOne();
            _lookup["test"] = new TestType();
            _lookupModified.Set();
        }
    }
}
