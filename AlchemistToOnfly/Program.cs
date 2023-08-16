// See https://aka.ms/new-console-template for more information
using BimAndCo.Alchemist.Web.Api.Clients.CSharp;
using BimAndCo.Alchemist.Web.Api.Clients.CSharp.Contracts;
using BimAndCo.Api.Client.CSharp;
using BimAndCo.Api.Client.CSharp.Contracts;

using ConsoleApp1.SDK;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using System.Collections.ObjectModel;

void ConfigureEnvironment(IServiceProvider serviceProvider)
{
    serviceProvider.GetRequiredService<IEnvironmentManager>().SetEnvironment(BCEnvironment.PROD);
    serviceProvider.GetRequiredService<IEnvironmentManager>().SetSSOEnvironment(BCEnvironment.PROD);
    serviceProvider.GetRequiredService<IEnvironmentManager>().SetAlchemistEnvironment(BCEnvironment.PROD);
}

using IHost host = CreateHostBuilder(args, ConfigureEnvironment).Build();

using IServiceScope scope = host.Services.CreateScope();
IServiceProvider services = scope.ServiceProvider;

/** INITIALIZATION **/
ConfigureEnvironment(services);

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

/*string userName = "bjean@bimandco.com";
string password = "";*/

/*Guid spaceId = Guid.Parse("97d53114-615a-4ddb-8ae5-8f82080c6645");
Guid repositoryId = Guid.Parse("07d96202-c3ae-4d86-bce3-3fe9c942a7fd");*/

Guid spaceId = Guid.Parse("1ef56a31-8fa5-49fc-abae-b19a17077469");
Guid repositoryId = Guid.Parse("e7355c4e-b89b-4698-9fc7-b81f0b9f6fc0");

string dataCulture = "en";

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

IRepositoryClient repositoryClient = services.GetRequiredService<IRepositoryClient>();
IUploadBundlesClient uploadBundlesClient = services.GetRequiredService<IUploadBundlesClient>();

/** RETRIEVE PARAMETERS **/

ObservableCollection<ParameterStreamingResponseDto> parametersResponse;
try
{
    parametersResponse = await repositoryClient.GetTableParametersAsync(spaceId, repositoryId, null, "1.0");
    Console.WriteLine("Columns retrieve !");
}
catch (Exception ex)
{
    Console.WriteLine("Error Retrieving columns");
    Console.WriteLine(ex.ToString());
    return;
}

/** RETREIVE ENTITIES **/

int from = 0;
int size = 100;
bool hasNextResult = true;

while (hasNextResult) {

    ListPaginatedEntitiesResponseDto results = await repositoryClient.ListEntitiesPaginatedAsync(spaceId, repositoryId, from, size, "1.0");

    UploadBundlesRequestFromBody uploadRequest = new UploadBundlesRequestFromBody()
    {
        Bundles = new ObservableCollection<UploadBundleRequest2>(
            results.Entities.Select(item => new UploadBundleRequest2()
            {
                Guid = Guid.NewGuid().ToString(),
                Bundle = new SmBundleV5()
                {
                    BundleType = BundleType.Upload,
                    BimObject = new SmBimObjectV5()
                    {
                        Definitions = new ObservableCollection<SmDefinition> { new SmDefinition() { Name = "My Object Name", LanguageCode = dataCulture, IsDefault = true }},
                        Variants = new ObservableCollection<SmVariant>()
                        {
                            new SmVariant()
                            {
                                Name= "REF1",
                                VariantValues = new ObservableCollection<SmVariantValue>(
                                    item.Values
                                    .Join(
                                        parametersResponse,
                                        value => value.ParameterId,
                                        parameter => parameter.Id,
                                        (value, parameter) => new
                                        {
                                            value,
                                            parameter
                                        }
                                    )
                                    .Where(r => r.parameter.OnflyPropertyGuid != Guid.Empty)
                                    .Select(r => new SmVariantValue()
                                    {
                                        Property = new SmProperty()
                                        {
                                            Guid = r.parameter.OnflyPropertyGuid
                                        },
                                        Values = new ObservableCollection<SmValue>()
                                        {
                                            new SmValue()
                                            {
                                                Value = r.value.Value,
                                                LanguageCode = dataCulture
                                            }
                                        }
                                    })
                                ),
                            }
                        }
                    }
                }
            }
        )),
        GlobalGuid = Guid.NewGuid().ToString(),
    };

    await uploadBundlesClient.UploadBundlesAsync(uploadRequest);

    hasNextResult = results?.HasNext ?? false;
    from += size;
}

// DEPENDENCY INJECTION

IHostBuilder CreateHostBuilder(string[] strings, Action<IServiceProvider> configureServices)
{

    return Host.CreateDefaultBuilder()
        .ConfigureServices((_, services) =>
        {
            services.AddSingleton<IEnvironmentManager, EnvironmentManager>();

            var sp = services.BuildServiceProvider();

            configureServices.Invoke(sp);

            var createHttpClientAlchemist = (HttpClient client) =>
            {
                client.BaseAddress = new Uri(sp.GetRequiredService<IEnvironmentManager>().GetAlchemistApiUrl());
            };

            var createHttpClientOnfly = (HttpClient client) =>
            {
                client.BaseAddress = new Uri(sp.GetRequiredService<IEnvironmentManager>().GetPlatformApiUrl());
            };

            services.AddSingleton<IAuthenticationManager, AuthenticationManager>();
            
            services.AddTransient<AddHeadersHandler>();

            services
                .AddHttpClient<IUploadBundlesClient, UploadBundlesClient>(createHttpClientOnfly)
                .AddHttpMessageHandler<AddHeadersHandler>();

            services
                .AddHttpClient<IRepositoryClient, RepositoryClient>(createHttpClientAlchemist)
                .AddHttpMessageHandler<AddHeadersHandler>();

        });
}
