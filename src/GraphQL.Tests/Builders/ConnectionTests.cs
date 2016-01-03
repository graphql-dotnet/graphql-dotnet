using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GraphQL.Types;
using Should;

namespace GraphQL.Tests.Builders
{
    public class ConnectionTests
    {
        [Test]
        public void SimpleConnection()
        {
            var connection = GenerateConnection(5);

            connection.TotalCount.ShouldEqual(5);
            connection.PageInfo.HasNextPage.ShouldEqual(false);
            connection.PageInfo.HasPreviousPage.ShouldEqual(false);
            connection.PageInfo.StartCursor.ShouldEqual("00000001");
            connection.PageInfo.EndCursor.ShouldEqual("00000005");
            connection.Edges.Count.ShouldEqual(5);
            connection.Items.Count.ShouldEqual(5);

            for (var i = 1; i <= 5; i++)
            {
                connection.Edges[i - 1].Cursor.ShouldEqual($"{i:D8}");
                connection.Edges[i - 1].Node.Value.ShouldEqual(i);
                connection.Items[i - 1].Value.ShouldEqual(i);
            }
        }

        [Test]
        public void First3()
        {
            var connection = GenerateConnection(5, first: 3);

            connection.TotalCount.ShouldEqual(5);
            connection.PageInfo.HasNextPage.ShouldEqual(true);
            connection.PageInfo.HasPreviousPage.ShouldEqual(false);
            connection.PageInfo.StartCursor.ShouldEqual("00000001");
            connection.PageInfo.EndCursor.ShouldEqual("00000003");
            connection.Edges.Count.ShouldEqual(3);
            connection.Items.Count.ShouldEqual(3);

            for (var i = 1; i <= 3; i++)
            {
                connection.Edges[i - 1].Cursor.ShouldEqual($"{i:D8}");
                connection.Edges[i - 1].Node.Value.ShouldEqual(i);
                connection.Items[i - 1].Value.ShouldEqual(i);
            }
        }

        [Test]
        public void Last3()
        {
            var connection = GenerateConnection(5, last: 3);

            connection.TotalCount.ShouldEqual(5);
            connection.PageInfo.HasNextPage.ShouldEqual(false);
            connection.PageInfo.HasPreviousPage.ShouldEqual(true);
            connection.PageInfo.StartCursor.ShouldEqual("00000003");
            connection.PageInfo.EndCursor.ShouldEqual("00000005");
            connection.Edges.Count.ShouldEqual(3);
            connection.Items.Count.ShouldEqual(3);

            for (var i = 1; i <= 3; i++)
            {
                connection.Edges[i - 1].Cursor.ShouldEqual($"{i + 2:D8}");
                connection.Edges[i - 1].Node.Value.ShouldEqual(i + 2);
                connection.Items[i - 1].Value.ShouldEqual(i + 2);
            }
        }

        [Test]
        public void First3After1()
        {
            var connection = GenerateConnection(5, after: "00000001", first: 3);

            connection.TotalCount.ShouldEqual(5);
            connection.PageInfo.HasNextPage.ShouldEqual(true);
            connection.PageInfo.HasPreviousPage.ShouldEqual(true);
            connection.PageInfo.StartCursor.ShouldEqual("00000002");
            connection.PageInfo.EndCursor.ShouldEqual("00000004");
            connection.Edges.Count.ShouldEqual(3);
            connection.Items.Count.ShouldEqual(3);

            for (var i = 1; i <= 3; i++)
            {
                connection.Edges[i - 1].Cursor.ShouldEqual($"{i + 1:D8}");
                connection.Edges[i - 1].Node.Value.ShouldEqual(i + 1);
                connection.Items[i - 1].Value.ShouldEqual(i + 1);
            }
        }

        [Test]
        public void Last3Before5()
        {
            var connection = GenerateConnection(5, before: "00000005", last: 3);

            connection.TotalCount.ShouldEqual(5);
            connection.PageInfo.HasNextPage.ShouldEqual(true);
            connection.PageInfo.HasPreviousPage.ShouldEqual(true);
            connection.PageInfo.StartCursor.ShouldEqual("00000002");
            connection.PageInfo.EndCursor.ShouldEqual("00000004");
            connection.Edges.Count.ShouldEqual(3);
            connection.Items.Count.ShouldEqual(3);

            for (var i = 1; i <= 3; i++)
            {
                connection.Edges[i - 1].Cursor.ShouldEqual($"{i + 1:D8}");
                connection.Edges[i - 1].Node.Value.ShouldEqual(i + 1);
                connection.Items[i - 1].Value.ShouldEqual(i + 1);
            }
        }

        [Test]
        public void AfterBounds()
        {
            var connection = GenerateConnection(5, after: "00000005", first: 2);

            connection.TotalCount.ShouldEqual(5);
            connection.PageInfo.HasNextPage.ShouldEqual(false);
            connection.PageInfo.HasPreviousPage.ShouldEqual(true);
            connection.PageInfo.StartCursor.ShouldEqual("00000006");
            connection.PageInfo.EndCursor.ShouldEqual("00000006");
            connection.Edges.Count.ShouldEqual(0);
            connection.Items.Count.ShouldEqual(0);
        }


