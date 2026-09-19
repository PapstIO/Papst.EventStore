namespace Papst.EventStore.Pipeline;

/// <summary>
/// The continuation of an Event Store pipeline. A handler MUST invoke
/// <c>await next()</c> to run the remaining handlers and, finally, the store's
/// persistence terminal — or throw to short-circuit the pipeline.
/// </summary>
public delegate Task EventStorePipelineDelegate();
