using System;
using Microsoft.Extensions.DependencyInjection;

namespace LongRunningActions.Services
{
    public static class LongRunningServiceExtensions
    {
        public static IServiceCollection AddLongRunningJobService(this IServiceCollection services, Action<LongRunningServiceOptions> configure)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));
            if (configure == null)
                throw new ArgumentNullException(nameof(configure));
            services.Configure(configure);
            services.AddLongRunningJobService();
            return services;
        }

        public static IServiceCollection AddLongRunningJobService(this IServiceCollection services)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));
            services.AddSingleton<ILongProcessService, LongProcessService>();
            return services;
        }
    }
}
