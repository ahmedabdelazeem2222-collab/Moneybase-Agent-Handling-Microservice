using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MoneyBase.Support.Infrastructure.HostedServices;

namespace MoneyBase.Support.Infrastructure.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddMoneyBaseServices(this IServiceCollection services, IConfiguration configuration)
        {
            return services;
        }
        public static IServiceCollection AddHostedServices(this IServiceCollection services)
        {
            services.AddHostedService<MultiAgentHandlerService>();
            return services;
        }
    }
}
