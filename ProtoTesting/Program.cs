// See https://aka.ms/new-console-template for more information
using System;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
const string filePath = @"C:\projects\grpc\user_events.pb";

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
                    UserId = "david",
                    EventDate = Timestamp.FromDateTime(DateTime.UtcNow.AddDays(-1))
                }
            }
};


InitAppend();
WriteUserEvents(userEvents);

var events = ReadUserEvents();
foreach(var e in events.Events)
{
    Console.WriteLine($"{e.UserId}, {e.EventDate}");
}
Console.WriteLine(events.Events.Count);
Console.ReadLine();

static void WriteUserEvents(UserEvents userEvents)
{
    using var output = File.Create(filePath);
    userEvents.WriteTo(output);
}

static void InitAppend()
{
    using var fs = new FileStream(filePath, FileMode.Append, FileAccess.Write, FileShare.Read);
    using var output = new CodedOutputStream(fs);

    var userEvents = new UserEvents();

    userEvents.WriteTo(output); // Write protobuf message
    output.Flush(); // Ensure all data is written
}


static UserEvents ReadUserEvents()
{
    using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
    return UserEvents.Parser.ParseFrom(fs);
}