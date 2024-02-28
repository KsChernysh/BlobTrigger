using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SendGrid;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices(services =>
    {
        services.AddSingleton<ISendGridClient>(new SendGridClient(Environment.GetEnvironmentVariable("SENDGRID_API_KEY")));
    })
    .Build();

host.Run();