        [Test]
        public void BeforeBounds()
        {
            var connection = GenerateConnection(5, before: "00000001", last: 2);

            connection.TotalCount.ShouldEqual(5);
            connection.PageInfo.HasNextPage.ShouldEqual(true);
            connection.PageInfo.HasPreviousPage.ShouldEqual(false);
            connection.PageInfo.StartCursor.ShouldEqual("00000000");
            connection.PageInfo.EndCursor.ShouldEqual("00000000");
            connection.Edges.Count.ShouldEqual(0);
            connection.Items.Count.ShouldEqual(0);
        }

        [Test]
        public void First0()
        {
            var connection = GenerateConnection(5, first: 0);

            connection.TotalCount.ShouldEqual(5);
            connection.PageInfo.HasNextPage.ShouldEqual(true);
            connection.PageInfo.HasPreviousPage.ShouldEqual(false);
            connection.PageInfo.StartCursor.ShouldEqual("00000000");
            connection.PageInfo.EndCursor.ShouldEqual("00000000");
            connection.Edges.Count.ShouldEqual(0);
            connection.Items.Count.ShouldEqual(0);
        }

        [Test]
        public void First0After2()
        {
            var connection = GenerateConnection(5, after: "00000002", first: 0);

            connection.TotalCount.ShouldEqual(5);
            connection.PageInfo.HasNextPage.ShouldEqual(true);
            connection.PageInfo.HasPreviousPage.ShouldEqual(true);
            connection.PageInfo.StartCursor.ShouldEqual("00000003");
            connection.PageInfo.EndCursor.ShouldEqual("00000003");
            connection.Edges.Count.ShouldEqual(0);
            connection.Items.Count.ShouldEqual(0);
        }

        [Test]
        public void First1After2()
        {
            var connection = GenerateConnection(5, after: "00000002", first: 1);

            connection.TotalCount.ShouldEqual(5);
            connection.PageInfo.HasNextPage.ShouldEqual(true);
            connection.PageInfo.HasPreviousPage.ShouldEqual(true);
            connection.PageInfo.StartCursor.ShouldEqual("00000003");
            connection.PageInfo.EndCursor.ShouldEqual("00000003");
            connection.Edges.Count.ShouldEqual(1);
            connection.Items.Count.ShouldEqual(1);
        }

        [Test]
        public void AfterLast()
        {
            var connection = GenerateConnection(5, after: "00000005");

            connection.TotalCount.ShouldEqual(5);
            connection.PageInfo.HasNextPage.ShouldEqual(false);
            connection.PageInfo.HasPreviousPage.ShouldEqual(true);
            connection.PageInfo.StartCursor.ShouldEqual("00000006");
            connection.PageInfo.EndCursor.ShouldEqual("00000006");
            connection.Edges.Count.ShouldEqual(0);
            connection.Items.Count.ShouldEqual(0);
        }

        [Test]
        public void Last0()
        {
            var connection = GenerateConnection(5, last: 0);

            connection.TotalCount.ShouldEqual(5);
            connection.PageInfo.HasNextPage.ShouldEqual(false);
            connection.PageInfo.HasPreviousPage.ShouldEqual(true);
            connection.PageInfo.StartCursor.ShouldEqual("00000006");
            connection.PageInfo.EndCursor.ShouldEqual("00000006");
            connection.Edges.Count.ShouldEqual(0);
            connection.Items.Count.ShouldEqual(0);
        }

        [Test]
        public void Last0Before3()
        {
            var connection = GenerateConnection(5, before: "00000003", last: 0);

            connection.TotalCount.ShouldEqual(5);
            connection.PageInfo.HasNextPage.ShouldEqual(true);
            connection.PageInfo.HasPreviousPage.ShouldEqual(true);
            connection.PageInfo.StartCursor.ShouldEqual("00000002");
            connection.PageInfo.EndCursor.ShouldEqual("00000002");
            connection.Edges.Count.ShouldEqual(0);
            connection.Items.Count.ShouldEqual(0);
        }

        [Test]
        public void BeforeFirst()
        {
            var connection = GenerateConnection(5, before: "00000001");

            connection.TotalCount.ShouldEqual(5);
            connection.PageInfo.HasNextPage.ShouldEqual(true);
            connection.PageInfo.HasPreviousPage.ShouldEqual(false);
            connection.PageInfo.StartCursor.ShouldEqual("00000000");
            connection.PageInfo.EndCursor.ShouldEqual("00000000");
            connection.Edges.Count.ShouldEqual(0);
            connection.Items.Count.ShouldEqual(0);
        }

