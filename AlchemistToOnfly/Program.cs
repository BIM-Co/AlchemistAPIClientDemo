// See https://aka.ms/new-console-template for more information
using BimAndCo.Alchemist.Web.Api.Clients.CSharp;
using BimAndCo.Alchemist.Web.Api.Clients.CSharp.Contracts;
using BimAndCo.Api.Client.CSharp;
using BimAndCo.Api.Client.CSharp.Contracts;
using ConsoleApp1.SDK;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Collections.ObjectModel;

using IHost host = CreateHostBuilder(args).Build();

using IServiceScope scope = host.Services.CreateScope();
IServiceProvider services = scope.ServiceProvider;

/** INITIALIZATION **/

Console.WriteLine("Please enter your email ?");
string userName = Console.ReadLine();
Console.WriteLine("Please enter your password ?");
string password = "";
ConsoleKey key;
do
{
    var keyInfo = Console.ReadKey(intercept: true);
    key = keyInfo.Key;

    if (key == ConsoleKey.Backspace && password.Length > 0)
    {
        Console.Write("\b \b");
        password = password[0..^1];
    }
    else if (!char.IsControl(keyInfo.KeyChar))
    {
        Console.Write("*");
        password += keyInfo.KeyChar;
    }
} while (key != ConsoleKey.Enter);

string spaceId = "05b674d9-186f-474d-ae4d-076dd716ddff";
string repositoryId = "653f4b60-8a4a-4df1-b371-e4b93ccd38c8";

/** AUTHENTICATION **/

IAuthenticationManager authenticationManager = services.GetRequiredService<IAuthenticationManager>();
await authenticationManager.Initialize();
bool isAuthenticated = await authenticationManager.GetAuthenticationTokenWithCredentialAsync(userName, password);
Guid userId = Guid.Empty;

if (isAuthenticated)
{
    Console.WriteLine($"SuccessFully Authenticated as {authenticationManager.GetUserEmail()} ({authenticationManager.GetUserGuid()})");
    userId = Guid.Parse(authenticationManager.GetUserGuid());
}
else
{
    Console.WriteLine("Authentication Failed");
    return;
}

/** RETRIEVE PARAMETERS **/

IRepositoryClient repositoryClient = services.GetRequiredService<IRepositoryClient>();
ObservableCollection<ParameterStreamingResponseDto> parametersResponse;
try
{
    parametersResponse = await repositoryClient.GetTableParametersAsync(Guid.Parse(spaceId), Guid.Parse(repositoryId), null, "1.0");
    Console.WriteLine("Columns retrieve !");
}
catch (Exception ex)
{
    Console.WriteLine("Error Retrieving columns");
    Console.WriteLine(ex.ToString());
    return;
}



IUploadBundlesClient uploadBundlesClient = services.GetRequiredService<IUploadBundlesClient>();
UploadBundlesRequestFromBody uploadRequest = new UploadBundlesRequestFromBody()
{
    Bundles = new ObservableCollection<UploadBundleRequest2>()
    {
        new UploadBundleRequest2(){
            Guid = Guid.NewGuid().ToString(),
            Bundle = new SmBundleV5()
            {
                BundleType = BundleType.Upload,
                BimObject = new SmBimObjectV5()
                {
                    Variants = new ObservableCollection<SmVariant>()
                    {

                    }
                }
            }
        }
    },
    GlobalGuid = Guid.NewGuid().ToString(),
};
await uploadBundlesClient.UploadBundlesAsync(uploadRequest);

// DEPENDENCY INJECTION

IHostBuilder CreateHostBuilder(string[] strings)
{

    return Host.CreateDefaultBuilder()
        .ConfigureServices((_, services) =>
        {
            services.AddSingleton<IEnvironmentManager, EnvironmentManager>();

            var sp = services.BuildServiceProvider();

            var createHttpClient = (HttpClient client) =>
            {
                client.BaseAddress = new Uri(sp.GetRequiredService<IEnvironmentManager>().GetAlchemistApiUrl());
            };

            services.AddSingleton<IAuthenticationManager, AuthenticationManager>();
            services.AddTransient<AddHeadersHandler>();
            services.AddHttpClient<IUploadBundlesClient, UploadBundlesClient>();
            services.AddHttpClient<IBatchClient, BatchClient>(createHttpClient).AddHttpMessageHandler<AddHeadersHandler>();
            services.AddHttpClient<IRepositoryClient, RepositoryClient>(createHttpClient).AddHttpMessageHandler<AddHeadersHandler>();
        });
}