// See https://aka.ms/new-console-template for more information
using System;
using System.Net;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Grpc.Net.Client;
using SettingsStore.WebAppService.Core.Grpc;
const string filePath = @"C:\projects\grpc\user_events.pb";
const string filePathStreamed = @"C:\projects\grpc\user_events_stream.pb";
const string localUrl = "http://localhost:25001";
const string cosmicUrl = "http://settingsstore.reg-int-uswe.teams-core-settingsstore.northcentralus-test.cosmic-int.office.net:5001";

const string tenantId = "testGrpc101";

//await TestWriteAsync(localUrl, new AddUserEventRequest
//{
//    EventType = "testevent",
//    TenantId = tenantId,
//    UserEvent = new UserEvent
//    {
//        UserId = "Damiano",
//        EventDate = Timestamp.FromDateTime(new DateTime(2025, 2, 1, 0, 0, 0, DateTimeKind.Utc))
//    }
//});
//await TestWriteAsync(localUrl, new AddUserEventRequest
//{
//    EventType = "testevent",
//    TenantId = tenantId,
//    UserEvent = new UserEvent
//    {
//        UserId = "Emilio",
//        EventDate = Timestamp.FromDateTime(new DateTime(2025, 2, 1, 0, 0, 0, DateTimeKind.Utc))
//    }
//});

await TestReadAsync(cosmicUrl, false, tenantId, new DateTime(2025, 2, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 2, 1, 0, 0, 0, DateTimeKind.Utc));

Console.ReadLine();

async Task TestReadAsync(string url, bool dummyEndpoint, string tenantId, DateTime from, DateTime to)
{

    using var channel = GrpcChannel.ForAddress(url);

    var request = new UserEventsReadRequest
    {
        EventType = "testevent",
        TenantId = tenantId,
        StartDate = Timestamp.FromDateTime(from),
        EndDate = Timestamp.FromDateTime(to)
    };

    if(dummyEndpoint)
    {
        var client = new UserEventsDummy.UserEventsDummyClient(channel);
        using var call = client.GetUserEvents(request);

        Console.WriteLine($"Response from {url}, dummy: {dummyEndpoint}");
        await foreach (var userEvent in call.ResponseStream.ReadAllAsync<UserEvent>())
        {
            Console.WriteLine($"{userEvent.UserId}, {userEvent.EventDate}");
        }

    }
    else
    {
        var client = new UserEvents.UserEventsClient(channel);
        using var call = client.GetUserEvents(request);

        await foreach (var userEvent in call.ResponseStream.ReadAllAsync<UserEvent>())
        {
            Console.WriteLine($"{userEvent.UserId}, {userEvent.EventDate}");
        }

    }
}

async Task TestWriteAsync(string url, AddUserEventRequest request)
{
    using var channel = GrpcChannel.ForAddress(url);
    var client = new UserEvents.UserEventsClient(channel);
    await client.AddUserEventAsync(request, new CallOptions { });
}