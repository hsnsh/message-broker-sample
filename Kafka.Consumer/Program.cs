﻿using System;
using Kafka.Consumer.Consumers;
using Kafka.Message.Tools;

namespace Kafka.Consumer
{
    class Program
    {
        static void Main(string[] args)
        {
            ConsoleWriter.Info("Consumer Started !");

            var emailMessageConsumer = new EmailMessageConsumer();
            emailMessageConsumer.StartConsuming();

            Console.ReadLine();
        }
    }
}