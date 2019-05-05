using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class HostingDiagnosticHandlerExtensions
    {
        public static IServiceCollection AddHostingDiagnosticHandler<TDiagnosticHandler>(this IServiceCollection services) where TDiagnosticHandler : HostingDiagnosticHandler
        {
            return services.AddHostedService<TDiagnosticHandler>();
        }
    }
}

namespace Microsoft.AspNetCore.Diagnostics
{
    public abstract class HostingDiagnosticHandler : IHostedService, IObserver<KeyValuePair<string, object>>, IDisposable
    {
        private readonly DiagnosticListener _diagnosticListener;
        private IDisposable _subscription;
        private PropertyHelper _hostingStartHttpContext;
        private PropertyHelper _hostingStopHttpContext;

        private PropertyHelper _hostingUnhandledHttpContext;
        private PropertyHelper _hostingUnhandledException;

        private PropertyHelper _diagnosticsHandledHttpContext;
        private PropertyHelper _diagnosticsHandledException;

        private PropertyHelper _diagnosticsUnhandledHttpContext;
        private PropertyHelper _diagnosticsUnhandledException;

        public HostingDiagnosticHandler(DiagnosticListener diagnosticListener)
        {
            _diagnosticListener = diagnosticListener;
        }

        public void OnCompleted()
        {

        }

        public void OnError(Exception error)
        {
        }

        public void OnNext(KeyValuePair<string, object> value)
        {
            switch (value.Key)
            {
                case "Microsoft.AspNetCore.Hosting.HttpRequestIn.Start":
                    {
                        var httpContextProperty = LazyInitializer.EnsureInitialized(ref _hostingStartHttpContext, () => new PropertyHelper(value.Value.GetType().GetProperty("HttpContext")));

                        var httpContext = (HttpContext)httpContextProperty.GetValue(value.Value);

                        OnHttpRequestStart(httpContext);
                    }
                    break;
                case "Microsoft.AspNetCore.Hosting.HttpRequestIn.Stop":
                    {
                        var httpContextProperty = LazyInitializer.EnsureInitialized(ref _hostingStopHttpContext, () => new PropertyHelper(value.Value.GetType().GetProperty("HttpContext")));

                        var httpContext = (HttpContext)httpContextProperty.GetValue(value.Value);

                        OnHttpRequestStop(httpContext);
                    }
                    break;
                case "Microsoft.AspNetCore.Hosting.UnhandledException":
                    {
                        var httpContextProperty = LazyInitializer.EnsureInitialized(ref _hostingUnhandledHttpContext, () => new PropertyHelper(value.Value.GetType().GetProperty("httpContext")));
                        var exceptionProperty = LazyInitializer.EnsureInitialized(ref _hostingUnhandledException, () => new PropertyHelper(value.Value.GetType().GetProperty("exception")));

                        var httpContext = (HttpContext)httpContextProperty.GetValue(value.Value);
                        var exception = (Exception)exceptionProperty.GetValue(value.Value);

                        OnHttpRequestException(httpContext, exception);
                    }
                    break;
                case "Microsoft.AspNetCore.Diagnostics.UnhandledException":
                    {
                        var httpContextProperty = LazyInitializer.EnsureInitialized(ref _diagnosticsUnhandledHttpContext, () => new PropertyHelper(value.Value.GetType().GetProperty("httpContext")));
                        var exceptionProperty = LazyInitializer.EnsureInitialized(ref _diagnosticsUnhandledException, () => new PropertyHelper(value.Value.GetType().GetProperty("exception")));

                        var httpContext = (HttpContext)httpContextProperty.GetValue(value.Value);
                        var exception = (Exception)exceptionProperty.GetValue(value.Value);

                        OnHttpRequestException(httpContext, exception);
                    }
                    break;
                case "Microsoft.AspNetCore.Diagnostics.HandledException":
                    {
                        var httpContextProperty = LazyInitializer.EnsureInitialized(ref _diagnosticsHandledHttpContext, () => new PropertyHelper(value.Value.GetType().GetProperty("httpContext")));
                        var exceptionProperty = LazyInitializer.EnsureInitialized(ref _diagnosticsHandledException, () => new PropertyHelper(value.Value.GetType().GetProperty("exception")));

                        var httpContext = (HttpContext)httpContextProperty.GetValue(value.Value);
                        var exception = (Exception)exceptionProperty.GetValue(value.Value);

                        OnHttpRequestException(httpContext, exception);
                    }
                    break;
                default:
                    break;
            }
        }

        protected virtual void OnHttpRequestStart(HttpContext httpContext)
        {

        }

        protected virtual void OnHttpRequestStop(HttpContext httpContext)
        {

        }

