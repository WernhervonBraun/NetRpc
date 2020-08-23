﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using DataContract;
using NetRpc.Grpc;

namespace Client
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var p = NManager.CreateClientProxy<IService>(new GrpcClientOptions
            {
                Host = "localhost",
                Port = 50001
            });

            int f = 0;
            while (true)
            {
                f++;
                Console.WriteLine(f);
                await p.Proxy.Call("hello world.");
            }

            Console.Read();
        }
    }
}