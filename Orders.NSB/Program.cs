using System;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Orders.API.Extensions;

Console.Title = AppDomain.CurrentDomain.FriendlyName;
Console.WriteLine($"{AppDomain.CurrentDomain.FriendlyName} started.");

var builder = Host.CreateApplicationBuilder();

builder.Logging.AddSeq();

builder.UseNServiceBus("NServiceBusDemo.Orders", "orders");

var app = builder.Build();

await app.RunAsync();
