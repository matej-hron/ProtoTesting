using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using NBomber.Contracts;
using NBomber.CSharp;

// gRPC references
using Grpc.Net.Client;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;

// The generated stubs namespace from your .proto. 
// You said this is working: "UserEvents.UserEvents.UserEventsClient" 
using UserEvents;

namespace SettingsStoreLoadTest
{
    class Program
    {
        //-----------------------------------------------------------
        // 1) Two sets of Tenants: REST vs. gRPC
        //-----------------------------------------------------------
        private static readonly string[] REST_TENANTS = {
            "2100c1c2-e564-44fa-a537-4b6c8ae6c65c",
            "86ef99e2-f3ae-4787-be13-bc0ce0778ec9",
            "b39ca956-0dfb-4e1b-ad2c-2c5d3bb660ca",
            "6ce92d5e-7fe3-496b-ac46-37f95083e345",
            "4475a224-2eac-490e-bf39-a5dbc2b22730",
            "d9b21946-7de0-495a-9795-c0bbff81926c",
            "5f27409d-af60-4a1f-a2aa-fb54e2de0a80",
            "af6f9e75-5809-4d4f-a104-dc4c726c4590",
            "41fa7bfc-9ba3-4d25-9f15-17be889c1f48",
            "2df3ad4f-a0dc-4db9-bb96-05a6fc700950",
        };

        private static readonly string[] GRPC_TENANTS = {
            "aaaaaaaa-a564-44fa-a537-4b6c8ae6caaaa",
            "bbbbbbbb-a564-44fa-a537-4b6c8ae6cbbbb",
            "cccccccc-0dfb-4e1b-ad2c-2c5d3bb66ccc",
            "dddddddd-7fe3-496b-ac46-37f95083edde",
            "eeeeeeee-2eac-490e-bf39-a5dbc2b2ee30",
            "ffffffff-7de0-495a-9795-c0bbff81ffff",
            "11111111-af60-4a1f-a2aa-fb54e2de1111",
            "22222222-5809-4d4f-a104-dc4c726c2222",
            "33333333-9ba3-4d25-9f15-17be889c3333",
            "44444444-a0dc-4db9-bb96-05a6fc704444"
        };

        //-----------------------------------------------------------
        // 2) Round-robin counters
        //-----------------------------------------------------------
        private static int _iterRestPost = 0;
        private static int _iterRestGet  = 0;
        private static int _iterGrpcAdd  = 0;
        private static int _iterGrpcGet  = 0;

        //-----------------------------------------------------------
        // 3) REST base URL
        //-----------------------------------------------------------
        private static readonly string REST_BASE_URL = 
            "http://settingsstore.reg-int-uswe.teams-core-settingsstore.northcentralus-test.cosmic-int.office.net";

