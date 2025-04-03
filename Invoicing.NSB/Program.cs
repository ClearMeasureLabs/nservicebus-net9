using System;
using Invoicing.NSB.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

Console.Title = AppDomain.CurrentDomain.FriendlyName;
Console.WriteLine($"{AppDomain.CurrentDomain.FriendlyName} started.");

var builder = Host.CreateApplicationBuilder();

builder.Logging.AddSeq();
builder.UseNServiceBus("NServiceBusDemo.Invoicing", "invoicing");

var app = builder.Build();

await app.RunAsync();
