using Azure.Core;
using Azure.Identity;

namespace VoiceAgent;

public static class AzureHelpers
{
    public static IServiceCollection AddAzureTokenCredential(this IServiceCollection s)
        => s.AddSingleton(ResolveTokenCredential);

    private static TokenCredential ResolveTokenCredential(IServiceProvider services)
    {
        var tenantId = services.GetRequiredService<IConfiguration>().GetValue<string>("AzureTenantId");
        var managedIdentityObjectId = services.GetRequiredService<IConfiguration>().GetValue<string>("ManagedIdentityClientId");

        if (string.IsNullOrWhiteSpace(managedIdentityObjectId))
        {
            var options = new AzureCliCredentialOptions();
            if (!string.IsNullOrEmpty(tenantId)) options.TenantId = tenantId;
            return new AzureCliCredential(options);
        }
        else
        {
            DefaultAzureCredentialOptions options = new()
            {
                ManagedIdentityClientId = managedIdentityObjectId,
                WorkloadIdentityClientId = managedIdentityObjectId
            };
            return new DefaultAzureCredential(options);
        }
    }
}