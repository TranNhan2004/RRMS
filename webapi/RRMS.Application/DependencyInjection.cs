using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using RRMS.Application.Interfaces.Services.FileStorages;
using RRMS.Application.Services.FileStorages;

namespace RRMS.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddScoped<RRMS.Application.Interfaces.Services.Addresses.IAddressQueryService, RRMS.Application.Services.Addresses.AddressQueryService>();
        services.AddScoped<RRMS.Application.Interfaces.Services.Users.IUserQueryService, RRMS.Application.Services.Users.UserQueryService>();

        services.AddScoped<IFileStorageCommandService, FileStorageCommandService>();
        services.AddScoped<IFileStorageQueryService, FileStorageQueryService>();


        return services;
    }
}
