// See https://aka.ms/new-console-template for more information
using BimAndCo.Alchemist.Web.Api.Clients.CSharp;
using BimAndCo.Alchemist.Web.Api.Clients.CSharp.Contracts;

using ConsoleApp1.SDK;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using System.Collections.ObjectModel;

void ConfigureEnvironment(IServiceProvider serviceProvider)
{
    serviceProvider.GetRequiredService<IEnvironmentManager>().SetEnvironment(BCEnvironment.DEV);
    serviceProvider.GetRequiredService<IEnvironmentManager>().SetSSOEnvironment(BCEnvironment.PROD);
    serviceProvider.GetRequiredService<IEnvironmentManager>().SetAlchemistEnvironment(BCEnvironment.DEV);
}

using IHost host = CreateHostBuilder(args, ConfigureEnvironment).Build();

using IServiceScope scope = host.Services.CreateScope();
IServiceProvider services = scope.ServiceProvider;

/** INITIALIZATION **/
ConfigureEnvironment(services);

/*Console.WriteLine("Please enter your email ?");
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
} while (key != ConsoleKey.Enter);*/

string userName = "bjean@bimandco.com";
string password = "4dohCvcH8";

/*string userName = "bjean@bimandco.com";
string password = "";*/

/*Guid spaceId = Guid.Parse("97d53114-615a-4ddb-8ae5-8f82080c6645");
Guid repositoryId = Guid.Parse("07d96202-c3ae-4d86-bce3-3fe9c942a7fd");*/

Guid spaceId = Guid.Parse("1ef56a31-8fa5-49fc-abae-b19a17077469");
Guid repositoryId = Guid.Parse("e7355c4e-b89b-4698-9fc7-b81f0b9f6fc0");


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

IBatchClient batchClient = services.GetRequiredService<IBatchClient>();

/** ADD COLUMNS **/

try
{
    AddColumnsBatchCommitResponseDto response = await batchClient.AddColumnsBatchAsync("1.0", spaceId, repositoryId, new AddColumnsBatchCommitRequestDto()
    {
        SubRequests = new ObservableCollection<AddColumnsBatchCommitSubRequestDto>()
        {
            new AddColumnsBatchCommitSubRequestDto()
            {
                Name = "My property Toto",
                ParameterId = Guid.NewGuid().ToString(),
                ParameterDataType = "4",
                ParameterKind = "2"
            },
            new AddColumnsBatchCommitSubRequestDto()
            {
                Name = "My property Titi",
                ParameterId = Guid.NewGuid().ToString(),
                ParameterDataType = "4",
                ParameterKind = "2"
            }
        }
    });

    Console.WriteLine("Columns Created !");
}
catch (Exception ex)
{
    Console.WriteLine("Error Creating columns");
    Console.WriteLine(ex.ToString());
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


/** ADD LINES **/

try
{
    AddLinesBatchCommitResponseDto response = await batchClient.AddLinesBatchAsync("1.0", spaceId, repositoryId, new BimAndCo.Alchemist.Web.Api.Clients.CSharp.Contracts.AddLinesBatchCommitRequestDto()
    {
        Entities = new ObservableCollection<NewEntityDto>()
        {
            new BimAndCo.Alchemist.Web.Api.Clients.CSharp.Contracts.NewEntityDto()
            {
                EntityCadId = Guid.NewGuid().ToString(),
                EntityName = "My Object Name",
                Values = new ObservableCollection<NewCellValueDto>()
                {
                    new NewCellValueDto()
                    {
                        ParameterId = parametersResponse.FirstOrDefault(p => p.Name == "My property Toto").Id,
                        Value = "toto"
                    },
                     new NewCellValueDto()
                    {
                        ParameterId = parametersResponse.FirstOrDefault(p => p.Name == "My property Titi").Id,
                        Value = "titi"
                    },
                },
            }
        }
    });

    Console.WriteLine("Entity Created !");
}
catch(Exception ex)
{
    Console.WriteLine("Error Creating entity");
    Console.WriteLine(ex.ToString());
    return;
}


// DEPENDENCY INJECTION

IHostBuilder CreateHostBuilder(string[] strings, Action<IServiceProvider> configureEnvironment)
{

    return Host.CreateDefaultBuilder()
        .ConfigureServices((_, services) =>
        {
            services.AddSingleton<IEnvironmentManager, EnvironmentManager>();

            var sp = services.BuildServiceProvider();
            configureEnvironment.Invoke(sp);

            var createHttpClient = (HttpClient client) =>
            {
                client.BaseAddress = new Uri(sp.GetRequiredService<IEnvironmentManager>().GetAlchemistApiUrl());
            };

            services.AddSingleton<IAuthenticationManager, AuthenticationManager>();

            services.AddTransient<AddHeadersHandler>();

            services
                .AddHttpClient<IBatchClient, BatchClient>(createHttpClient)
                .AddHttpMessageHandler<AddHeadersHandler>();

            services
                .AddHttpClient<IRepositoryClient, RepositoryClient>(createHttpClient)
                .AddHttpMessageHandler<AddHeadersHandler>();
        });
}