        protected virtual void OnHttpRequestException(HttpContext httpContext, Exception exception)
        {

        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _subscription = _diagnosticListener.Subscribe(this);
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _subscription?.Dispose();
        }
        private class PropertyHelper
        {
            // Delegate type for a by-ref property getter
            private delegate TValue ByRefFunc<TDeclaringType, TValue>(ref TDeclaringType arg);

            private static readonly MethodInfo CallPropertyGetterOpenGenericMethod =
                typeof(PropertyHelper).GetTypeInfo().GetDeclaredMethod(nameof(CallPropertyGetter));

            private static readonly MethodInfo CallPropertyGetterByReferenceOpenGenericMethod =
                typeof(PropertyHelper).GetTypeInfo().GetDeclaredMethod(nameof(CallPropertyGetterByReference));

            private Func<object, object> _valueGetter;

            public PropertyHelper(PropertyInfo property)
            {
                Property = property ?? throw new ArgumentNullException(nameof(property));
            }

            public PropertyInfo Property { get; }

            private Func<object, object> ValueGetter
            {
                get
                {
                    if (_valueGetter == null)
                    {
                        _valueGetter = MakeFastPropertyGetter(Property);
                    }

                    return _valueGetter;
                }
            }

            public object GetValue(object instance)
            {
                return ValueGetter(instance);
            }

            private static Func<object, object> MakeFastPropertyGetter(PropertyInfo propertyInfo)
            {
                Debug.Assert(propertyInfo != null);

                return MakeFastPropertyGetter(
                    propertyInfo,
                    CallPropertyGetterOpenGenericMethod,
                    CallPropertyGetterByReferenceOpenGenericMethod);
            }

            private static Func<object, object> MakeFastPropertyGetter(
                PropertyInfo propertyInfo,
                MethodInfo propertyGetterWrapperMethod,
                MethodInfo propertyGetterByRefWrapperMethod)
            {
                Debug.Assert(propertyInfo != null);

                // Must be a generic method with a Func<,> parameter
                Debug.Assert(propertyGetterWrapperMethod != null);
                Debug.Assert(propertyGetterWrapperMethod.IsGenericMethodDefinition);
                Debug.Assert(propertyGetterWrapperMethod.GetParameters().Length == 2);

                // Must be a generic method with a ByRefFunc<,> parameter
                Debug.Assert(propertyGetterByRefWrapperMethod != null);
                Debug.Assert(propertyGetterByRefWrapperMethod.IsGenericMethodDefinition);
                Debug.Assert(propertyGetterByRefWrapperMethod.GetParameters().Length == 2);

                var getMethod = propertyInfo.GetMethod;
                Debug.Assert(getMethod != null);
                Debug.Assert(!getMethod.IsStatic);
                Debug.Assert(getMethod.GetParameters().Length == 0);

                // Instance methods in the CLR can be turned into static methods where the first parameter
                // is open over "target". This parameter is always passed by reference, so we have a code
                // path for value types and a code path for reference types.
                if (getMethod.DeclaringType.GetTypeInfo().IsValueType)
                {
                    // Create a delegate (ref TDeclaringType) -> TValue
                    return MakeFastPropertyGetter(
                        typeof(ByRefFunc<,>),
                        getMethod,
                        propertyGetterByRefWrapperMethod);
                }
                else
                {
                    // Create a delegate TDeclaringType -> TValue
                    return MakeFastPropertyGetter(
                        typeof(Func<,>),
                        getMethod,
                        propertyGetterWrapperMethod);
                }
            }

            private static Func<object, object> MakeFastPropertyGetter(
                Type openGenericDelegateType,
                MethodInfo propertyGetMethod,
                MethodInfo openGenericWrapperMethod)
            {
                var typeInput = propertyGetMethod.DeclaringType;
                var typeOutput = propertyGetMethod.ReturnType;

                var delegateType = openGenericDelegateType.MakeGenericType(typeInput, typeOutput);
                var propertyGetterDelegate = propertyGetMethod.CreateDelegate(delegateType);

                var wrapperDelegateMethod = openGenericWrapperMethod.MakeGenericMethod(typeInput, typeOutput);
                var accessorDelegate = wrapperDelegateMethod.CreateDelegate(
                    typeof(Func<object, object>),
                    propertyGetterDelegate);

                return (Func<object, object>)accessorDelegate;
            }

            // Called via reflection
            private static object CallPropertyGetter<TDeclaringType, TValue>(
                Func<TDeclaringType, TValue> getter,
                object target)
            {
                return getter((TDeclaringType)target);
            }

            // Called via reflection
            private static object CallPropertyGetterByReference<TDeclaringType, TValue>(
                ByRefFunc<TDeclaringType, TValue> getter,
                object target)
            {
                var unboxed = (TDeclaringType)target;
                return getter(ref unboxed);
            }
        }
    }
}