        [Test]
        public void CombineBeforeAndAfter()
        {
            ArgumentException argEx = null;
            try
            {
                GenerateConnection(5, before: "00000001", after: "00000001");
            }
            catch (ArgumentException ex)
            {
                argEx = ex;
            }
            argEx.ShouldNotBeNull();
            argEx?.Message.ShouldEqual("Cannot use `after` in conjunction with `before`.");
        }

        [Test]
        public void CombineFirstAndLast()
        {
            ArgumentException argEx = null;
            try
            {
                GenerateConnection(5, first: 1, last: 1);
            }
            catch (ArgumentException ex)
            {
                argEx = ex;
            }
            argEx.ShouldNotBeNull();
            argEx?.Message.ShouldEqual("Cannot use `first` in conjunction with `last`.");
        }

        [Test]
        public void CombineFirstAndBefore()
        {
            ArgumentException argEx = null;
            try
            {
                GenerateConnection(5, first: 1, before: "00000003");
            }
            catch (ArgumentException ex)
            {
                argEx = ex;
            }
            argEx.ShouldNotBeNull();
            argEx?.Message.ShouldEqual("Cannot use `first` in conjunction with `before`.");
        }

        [Test]
        public void CombineLastAndAfter()
        {
            ArgumentException argEx = null;
            try
            {
                GenerateConnection(5, last: 1, after: "00000001");
            }
            catch (ArgumentException ex)
            {
                argEx = ex;
            }
            argEx.ShouldNotBeNull();
            argEx?.Message.ShouldEqual("Cannot use `last` in conjunction with `after`.");
        }

        [Test]
        public void First3OfInfiniteCollection()
        {
            var connection = GenerateInfiniteConnection(5, 6, first: 3);

            connection.PageInfo.HasNextPage.ShouldEqual(true);
            connection.PageInfo.HasPreviousPage.ShouldEqual(false);
            connection.PageInfo.StartCursor.ShouldEqual("00000001");
            connection.PageInfo.EndCursor.ShouldEqual("00000003");
            connection.Edges.Count.ShouldEqual(3);
            connection.Items.Count.ShouldEqual(3);

            for (var i = 1; i <= 3; i++)
            {
                connection.Edges[i - 1].Cursor.ShouldEqual($"{i:D8}");
                connection.Edges[i - 1].Node.ShouldEqual(i);
            }
        }

        [Test]
        public void First3After2OfInfiniteCollection()
        {
            var connection = GenerateInfiniteConnection(5, 6, first: 3, after: "00000002");

            connection.PageInfo.HasNextPage.ShouldEqual(true);
            connection.PageInfo.HasPreviousPage.ShouldEqual(true);
            connection.PageInfo.StartCursor.ShouldEqual("00000003");
            connection.PageInfo.EndCursor.ShouldEqual("00000005");
            connection.Edges.Count.ShouldEqual(3);
            connection.Items.Count.ShouldEqual(3);

            for (var i = 1; i <= 3; i++)
            {
                connection.Edges[i - 1].Cursor.ShouldEqual($"{i + 2:D8}");
                connection.Edges[i - 1].Node.ShouldEqual(i + 2);
            }
        }

        [Test]
        public void Last3OfInfiniteCollectionThrowsException()
        {
            try
            {
                GenerateInfiniteConnection(5, 6, last: 3);
                true.ShouldBeFalse("Getting the last entries of an infinite collection should fail.");
            }
            catch (Exception ex)
            {
                ex.Message.ShouldEqual("Enumerated too many entries");
            }
        }

        [Test]
        public void TotalCountOfInfiniteCollectionThrowsException()
        {
            try
            {
                var connection = GenerateInfiniteConnection(5, 6, first: 3);
                connection.TotalCount.ShouldBeNull();
                true.ShouldBeFalse("Getting the total count of an infinite collection should fail.");
            }
            catch (Exception ex)
            {
                ex.Message.ShouldEqual("Enumerated too many entries");
            }
        }

        private Connection<Foo> GenerateConnection(
            int count, int? first = null, int? last = null, string after = null, string before = null)
        {
            var items = new List<Foo>();
            for (var i = 1; i <= count; i++)
            {
                items.Add(new Foo { Value = i });
            }

            return new Connection<Foo>()
                .FromCollection(items)
                .Paginate(first, last, after, before);
        }

        private IEnumerable<int> InfiniteCollection(int throwAboveThisNumber = -1)
        {
            for (var i = 1; ; i++)
            {
                if (throwAboveThisNumber >= 0 && i > throwAboveThisNumber)
                {
                    throw new Exception("Enumerated too many entries");
                }
                yield return i;
            }
        }

        private Connection<int> GenerateInfiniteConnection(
            int count, int throwExceptionAboveThisNumber,
            int? first = null, int? last = null, string after = null, string before = null)
        {
            return new Connection<int>()
                .FromCollection(InfiniteCollection(throwExceptionAboveThisNumber))
                .Paginate(first, last, after, before);
        }

        class Foo
        {
            public int Value { get; set; }
        }

        class Bar
        {
            public string Value { get; set; }
        }
    }
}
