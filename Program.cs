// See https://aka.ms/new-console-template for more information


using ChatSystem.Client.Extensions;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ChatSystem.Client.Console;


public class Program
{
    public static async Task Main(string[] args)
    {
        var host = Host.CreateDefaultBuilder(args)
            .ConfigureServices((context, services) =>
            {
                services.AddChatClient();
                services.AddTransient<ConsoleApplication>();
            })
            .Build();

        var app = host.Services.GetRequiredService<ConsoleApplication>();
        await app.RunAsync();
    }
}