        static async Task Main(string[] args)
        {
            //-----------------------------------------------------------
            // SCENARIO #1: REST POST (like "post_scenario")
            //-----------------------------------------------------------
            var restPostScenario = Scenario.Create("rest_post_scenario", async context =>
            {
                var idx = _iterRestPost++ % REST_TENANTS.Length;
                var tenantId = REST_TENANTS[idx];
                var eventType = "testevent";

                // POST /v1.0/event/testevent/{tenantId}
                var url = $"{REST_BASE_URL}/v1.0/event/{eventType}/{tenantId}";

                var payload = new {
                    UserId = "my-unique-user",
                    EventDate = "2025-03-02T12:00:00"
                };
                var jsonBody = JsonSerializer.Serialize(payload);
                using var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

                using var httpClient = new HttpClient();
                var response = await httpClient.PostAsync(url, content);

                if ((int)response.StatusCode == 201)
                    return Response.Ok();
                else
                    return Response.Fail();
            })
            .WithLoadSimulations(
                // 5 RPS for 30s, then 10 RPS for 30s
                Simulation.Inject(rate: 5, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(30)),
                Simulation.Inject(rate: 10, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(30))
            );

            Console.WriteLine("Running REST POST scenario...");
            NBomberRunner
                .RegisterScenarios(restPostScenario)
                .Run();

            //-----------------------------------------------------------
            // SCENARIO #2: REST GET (like "get_scenario")
            //-----------------------------------------------------------
            var restGetScenario = Scenario.Create("rest_get_scenario", async context =>
            {
                var idx = _iterRestGet++ % REST_TENANTS.Length;
                var tenantId = REST_TENANTS[idx];
                var eventType = "testevent";

                // GET /v1.0/event/testevent/{tenantId}?startDate=...&endDate=...
                var startDate = "2025-03-01T00:00:00Z";
                var endDate   = "2025-03-03T00:00:00Z";
                var url = $"{REST_BASE_URL}/v1.0/event/{eventType}/{tenantId}?startDate={startDate}&endDate={endDate}";

                using var httpClient = new HttpClient();
                var response = await httpClient.GetAsync(url);

                if ((int)response.StatusCode == 200)
                    return Response.Ok();
                else
                    return Response.Fail();
            })
            .WithLoadSimulations(
                Simulation.Inject(rate: 5, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(30)),
                Simulation.Inject(rate: 10, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(30))
            );

            Console.WriteLine("Running REST GET scenario...");
            NBomberRunner
                .RegisterScenarios(restGetScenario)
                .Run();

            //-----------------------------------------------------------
            // SCENARIO #3: gRPC AddUserEvent ("post_scenario" for gRPC)
            //-----------------------------------------------------------
            var grpcAddScenario = Scenario.Create("grpc_add_scenario", async context =>
            {
                var idx = _iterGrpcAdd++ % GRPC_TENANTS.Length;
                var tenantId = GRPC_TENANTS[idx];

                using var channel = GrpcChannel.ForAddress(
                    "http://settingsstore.reg-int-uswe.teams-core-settingsstore.northcentralus-test.cosmic-int.office.net:5001"
                );

                // from your working snippet
                var client = new UserEvents.UserEvents.UserEventsClient(channel);

                try
                {
                    var request = new AddUserEventRequest
                    {
                        EventType = "testevent",
                        TenantId  = tenantId,
                        UserEvent = new UserEvent
                        {
                            UserId = "my-unique-user",
                            EventDate = new Timestamp
                            {
                                Seconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                                Nanos   = 0
                            }
                        }
                    };

                    await client.AddUserEventAsync(request);
                    return Response.Ok();
                }
                catch (RpcException)
                {
                    return Response.Fail();
                }
            })
            .WithLoadSimulations(
                Simulation.Inject(rate: 5, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(30)),
                Simulation.Inject(rate: 10, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(30))
            );

            Console.WriteLine("Running gRPC AddUserEvent scenario...");
            NBomberRunner
                .RegisterScenarios(grpcAddScenario)
                .Run();

            //-----------------------------------------------------------
            // SCENARIO #4: gRPC GetUserEvents ("get_scenario" for gRPC)
            //-----------------------------------------------------------
            var grpcGetScenario = Scenario.Create("grpc_get_scenario", async context =>
            {
                var idx = _iterGrpcGet++ % GRPC_TENANTS.Length;
                var tenantId = GRPC_TENANTS[idx];

                using var channel = GrpcChannel.ForAddress(
                    "http://settingsstore.reg-int-uswe.teams-core-settingsstore.northcentralus-test.cosmic-int.office.net:5001"
                );
                var client = new UserEvents.UserEvents.UserEventsClient(channel);

                try
                {
                    var request = new UserEventsReadRequest
                    {
                        EventType = "testevent",
                        TenantId  = tenantId,
                        StartDate = new Timestamp {
                            Seconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                            Nanos   = 0
                        },
                        EndDate   = new Timestamp {
                            Seconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                            Nanos   = 0
                        }
                    };

                    using var call = client.GetUserEvents(request);
                    while (await call.ResponseStream.MoveNext())
                    {
                        // reading each event if needed
                    }
                    return Response.Ok();
                }
                catch (RpcException)
                {
                    return Response.Fail();
                }
            })
            .WithLoadSimulations(
                Simulation.Inject(rate: 5, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(30)),
                Simulation.Inject(rate: 10, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(30))
            );

            Console.WriteLine("Running gRPC GetUserEvents scenario...");
            NBomberRunner
                .RegisterScenarios(grpcGetScenario)
                .Run();

            Console.WriteLine("All load tests completed!");
        }
    }
}
