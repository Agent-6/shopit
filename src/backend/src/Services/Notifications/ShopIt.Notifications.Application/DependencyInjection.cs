using System.Data.Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ShopIt.Notifications.Application.Emails;

namespace ShopIt.Notifications.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Email transport: SMTP. Host/port are taken from the Mailpit connection string
        // injected by Aspire (ConnectionStrings__mailpit) unless overridden in config.
        services.AddOptions<SmtpOptions>()
            .Configure<IConfiguration>((options, configuration) =>
            {
                var section = configuration.GetSection(SmtpOptions.SectionName);
                options.From = section["From"] ?? options.From;
                options.UserName = section["UserName"] ?? options.UserName;
                options.Password = section["Password"] ?? options.Password;
                if (int.TryParse(section["Port"], out var configuredPort))
                {
                    options.Port = configuredPort;
                }

                // Explicit host wins over the Aspire-injected Mailpit connection string.
                if (!string.IsNullOrWhiteSpace(section["Host"]))
                {
                    options.Host = section["Host"];
                    return;
                }

                var connectionString = configuration.GetConnectionString("mailpit");
                if (!string.IsNullOrWhiteSpace(connectionString)
                    && new DbConnectionStringBuilder { ConnectionString = connectionString }
                        .TryGetValue("Endpoint", out var endpoint)
                    && Uri.TryCreate(Convert.ToString(endpoint), UriKind.Absolute, out var uri))
                {
                    options.Host = uri.Host;
                    options.Port = uri.Port;
                    return;
                }

                options.Host ??= "localhost";
                options.Port ??= 1025;
            });

        services.AddSingleton<IEmailSender, SmtpEmailSender>();

        return services;
    }
}
