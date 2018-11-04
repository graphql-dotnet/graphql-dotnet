using GraphQL.Execution;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using ExecutionContext = GraphQL.Execution.ExecutionContext;

namespace GraphQL.Execution
{

    public class DataflowBlocksParallelExecutionStrategy : ExecutionStrategy
    {
        protected override async Task ExecuteNodeTreeAsync(ExecutionContext context, ObjectExecutionNode rootNode)
        {
            //TODO: This is needed to prevent the DataLoader from deadlocking... not sure exactly where this
            //      needs to go...
            //await OnBeforeExecutionStepAwaitedAsync(context)
            //    .ConfigureAwait(false);
            //Options
            var blockOptions = new ExecutionDataflowBlockOptions
            {
                CancellationToken = context.CancellationToken,
                MaxDegreeOfParallelism = 1024 //TODO: Store maxDegreesOfParallelism in settings. 
            };
            //Execute Block
            var block = new TransformManyBlock<Job, Job>(
                async job =>
                {
                    var node = await ExecuteNodeAsync(job.Context, job.Node).ConfigureAwait(false);
                    var childJobs = (job.Node as IParentExecutionNode)?
                        .GetChildNodes()
                        .Select(child => job.CreateChildJob(job.Context, child))
                        .ToArray();
                    job.Complete();
                    return childJobs;
                },
                blockOptions);
            //Link to self
            block.LinkTo(block);
            //Start
            var rootJob = new Job(context, rootNode);
            await block.SendAsync(rootJob);
            //Wait until done
            await rootJob.Completion;
        }

        private class Job
        {
            private JobsCounter _counter;

            public Job(ExecutionContext context, ExecutionNode node)
                : this(new JobsCounter(), context, node) { }

            private Job(JobsCounter counter, ExecutionContext context, ExecutionNode node)
            {
                _counter = counter;
                _counter.Increment();
                Context = context;
                Node = node;
            }

            public ExecutionContext Context { get; private set; }
            public ExecutionNode Node { get; private set; }

            public Job CreateChildJob(ExecutionContext context, ExecutionNode node)
            {
                return new Job(_counter, context, node);
            }

            public int Complete()
            {
                return _counter.Decrement();
            }

            public Task Completion
            {
                get { return _counter.Completion.Task; }
            }

            private class JobsCounter
            {
                private int _counter;

                public int Increment()
                {
                    return Interlocked.Increment(ref _counter);
                }

                public int Decrement()
                {
                    var counter = Interlocked.Decrement(ref _counter);
                    if (counter == 0)
                        Completion.TrySetResult(counter);
                    return counter;
                }

                public TaskCompletionSource<int> Completion { get; } = new TaskCompletionSource<int>();
            }

        }

    }

}
