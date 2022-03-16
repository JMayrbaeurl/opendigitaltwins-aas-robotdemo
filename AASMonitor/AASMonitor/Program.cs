using AASMonitor.Data;
using Azure.Core.Pipeline;
using Azure.DigitalTwins.Core;
using Azure.Identity;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);
string adtInstanceUrl = builder.Configuration["ADTEndpoint"];

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddSingleton<WeatherForecastService>();
builder.Services.AddSingleton<ADTAASRepoService>();
builder.Services.AddHostedService<TimedHostedService>();
builder.Services.AddAzureClients(builder =>
{
    builder.AddClient<DigitalTwinsClient, DigitalTwinsClientOptions>((options, provider) =>
    {
        var appOptions = provider.GetService<IOptions<DigitalTwinsClientOptions>>();

        var credentials = new DefaultAzureCredential();
        DigitalTwinsClient client = new DigitalTwinsClient(new Uri(adtInstanceUrl),
                    credentials, new Azure.DigitalTwins.Core.DigitalTwinsClientOptions { 
                        Transport = new HttpClientTransport(new HttpClient()) });
        return client;
    });

    // First use DefaultAzureCredentials and second EnvironmentCredential to enable local docker execution
    builder.UseCredential(new ChainedTokenCredential(new DefaultAzureCredential(), new EnvironmentCredential()));
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}


app.UseStaticFiles();

app.UseRouting();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();
