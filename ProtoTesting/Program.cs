// See https://aka.ms/new-console-template for more information
using System;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
const string filePath = @"C:\projects\grpc\user_events.pb";
const string filePathStreamed = @"C:\projects\grpc\user_events_stream.pb";

var userEvents = new UserEvents
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

File.Delete(filePathStreamed);
File.Delete(filePath);
WriteUserEvents(userEvents);
WriteUserEvent(new UserEvent { UserId = "davidbabka", EventDate = Timestamp.FromDateTime(DateTime.UtcNow.AddDays(-1)) });
WriteUserEvent(new UserEvent { UserId = "ema", EventDate = Timestamp.FromDateTime(DateTime.UtcNow.AddDays(-1)) });
WriteUserEvent(new UserEvent { UserId = "Evgenij", EventDate = Timestamp.FromDateTime(DateTime.UtcNow.AddDays(-1)) });
ReadStream();
//var events = ReadUserEvents();
//foreach(var e in events.Events)
//{
//    Console.WriteLine($"{e.UserId}, {e.EventDate}");
//}
//Console.WriteLine(events.Events.Count);
Console.ReadLine();

static void WriteUserEvents(UserEvents userEvents)
{
    using var output = File.Create(filePath);
    userEvents.WriteTo(output);
}
static UserEvents ReadUserEvents()
{
    using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
    return UserEvents.Parser.ParseFrom(fs);
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