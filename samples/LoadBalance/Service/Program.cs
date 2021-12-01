﻿using System;
using System.IO;
using System.Threading.Tasks;
using DataContract;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Helper = TestHelper.Helper;

namespace Service;

internal class Program
{
    private static async Task Main(string[] args)
    {
        var mpHost = new HostBuilder()
            .ConfigureServices((_, services) =>
            {
                services.AddNRabbitMQService(i => i.CopyFrom(Helper.GetMQOptions()));
                services.AddNServiceContract<IServiceAsync, ServiceAsync>();
            })
            .Build();
        Console.WriteLine("Service Opened.");
        await mpHost.RunAsync();
    }
}

internal class ServiceAsync : IServiceAsync
{
    public async Task CallAsync(Func<int, Task> cb, string s1)
    {
        Console.Write($"Receive: {s1}, start...");
        for (var i = 0; i < 10; i++)
        {
            await Task.Delay(1000);
            await cb(i);
            Console.Write($"{i}, ");
        }

        Console.WriteLine("end");
    }

    public async Task PostAsync(string s1, Stream data)
    {
        Console.Write($"Receive: {s1}, stream:{Helper.ReadStr(data)}, start...");
        for (var i = 0; i < 3; i++)
        {
            await Task.Delay(1000);
            Console.Write($"{i}, ");
        }

        Console.WriteLine("end");
    }
}