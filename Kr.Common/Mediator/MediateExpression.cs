using System.Linq.Expressions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Kr.Common.Mediator;

public partial class Mediate
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IServiceProvider _rootServiceProvider;
    
    public Mediate(IHttpContextAccessor httpContextAccessor, IServiceProvider serviceProvider)
    {
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        _rootServiceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }
    
    private IServiceProvider GetScopedServiceProvider()
    {
        return _httpContextAccessor.HttpContext?.RequestServices ?? _rootServiceProvider;
    }
    
    /// <summary>
    /// Compiles an expression tree for handler invocation (with response)
    /// Result: (serviceProvider, request, ct) => handler.Handle(request, ct)
    /// </summary>
    private Func<IServiceProvider, IRequest<TResponse>, CancellationToken, Task<TResponse>> CompileHandlerInvoker<TResponse>(
        Type requestType, 
        Type handlerType)
    {
        // Parameters: serviceProvider, request, cancellationToken
        var serviceProviderParam = Expression.Parameter(typeof(IServiceProvider), "serviceProvider");
        var requestParam = Expression.Parameter(typeof(IRequest<TResponse>), "request");
        var ctParam = Expression.Parameter(typeof(CancellationToken), "ct");
        
        // Resolve handler: serviceProvider.GetRequiredService<THandler>()
        var getServiceMethod = typeof(ServiceProviderServiceExtensions)
            .GetMethod(nameof(ServiceProviderServiceExtensions.GetRequiredService), new[] { typeof(IServiceProvider) })!
            .MakeGenericMethod(handlerType);
        
        var handlerExpr = Expression.Call(null, getServiceMethod, serviceProviderParam);
        
        // Call handler.Handle(request, ct)
        var handleMethod = handlerType.GetMethod("Handle")!;
        var handleCall = Expression.Call(
            handlerExpr, 
            handleMethod, 
            Expression.Convert(requestParam, requestType), // Cast IRequest<T> to concrete type
            ctParam
        );
        
        // Compile: (serviceProvider, request, ct) => handler.Handle((TRequest)request, ct)
        var lambda = Expression.Lambda<Func<IServiceProvider, IRequest<TResponse>, CancellationToken, Task<TResponse>>>(
            handleCall, 
            serviceProviderParam,
            requestParam, 
            ctParam
        );
        
        return lambda.Compile();
    }

    /// <summary>
    /// Compiles an expression tree for handler invocation (no response)
    /// </summary>
    private Func<IServiceProvider, IRequest, CancellationToken, Task> CompileHandlerInvokerNoResponse(
        Type requestType, 
        Type handlerType)
    {
        var serviceProviderParam = Expression.Parameter(typeof(IServiceProvider), "serviceProvider");
        var requestParam = Expression.Parameter(typeof(IRequest), "request");
        var ctParam = Expression.Parameter(typeof(CancellationToken), "ct");
        
        var getServiceMethod = typeof(ServiceProviderServiceExtensions)
            .GetMethod(nameof(ServiceProviderServiceExtensions.GetRequiredService), new[] { typeof(IServiceProvider) })!
            .MakeGenericMethod(handlerType);
        
        var handlerExpr = Expression.Call(null, getServiceMethod, serviceProviderParam);
        var handleMethod = handlerType.GetMethod("Handle")!;
        var handleCall = Expression.Call(
            handlerExpr, 
            handleMethod, 
            Expression.Convert(requestParam, requestType),
            ctParam
        );
        
        var lambda = Expression.Lambda<Func<IServiceProvider, IRequest, CancellationToken, Task>>(
            handleCall, 
            serviceProviderParam,
            requestParam, 
            ctParam
        );
        
        return lambda.Compile();
    }

    /// <summary>
    /// Compiles an expression tree for behavior invocation (with response)
    /// Result: (behavior, request, next, ct) => behavior.Handle(request, next, ct)
    /// </summary>
    private Func<object, IRequest<TResponse>, RequestHandlerDelegate<TResponse>, CancellationToken, Task<TResponse>> 
        CompileBehaviorInvoker<TResponse>(Type requestType, Type behaviorType)
    {
        var behaviorParam = Expression.Parameter(typeof(object), "behavior");
        var requestParam = Expression.Parameter(typeof(IRequest<TResponse>), "request");
        var nextParam = Expression.Parameter(typeof(RequestHandlerDelegate<TResponse>), "next");
        var ctParam = Expression.Parameter(typeof(CancellationToken), "ct");
        
        // Cast behavior from object to specific type
        var behaviorCast = Expression.Convert(behaviorParam, behaviorType);
        
        // Call behavior.Handle(request, next, ct)
        var handleMethod = behaviorType.GetMethod("Handle")!;
        var handleCall = Expression.Call(
            behaviorCast,
            handleMethod,
            Expression.Convert(requestParam, requestType),
            nextParam,
            ctParam
        );
        
        var lambda = Expression.Lambda<Func<object, IRequest<TResponse>, RequestHandlerDelegate<TResponse>, CancellationToken, Task<TResponse>>>(
            handleCall,
            behaviorParam,
            requestParam,
            nextParam,
            ctParam
        );
        
        return lambda.Compile();
    }

    /// <summary>
    /// Compiles an expression tree for behavior invocation (no response)
    /// </summary>
    private Func<object, IRequest, Func<Task>, CancellationToken, Task> 
        CompileBehaviorInvokerNoResponse(Type requestType, Type behaviorType)
    {
        var behaviorParam = Expression.Parameter(typeof(object), "behavior");
        var requestParam = Expression.Parameter(typeof(IRequest), "request");
        var nextParam = Expression.Parameter(typeof(Func<Task>), "next");
        var ctParam = Expression.Parameter(typeof(CancellationToken), "ct");
        
        var behaviorCast = Expression.Convert(behaviorParam, behaviorType);
        var handleMethod = behaviorType.GetMethod("Handle")!;
        var handleCall = Expression.Call(
            behaviorCast,
            handleMethod,
            Expression.Convert(requestParam, requestType),
            nextParam,
            ctParam
        );
        
        var lambda = Expression.Lambda<Func<object, IRequest, Func<Task>, CancellationToken, Task>>(
            handleCall,
            behaviorParam,
            requestParam,
            nextParam,
            ctParam
        );
        
        return lambda.Compile();
    }
}