using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GraphQL.DataLoader.Tests.Models;
using Shouldly;
using Xunit;

namespace GraphQL.DataLoader.Tests
{
    public class SimpleDataLoaderTests : DataLoaderTestBase
    {
        public SimpleDataLoaderTests()
        {
            Users.AddUsers(
                new User
                {
                    UserId = 1
                },
                new User
                {
                    UserId = 2
                }
            );
        }

        [Fact]
        public async Task Result_Is_Cached()
        {
            var loader = new SimpleDataLoader<IEnumerable<User>>((ct) => Users.GetAllUsersAsync(ct, 20));

            var task = loader.LoadAsync();

            loader.Dispatch();

            var result1 = await task;

            result1.ShouldNotBeNull();
            result1.Count().ShouldBe(2);

            // Load again. Result should be cached
            var task2 = loader.LoadAsync();

            task2.Status.ShouldBe(TaskStatus.RanToCompletion);

            var result2 = await task2;

            // Results should be the same instance
            result2.ShouldBeSameAs(result1);

            Users.GetAllUsersCalledCount.ShouldBe(1);
        }

        [Fact]
        public async Task Operation_Can_Be_Cancelled()
        {
            var cts = new CancellationTokenSource();
            var loader = new SimpleDataLoader<IEnumerable<User>>((ct) => Users.GetAllUsersAsync(ct, 20));

            var task = loader.LoadAsync();

            cts.CancelAfter(5);

            loader.Dispatch(cts.Token);

            bool wasCancelled;

            try
            {
                var users = await task;
                wasCancelled = false;
            }
            catch (TaskCanceledException)
            {
                wasCancelled = true;
            }

            wasCancelled.ShouldBe(true);
            Users.GetAllUsersCalledCount.ShouldBe(1);
        }

        [Fact]
        public async Task Operation_Cancelled_Before_Dispatch_Does_Not_Execute()
        {
            var cts = new CancellationTokenSource();
            var loader = new SimpleDataLoader<IEnumerable<User>>((ct) => Users.GetAllUsersAsync(ct));

            var task = loader.LoadAsync();

            cts.Cancel();

            loader.Dispatch(cts.Token);

            bool wasCancelled;

            try
            {
                var users = await task;
                wasCancelled = false;
            }
            catch (TaskCanceledException)
            {
                wasCancelled = true;
            }

            wasCancelled.ShouldBe(true);
            Users.GetAllUsersCalledCount.ShouldBe(0, "Fetch delegate should not be called");
        }

        [Fact]
        public async Task Deferred_Exception_Is_Bubbled_Properly()
        {
            var loader = new SimpleDataLoader<IEnumerable<User>>(Users.ThrowExceptionDeferredAsync);

            var task = loader.LoadAsync();

            loader.Dispatch();

            try
            {
                await task;
            }
            catch (Exception ex) when (ex.Message == "Deferred")
            {
                // This is what should happen
            }
        }

        [Fact]
        public async Task Immediate_Exception_Is_Bubbled_Properly()
        {
            var loader = new SimpleDataLoader<IEnumerable<User>>(Users.ThrowExceptionImmediatelyAsync);

            var task = loader.LoadAsync();

            loader.Dispatch();

            try
            {
                await task;
            }
            catch (Exception ex) when (ex.Message == "Immediate")
            {
                // This is what should happen
            }
        }
    }
}
