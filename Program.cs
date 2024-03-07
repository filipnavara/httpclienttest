using System.Diagnostics.Tracing;
using System.Net;
using System.Text;

class Program
{
    public static async Task<int> Main(string[] args)
    {
        if (args.Length != 3)
        {
            Console.WriteLine("Usage: {0} <url> <username> <password>", Environment.ProcessPath);
            return 1;
        }

        using var listener = new HttpEventListener();
        try
        {
            using SocketsHttpHandler httpHandler = new SocketsHttpHandler();
            httpHandler.Credentials = new NetworkCredential(args[1], args[2]);
            using HttpClient httpClient = new HttpClient(httpHandler);
            var result = await httpClient.GetAsync(args[0]);
            Console.WriteLine($"HTTP status: {result.StatusCode}");
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }

        return 0;
    }
}

internal sealed class HttpEventListener : EventListener
{
    public const EventKeywords TasksFlowActivityIds = (EventKeywords)0x80;
    public const EventKeywords Debug = (EventKeywords)0x20000;
    protected override void OnEventSourceCreated(EventSource eventSource)
    {
        if (eventSource.Name.Contains("Http") || eventSource.Name.Contains("Security"))
        {
            EnableEvents(eventSource, EventLevel.LogAlways, EventKeywords.All);
        }
    }
    protected override void OnEventWritten(EventWrittenEventArgs eventData)
    {
        // It's a counter, parse the data properly.
        if (eventData.EventId == -1)
        {
            var sb = new StringBuilder().Append($"{eventData.TimeStamp:HH:mm:ss.fffffff}  {eventData.EventSource.Name}  ");
            var counterPayload = (IDictionary<string, object>)(eventData.Payload[0]);
            bool appendSeparator = false;
            foreach (var counterData in counterPayload)
            {
                if (appendSeparator)
                {
                    sb.Append(", ");
                }
                sb.Append(counterData.Key).Append(": ").Append(counterData.Value);
                appendSeparator = true;
            }
            Console.WriteLine(sb.ToString());
        }
        else
        {
            var sb = new StringBuilder().Append($"{eventData.ActivityId}.{eventData.RelatedActivityId}  {eventData.EventSource.Name}.{eventData.EventName}(");
            for (int i = 0; i < eventData.Payload?.Count; i++)
            {
                sb.Append(eventData.PayloadNames?[i]).Append(": ").Append(eventData.Payload[i]);
                if (i < eventData.Payload?.Count - 1)
                {
                    sb.Append(", ");
                }
            }
            sb.Append(")");
            Console.WriteLine(sb.ToString());
        }
    }
}
