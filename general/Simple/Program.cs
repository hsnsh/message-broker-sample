using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json.Linq;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Simple;

internal static class Program
{
    private static int _waitSampleWorkTime = 1000;

    private const string RabbitMqHost = "http://localhost:15672";
    private const string Username = "guest";
    private const string Password = "guest";
    private const string VHost = "%2f"; // default vhost için URL encoded "/"

    public static async Task Main(string[] args)
    {
        var connectionFactory = new ConnectionFactory
        {
            HostName = "localhost",
            Port = 5672,
            UserName = Username,
            Password = Password,
            VirtualHost = "/"
        };

        try
        {
            await ClearExchangeAllQueuesAsync("EDS");

            //await PublishAsync(connectionFactory, 100);

            //await SimpleConsumeAsync(connectionFactory, 10);
            //await SemaphoreConsumeAsync(connectionFactory, 10);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }

    private static async Task PublishAsync(ConnectionFactory connectionFactory, int publishCount = 1)
    {
        if (publishCount < 1) publishCount = 1;

        await using var connection = await connectionFactory.CreateConnectionAsync();
        await using var channel = await connection.CreateChannelAsync();

        await channel.QueueDeclareAsync("iskuyrugu", durable: true, false, false);
        for (var i = 1; i <= publishCount; i++)
        {
            var bytemessage = Encoding.UTF8.GetBytes($"is - {i}");

            var props = new BasicProperties();
            props.ContentType = "text/plain";
            props.DeliveryMode = DeliveryModes.Persistent;

            await channel.BasicPublishAsync("", "iskuyrugu", false, props, body: bytemessage);
        }
    }

    private static async Task SimpleConsumeAsync(IConnectionFactory connectionFactory, ushort prefetchCount = 1)
    {
        await using var connection = await connectionFactory.CreateConnectionAsync();
        await using var channel = await connection.CreateChannelAsync();

        await channel.QueueDeclareAsync("iskuyrugu", durable: true, false, false);
        await channel.BasicQosAsync(prefetchSize: 0, prefetchCount: prefetchCount, global: false);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (ch, ea) =>
        {
            var body = ea.Body.ToArray();
            var no = Encoding.UTF8.GetString(body);
            // copy or deserialise the payload
            // and process the message
            Thread.Sleep(_waitSampleWorkTime);
            // ...
            await channel.BasicAckAsync(ea.DeliveryTag, false);

            Console.WriteLine(no + " alındı");
        };
        // this consumer tag identifies the subscription
        // when it has to be cancelled
        var consumerTag = await channel.BasicConsumeAsync("iskuyrugu", false, consumer);
        Console.WriteLine("consumerTag: " + consumerTag);

        Console.Read(); // Block Consume Function terminate, Received Function Wait
    }

    private static async Task SemaphoreConsumeAsync(IConnectionFactory connectionFactory, ushort prefetchCount = 1)
    {
        await using var connection = await connectionFactory.CreateConnectionAsync();
        await using var channel = await connection.CreateChannelAsync();
        using var semaphore = new SemaphoreSlim(prefetchCount);

        await channel.QueueDeclareAsync("iskuyrugu", durable: true, false, false);
        await channel.BasicQosAsync(prefetchSize: 0, prefetchCount: prefetchCount, global: false);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (ch, ea) =>
        {
            await semaphore.WaitAsync();
            var body = ea.Body.ToArray();
            var no = Encoding.UTF8.GetString(body);

            // Do not using AWAIT , run asyncronosly
            Task.Run(async () =>
            {
                try
                {
                    Thread.Sleep(_waitSampleWorkTime);
                    // int a = 1;
                    // int b = 0;
                    // var c = a / b;

                    await channel.BasicAckAsync(ea.DeliveryTag, false);
                    Console.WriteLine(no + " alındı");
                }
                catch (TimeoutException timeProblem)
                {
                    // re-queue 
                    await channel.BasicNackAsync(ea.DeliveryTag, false, true);
                    //channel.BasicReject(ea.DeliveryTag, true);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(no + " HATA : " + ex.Message);

                    #region Copy To Error Queue

                    await using (var channelError = await connection.CreateChannelAsync())
                    {
                        await channelError.QueueDeclareAsync("iskuyruguError", durable: true, false, false);

                        var bytemessage = Encoding.UTF8.GetBytes(no);

                        var props = new BasicProperties();
                        props.ContentType = "text/plain";
                        props.DeliveryMode = DeliveryModes.Persistent;

                        await channelError.BasicPublishAsync("", "iskuyruguError", false, basicProperties: props, body: bytemessage);
                    }

                    #endregion

                    Console.WriteLine(no + " HATA kuyruguna aktarıldı");

                    // remove from old queue 
                    await channel.BasicNackAsync(ea.DeliveryTag, false, false);
                }
                finally
                {
                    semaphore.Release();
                }
            });
        };

        var consumerTag = await channel.BasicConsumeAsync("iskuyrugu", false, consumer);
        Console.WriteLine("consumerTag: " + consumerTag);

        Console.Read(); // Block Consume Function terminate, Received Function Wait
    }

    private static async Task ClearExchangeAllQueuesAsync(string exchangeName)
    {
        using var client = new HttpClient();
        var authToken = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{Username}:{Password}"));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authToken);
        
        // 1. Tüm kuyrukları al
        var response = await client.GetAsync($"{RabbitMqHost}/api/queues/{VHost}");
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        var queues = JArray.Parse(content);

        // 2. Her bir kuyruğu sil
        foreach (var queue in queues)
        {
            var queueName = queue["name"]?.ToString();
            if (!string.IsNullOrEmpty(queueName))
            {
                var deleteUrl = $"{RabbitMqHost}/api/queues/{VHost}/{Uri.EscapeDataString(queueName)}";

                var deleteResponse = await client.DeleteAsync(deleteUrl);

                if (deleteResponse.IsSuccessStatusCode)
                {
                    Console.WriteLine($"✓ Silindi: {queueName}");
                }
                else
                {
                    Console.WriteLine($"✗ Silinemedi: {queueName} - {deleteResponse.StatusCode}");
                }
            }
        }
    }
}