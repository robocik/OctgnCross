using Microsoft.Extensions.DependencyInjection;
using NuGet.Protocol.Core.Types;
using Octgn.ViewModels;

namespace Octgn;

public static class ServiceCollectionExtensions {
    public static void AddCommonServices(this IServiceCollection collection) {
        collection.AddSingleton<IConfiguratorLoader,DefaultConfiguratorLoader>();

        collection.AddTransient<MainViewModel>();
    }
}