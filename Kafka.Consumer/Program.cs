using Base.Core;
using Kafka.Consumer.Consumers;

namespace Kafka.Consumer;

class Program
{
    static void Main(string[] args)
    {
        ConsoleWriter.Info("Consumer Started !");

        var emailMessageConsumer = new EmailKafkaConsumer(args?[0]);
        emailMessageConsumer.StartConsuming();

        Console.ReadLine();
    }
}