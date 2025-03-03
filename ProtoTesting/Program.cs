// See https://aka.ms/new-console-template for more information
using System;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
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
                    EventDate = Timestamp.FromDateTime(DateTime.UtcNow)
                },
                new UserEvent
                {
                    UserId = "ema",
                    EventDate = Timestamp.FromDateTime(DateTime.UtcNow.AddDays(-1))
                },
                new UserEvent
                {
                    UserId = "davidbabka",
                    EventDate = Timestamp.FromDateTime(DateTime.UtcNow.AddDays(-1))
                }
            }
};

//TestRW(filePath, filePathStreamed, userEvents);

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
            Console.WriteLine($"User: {userEvent.UserId}, Event Date: {userEvent.EventDate.ToDateTime():u}");
        }
    }
}

async Task RunGrpcClient()
{
    // Replace with the correct namespace if different
    using var channel = Grpc.Net.Client.GrpcChannel.ForAddress("http://localhost:20344");
    var client = new SettingsStore.WebAppService.Core.Grpc.UserEvents.UserEventsClient(channel); // gRPC client

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
        Console.WriteLine($"{userEvent.UserId}");
    }
}

static void TestRW(string filePath, string filePathStreamed, UserEventsMessage userEvents)
{
    File.Delete(filePathStreamed);
    File.Delete(filePath);
    WriteUserEvents(userEvents);
    WriteUserEvent(new UserEvent { UserId = "davidbabka", EventDate = Timestamp.FromDateTime(DateTime.UtcNow.AddDays(-1)) });
    WriteUserEvent(new UserEvent { UserId = "ema", EventDate = Timestamp.FromDateTime(DateTime.UtcNow.AddDays(-1)) });
    WriteUserEvent(new UserEvent { UserId = "Evgenij", EventDate = Timestamp.FromDateTime(DateTime.UtcNow.AddDays(-1)) });
    Enumerable.Range(1, 10).ToList().ForEach(i => WriteUserEvent(new UserEvent { UserId = $"{i}", EventDate = Timestamp.FromDateTime(DateTime.UtcNow.AddDays(-1)) }));
    ReadStream();
    //var events = ReadUserEvents();
    //foreach(var e in events.Events)
    //{
    //    Console.WriteLine($"{e.UserId}, {e.EventDate}");
    //}
    //Console.WriteLine(events.Events.Count);
}