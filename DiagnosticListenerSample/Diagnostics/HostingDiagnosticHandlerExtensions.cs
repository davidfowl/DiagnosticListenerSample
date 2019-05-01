using Microsoft.AspNetCore.Diagnostics;

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
