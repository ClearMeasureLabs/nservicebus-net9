﻿namespace Shared
{
    public static class Endpoints
    {
        public static Endpoint Invoicing { get; } = new("NServiceBusDemo.Invoicing", "invoicing");
        public static Endpoint Orders { get; } = new("NServiceBusDemo.Orders", "orders");
        public static Endpoint OrdersApi { get; } = new("NServiceBusDemo.Orders.API", "orders");

        public sealed class Endpoint
        {
            internal Endpoint(string endpointName, string schema)
            {
                EndpointName = endpointName;
                Schema = schema;
            }

            public string EndpointName { get; }
            public string Schema { get; }
        }
    }
}
