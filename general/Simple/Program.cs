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
            Port = 35672,
            UserName = "guest",
            Password = "guest",
        };

        Publish(connectionFactory, 100);

        // SimpleConsume(connectionFactory);
        SemaphoreConsume(connectionFactory,10);
    }

    private static void Publish(IConnectionFactory connectionFactory, int publishCount = 1)
    {
        if (publishCount < 1) publishCount = 1;

        using (IConnection connection = connectionFactory.CreateConnection())
        using (IModel channel = connection.CreateModel())
        {
            channel.QueueDeclare("iskuyrugu", durable: true, false, false, null);
            for (int i = 1; i <= publishCount; i++)
            {
                byte[] bytemessage = Encoding.UTF8.GetBytes($"is - {i}");

                IBasicProperties properties = channel.CreateBasicProperties();
                properties.Persistent = true;

                channel.BasicPublish(exchange: "", routingKey: "iskuyrugu", basicProperties: properties, body: bytemessage);
            }
        }
    }

    private static void SimpleConsume(IConnectionFactory connectionFactory)
    {
        using (IConnection connection = connectionFactory.CreateConnection())
        using (IModel channel = connection.CreateModel())
        {
            channel.QueueDeclare("iskuyrugu", durable: true, false, false, null);
            channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);

            EventingBasicConsumer consumer = new EventingBasicConsumer(channel);
            channel.BasicConsume("iskuyrugu", false, consumer);

            consumer.Received += (sender, e) =>
            {
                Thread.Sleep(_waitSampleWorkTime);
                channel.BasicAck(e.DeliveryTag, false);
                Console.WriteLine(Encoding.UTF8.GetString(e.Body.ToArray()) + " alındı");
            };

            Console.Read(); // Block Consume Function terminate, Received Function Wait
        }
    }

    private static void SemaphoreConsume(IConnectionFactory connectionFactory, ushort threadCount = 1)
    {
        using (IConnection connection = connectionFactory.CreateConnection())
        using (IModel channel = connection.CreateModel())
        using (SemaphoreSlim semaphore = new SemaphoreSlim(threadCount))
        {
            channel.QueueDeclare("iskuyrugu", durable: true, false, false, null);
            channel.BasicQos(prefetchSize: 0, prefetchCount: threadCount, global: false);

            EventingBasicConsumer consumer = new EventingBasicConsumer(channel);
            channel.BasicConsume("iskuyrugu", false, consumer);

            consumer.Received += (sender, e) =>
            {
                semaphore.Wait();
                string no = Encoding.UTF8.GetString(e.Body.ToArray());
                Task.Run(() =>
                {
                    try
                    {
                        Thread.Sleep(_waitSampleWorkTime);
                        // int a = 1;
                        // int b = 0;
                        // var c = a / b;

                        channel.BasicAck(e.DeliveryTag, false);
                        Console.WriteLine(Encoding.UTF8.GetString(e.Body.ToArray()) + " alındı");
                    }
                    catch (TimeoutException timeProblem)
                    {
                        // re-queue 
                        channel.BasicNack(e.DeliveryTag, false, true);
                        //channel.BasicReject(e.DeliveryTag, true);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(no + " HATA : " + ex.Message);

                        #region Copy To Error Queue

                        using (IModel channelError = connection.CreateModel())
                        {
                            channelError.QueueDeclare("iskuyruguError", durable: true, false, false, null);

                            byte[] bytemessage = Encoding.UTF8.GetBytes(no);

                            IBasicProperties properties = channelError.CreateBasicProperties();
                            properties.Persistent = true;

                            channelError.BasicPublish(exchange: "", routingKey: "iskuyruguError", basicProperties: properties, body: bytemessage);
                        }

                        #endregion

                        Console.WriteLine(no + " HATA kuyruguna aktarıldı");

                        // remove from old queue 
                        channel.BasicNack(e.DeliveryTag, false, false);
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                });
            };

            Console.Read(); // Block Consume Function terminate, Received Function Wait
        }
    }
}