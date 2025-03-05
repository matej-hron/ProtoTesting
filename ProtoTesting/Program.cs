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
await RunGrpcClient();
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

async Task RunGrpcClient()
{

    using var channel = GrpcChannel.ForAddress("http://localhost:25001");
    //using var channel = GrpcChannel.ForAddress("http://settingsstore.reg-int-uswe.teams-core-settingsstore.northcentralus-test.cosmic-int.office.net:5001");

    var client = new UserEvents.UserEventsClient(channel); // gRPC client

    var request = new UserEventsRequest
    {
        EventType = "login",
        TenantId = "123",
        StartDate = Timestamp.FromDateTime(DateTime.UtcNow),
        EndDate = Timestamp.FromDateTime(DateTime.UtcNow)
    };

    using var call = client.GetUserEvents(request);

    await foreach (var userEvent in call.ResponseStream.ReadAllAsync<UserEvent>())
    {
        Console.WriteLine($"{userEvent.UserId}, {userEvent.EventDate}");
    }
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