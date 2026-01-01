using System.Collections.Concurrent;

namespace Kr.Common.Mediator;

public partial class Mediate : IMediate
{
    private readonly ConcurrentDictionary<Type, object> _handlerCacheWithResponse = new();
    private readonly ConcurrentDictionary<Type, object> _handlerCacheNoResponse = new();

    public async Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        if (request == null) throw new ArgumentNullException(nameof(request));

        var requestType = request.GetType();
        var invokerObj = _handlerCacheWithResponse.GetOrAdd(requestType, _ => CreateCompiledInvokerWithResponse<TResponse>(requestType));
        var invoker = (Func<IRequest<TResponse>, CancellationToken, Task<TResponse>>)invokerObj;

        return await invoker(request, cancellationToken);
    }

    public async Task Send(IRequest request, CancellationToken cancellationToken = default)
    {
        if (request == null) throw new ArgumentNullException(nameof(request));

        var requestType = request.GetType();
        var invokerObj = _handlerCacheNoResponse.GetOrAdd(requestType, _ => CreateCompiledInvokerNoResponse(requestType));
        var invoker = (Func<IRequest, CancellationToken, Task>)invokerObj;

        await invoker(request, cancellationToken);
    }

    private Func<IRequest<TResponse>, CancellationToken, Task<TResponse>> CreateCompiledInvokerWithResponse<TResponse>(Type requestType)
    {
        var handlerType = typeof(IRequestHandler<,>).MakeGenericType(requestType, typeof(TResponse));
        var behaviorType = typeof(IPipelineBehavior<,>).MakeGenericType(requestType, typeof(TResponse));

        // Compile the invokers - these take serviceProvider as first parameter
        var handlerInvoker = CompileHandlerInvoker<TResponse>(requestType, handlerType);
        var behaviorInvoker = CompileBehaviorInvoker<TResponse>(requestType, behaviorType);

        // Return a closure that captures the compiled invokers
        return async (request, ct) =>
        {
            // Get the scoped service provider for this request
            var serviceProvider = GetScopedServiceProvider();
            
            // Resolve behaviors from current scope
            var behaviorsType = typeof(IEnumerable<>).MakeGenericType(behaviorType);
            var behaviors = serviceProvider.GetService(behaviorsType) as System.Collections.IEnumerable;

            // Build the pipeline - wrap handlerInvoker to match RequestHandlerDelegate signature
            RequestHandlerDelegate<TResponse> pipeline = () => handlerInvoker(serviceProvider, request, ct);

            if (behaviors != null)
            {
                var behaviorList = behaviors.Cast<object>().Reverse().ToList();
                
                foreach (var behavior in behaviorList)
                {
                    var currentPipeline = pipeline;
                    var currentBehavior = behavior;
                    
                    pipeline = () => behaviorInvoker(currentBehavior, request, currentPipeline, ct);
                }
            }

            return await pipeline();
        };
    }

    private Func<IRequest, CancellationToken, Task> CreateCompiledInvokerNoResponse(Type requestType)
    {
        var handlerType = typeof(IRequestHandler<>).MakeGenericType(requestType);
        var behaviorType = typeof(IPipelineBehavior<>).MakeGenericType(requestType);

        var handlerInvoker = CompileHandlerInvokerNoResponse(requestType, handlerType);
        var behaviorInvoker = CompileBehaviorInvokerNoResponse(requestType, behaviorType);

        return async (request, ct) =>
        {
            var serviceProvider = GetScopedServiceProvider();
            
            var behaviorsType = typeof(IEnumerable<>).MakeGenericType(behaviorType);
            var behaviors = serviceProvider.GetService(behaviorsType) as System.Collections.IEnumerable;

            Func<Task> pipeline = () => handlerInvoker(serviceProvider, request, ct);

            if (behaviors != null)
            {
                var behaviorList = behaviors.Cast<object>().Reverse().ToList();
                
                foreach (var behavior in behaviorList)
                {
                    var currentPipeline = pipeline;
                    var currentBehavior = behavior;
                    
                    pipeline = () => behaviorInvoker(currentBehavior, request, currentPipeline, ct);
                }
            }

            await pipeline();
        };
    }
}