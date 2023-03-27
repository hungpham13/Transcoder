using System.Threading.Channels;
using Transcoder.Model;

namespace Transcoder.Services.BackgroundQueue;

public interface IBackgroundTaskQueue
{
    ValueTask QueueBackgroundWorkItemAsync(BaseProcessingJob workItem);

    ValueTask<BaseProcessingJob> DequeueAsync(
        CancellationToken cancellationToken);
}

public class BackgroundTaskQueue : IBackgroundTaskQueue
{
    private readonly Channel<BaseProcessingJob> _queue;

    public BackgroundTaskQueue(int capacity)
    {
        // Capacity should be set based on the expected application load and
        // number of concurrent threads accessing the queue.            
        // BoundedChannelFullMode.Wait will cause calls to WriteAsync() to return a task,
        // which completes only when space became available. This leads to backpressure,
        // in case too many publishers/calls start accumulating.
        var options = new BoundedChannelOptions(capacity)
        {
            FullMode = BoundedChannelFullMode.Wait
        };
        _queue = Channel.CreateBounded<BaseProcessingJob>(options);
    }

    public async ValueTask QueueBackgroundWorkItemAsync(BaseProcessingJob workItem)
    {
        if (workItem == null)
        {
            throw new ArgumentNullException(nameof(workItem));
        }

        await _queue.Writer.WriteAsync(workItem);
    }

    public async ValueTask<BaseProcessingJob> DequeueAsync(
        CancellationToken cancellationToken)
    {
        var workItem = await _queue.Reader.ReadAsync(cancellationToken);

        return workItem;
    }
}
