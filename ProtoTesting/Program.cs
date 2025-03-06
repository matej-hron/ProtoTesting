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

var userEvents = new UserEventsMessage
{
    Events =
            {
                new UserEvent
                {
                    UserId = "matej",
                    //EventDate = Timestamp.FromDateTime(DateTime.UtcNow)
                },
                new UserEvent
                {
                    UserId = "ema",
                    //EventDate = Timestamp.FromDateTime(DateTime.UtcNow.AddDays(-1))
                },
                new UserEvent
                {
                    UserId = "davidbabka",
                    //EventDate = Timestamp.FromDateTime(DateTime.UtcNow.AddDays(-1))
                }
            }
};

//TestRW(filePath, filePathStreamed, userEvents);

//WriteUserEvent(new UserEvent { UserId = "julia" });
await TestReadAsync(localUrl, false);

//foreach (var item in Enumerable.Range(0, 10000))
//{
//    await RunGrpcClient(cosmicUrl, true);
//}



Console.ReadLine();

static void WriteUserEvents(UserEventsMessage userEvents)
{
    using var output = File.Create(filePath);
    userEvents.WriteTo(output);
}
static UserEventsMessage ReadUserEvents()
{
    using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
    return UserEventsMessage.Parser.ParseFrom(fs);
}

static void WriteUserEvent(UserEvent userEvent)
{
    using var output = new FileStream(filePathStreamed, FileMode.Append, FileAccess.Write);
    userEvent.WriteDelimitedTo(output);
}

static void ReadStream()
{
    using var input = new FileStream(filePathStreamed, FileMode.Open, FileAccess.Read);

    Console.WriteLine("Reading stored events:");

    while (input.Position < input.Length)
    {
        var userEvent = UserEvent.Parser.ParseDelimitedFrom(input);
        if (userEvent != null)
        {
            Console.WriteLine($"User: {userEvent.UserId}");
        }
    }
}

async Task TestReadAsync(string url, bool dummyEndpoint)
{

    using var channel = GrpcChannel.ForAddress(url);

    var request = new UserEventsReadRequest
    {
        EventType = "testevent",
        TenantId = "grpcTenant4",
        StartDate = Timestamp.FromDateTime(new DateTime(2025, 2, 1, 0, 0, 0, DateTimeKind.Utc)),
        EndDate = Timestamp.FromDateTime(new DateTime(2025, 2, 2, 0, 0, 0, DateTimeKind.Utc))
    };

    if(dummyEndpoint)
    {
        var client = new UserEvents.UserEventsClient(channel);
        using var call = client.GetUserEvents(request);

        Console.WriteLine($"Response from {url}, dummy: {dummyEndpoint}");
        await foreach (var userEvent in call.ResponseStream.ReadAllAsync<UserEvent>())
        {
            Console.WriteLine($"{userEvent.UserId}, {userEvent.EventDate}");
        }

    }
    else
    {
        var client = new UserEvents2.UserEvents2Client(channel);
        using var call = client.GetUserEvents(request);

        await foreach (var userEvent in call.ResponseStream.ReadAllAsync<UserEvent>())
        {
            Console.WriteLine($"{userEvent.UserId}, {userEvent.EventDate}");
        }

    }

    // var client = new UserEvents.UserEventsClient(channel) //: new UserEvents2.UserEvents2Client(channel); // gRPC client


    //using var call = client.GetUserEvents(request);

    //await foreach (var userEvent in call.ResponseStream.ReadAllAsync<UserEvent>())
    //{
    //    Console.WriteLine($"{userEvent.UserId}, {userEvent.EventDate}");
    //}
}

async Task TestWriteAsync(string url, AddUserEventRequest request)
{
    using var channel = GrpcChannel.ForAddress(url);
    var client = new UserEvents2.UserEvents2Client(channel);
    await client.AddUserEventAsync(request, new CallOptions { });
}
static void TestRW(string filePath, string filePathStreamed, UserEventsMessage userEvents)
{
    File.Delete(filePathStreamed);
    File.Delete(filePath);
    WriteUserEvents(userEvents);
    WriteUserEvent(new UserEvent { UserId = "davidbabka", EventDate = Timestamp.FromDateTime(DateTime.Now) });
    WriteUserEvent(new UserEvent { UserId = "ema" });
    WriteUserEvent(new UserEvent { UserId = "Evgenij" });
    Enumerable.Range(1, 10).ToList().ForEach(i => WriteUserEvent(new UserEvent { UserId = $"{i}" }));
    ReadStream();
    //var events = ReadUserEvents();
    //foreach(var e in events.Events)
    //{
    //    Console.WriteLine($"{e.UserId}, {e.EventDate}");
    //}
    //Console.WriteLine(events.Events.Count);
}