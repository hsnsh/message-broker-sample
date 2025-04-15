using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Simple;

internal static class Program
{
    private static int _waitSampleWorkTime = 1000;

    public static void Main(string[] args)
    {
        var connectionFactory = new ConnectionFactory
        {
            HostName = "localhost",
            Port = 5672,
            UserName = "guest",
            Password = "guest",
            VirtualHost = "/"
        };

        try
        {
            PublishAsync(connectionFactory, 100).GetAwaiter().GetResult();

            // SimpleConsume(connectionFactory);
            SemaphoreConsumeAsync(connectionFactory, 10).GetAwaiter().GetResult();
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

        await channel.QueueDeclareAsync("iskuyrugu", durable: true, false, false, null);
        for (var i = 1; i <= publishCount; i++)
        {
            var bytemessage = Encoding.UTF8.GetBytes($"is - {i}");

            var props = new BasicProperties();
            props.ContentType = "text/plain";
            props.DeliveryMode = DeliveryModes.Persistent;

            await channel.BasicPublishAsync("", "iskuyrugu", false, props, body: bytemessage);
        }
    }

    private static async Task SimpleConsumeAsync(IConnectionFactory connectionFactory)
    {
        using (var connection = await connectionFactory.CreateConnectionAsync())
        using (var channel = await connection.CreateChannelAsync())
        {
            await channel.QueueDeclareAsync("iskuyrugu", durable: true, false, false, null);
            channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false);

            var consumer = new AsyncEventingBasicConsumer(channel);
            consumer.ReceivedAsync += async (ch, ea) =>
            {
                var body = ea.Body.ToArray();
                // copy or deserialise the payload
                // and process the message
                Thread.Sleep(_waitSampleWorkTime);
                // ...
                await channel.BasicAckAsync(ea.DeliveryTag, false);

                Console.WriteLine(Encoding.UTF8.GetString(body) + " alındı");
            };
            // this consumer tag identifies the subscription
            // when it has to be cancelled
            var consumerTag = await channel.BasicConsumeAsync("iskuyrugu", false, consumer);
            Console.WriteLine("consumerTag: " + consumerTag);

            Console.Read(); // Block Consume Function terminate, Received Function Wait
        }
    }

    private static async Task SemaphoreConsumeAsync(IConnectionFactory connectionFactory, ushort threadCount = 1)
    {
        await using var connection = await connectionFactory.CreateConnectionAsync();
        await using var channel = await connection.CreateChannelAsync();
        using var semaphore = new SemaphoreSlim(threadCount);

        await channel.QueueDeclareAsync("iskuyrugu", durable: true, false, false, null);
        await channel.BasicQosAsync(prefetchSize: 0, prefetchCount: threadCount, global: false);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (ch, ea) =>
        {
            await semaphore.WaitAsync();
            var body = ea.Body.ToArray();
            var no = Encoding.UTF8.GetString(body);
            Task.Run(() =>
            {
                try
                {
                    Thread.Sleep(_waitSampleWorkTime);
                    // int a = 1;
                    // int b = 0;
                    // var c = a / b;

                    channel.BasicAckAsync(ea.DeliveryTag, false).GetAwaiter().GetResult();
                    Console.WriteLine(Encoding.UTF8.GetString(ea.Body.ToArray()) + " alındı");
                }
                catch (TimeoutException timeProblem)
                {
                    // re-queue 
                    channel.BasicNackAsync(ea.DeliveryTag, false, true).GetAwaiter().GetResult();
                    //channel.BasicReject(ea.DeliveryTag, true);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(no + " HATA : " + ex.Message);

                    #region Copy To Error Queue

                    using (var channelError = connection.CreateChannelAsync().GetAwaiter().GetResult())
                    {
                        channelError.QueueDeclareAsync("iskuyruguError", durable: true, false, false, null).GetAwaiter().GetResult();

                        var bytemessage = Encoding.UTF8.GetBytes(no);

                        var props = new BasicProperties();
                        props.ContentType = "text/plain";
                        props.DeliveryMode = DeliveryModes.Persistent;

                        channelError.BasicPublishAsync("", "iskuyruguError", false, basicProperties: props, body: bytemessage).GetAwaiter().GetResult();
                    }

                    #endregion

                    Console.WriteLine(no + " HATA kuyruguna aktarıldı");

                    // remove from old queue 
                    channel.BasicNackAsync(ea.DeliveryTag, false, false).GetAwaiter().GetResult();
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
}