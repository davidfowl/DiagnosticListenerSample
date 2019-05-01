using System;
using System.Diagnostics;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;

namespace DiagnosticListenerSample
{
    public class MyDiagnosticHandler : HostingDiagnosticHandler
    {
        public MyDiagnosticHandler(DiagnosticListener diagnosticListener) : base(diagnosticListener)
        {

        }

        protected override void OnHttpRequestStart(HttpContext httpContext)
        {
            base.OnHttpRequestStart(httpContext);
        }

        protected override void OnHttpRequestStop(HttpContext httpContext)
        {
            base.OnHttpRequestStop(httpContext);
        }

        protected override void OnHttpRequestException(HttpContext httpContext, Exception exception)
        {
            // Handle exceptions here
        }
    }
}
