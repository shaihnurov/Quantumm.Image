using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Quantumm.Image.Services.Cache;

namespace Quantumm.Image.Services.DependencyInjection
{
    /// <summary>
    /// Extension methods for registering the ImageCacheService in a DI container.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddImageCache(this IServiceCollection services, int capacity = 100)
        {
            services.AddSingleton<IImageCacheService>(sp =>
            {
                var logger = sp.GetService<ILogger<ImageCacheService>>()!;
                return new ImageCacheService(logger, capacity);
            });

            return services;
        }
    }
}
