using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Internal;

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
    }
}
