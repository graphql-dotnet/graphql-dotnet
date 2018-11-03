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

        //TO DO: I have to believe there is a better way to do this...
        private static int _refCount = 0;

        protected override async Task ExecuteNodeTreeAsync(ExecutionContext context, ObjectExecutionNode rootNode)
        {
            var cancellationSource = new CancellationTokenSource();
            await ExecuteDataBlocksPipeline(context, rootNode, cancellationSource, 1024);
            //await OnBeforeExecutionStepAwaitedAsync(context)
            //    .ConfigureAwait(false);
        }

        private async Task ExecuteDataBlocksPipeline(
            ExecutionContext context, ObjectExecutionNode rootNode,
            CancellationTokenSource cancellationSource, int maxDegreesOfParallelism)
        {
            //Options
            var blockOptions = new ExecutionDataflowBlockOptions
            {
                CancellationToken = cancellationSource.Token,
                MaxDegreeOfParallelism = maxDegreesOfParallelism
            };
            //NavigateChildren Block
            var navigateChildren = new TransformManyBlock<Job, Job>(
                job =>
                {
                    var childJobs = (job.Node as IParentExecutionNode)?
                        .GetChildNodes()
                        .Select(child => new Job(job.Context, child))
                        .ToArray();
                    return childJobs;
                }, blockOptions);
            //Execute Block
            var executeBlock = new TransformBlock<Job, Job>(
                async job => {
                    var node = await ExecuteNodeAsync(job.Context, job.Node).ConfigureAwait(false);
                    Interlocked.Add(ref _refCount, (node as IParentExecutionNode)?.GetChildNodes().Count() ?? 0);
                    Interlocked.Decrement(ref _refCount);
                    if (_refCount == 0)
                        navigateChildren.Complete();
                    return new Job(job.Context, node);
                },
                blockOptions);
            //Pipeline
            var linkOptions = new DataflowLinkOptions { PropagateCompletion = false };
            executeBlock.LinkTo(navigateChildren, linkOptions);
            navigateChildren.LinkTo(executeBlock, linkOptions);
            _refCount = 1;
            await executeBlock.SendAsync(new Job(context, rootNode));
            await navigateChildren.Completion;
        }

        private class Job
        {
            public Job(ExecutionContext context, ExecutionNode node)
            {
                Context = context;
                Node = node;
            }
            public ExecutionContext Context { get; private set; }
            public ExecutionNode Node { get; private set; }
        }

    }

}
