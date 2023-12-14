

using Kafka.Message;

namespace Kafka.Producer
{
    class Program
    {
        public static void Main(string[] args)
        {
            IMessageProducer messageProducer = new MessageProducer();

            while (true)
            {
                //produce email message
                var emailMessage = new EmailMessage
                {
                    Content = DateTime.Now.ToString("yyyyMMdd HH:mm:ss zzz"),
                    Subject = "Contoso Retail Daily News",
                    To = "all@contosoretail.com.tr"
                };
                messageProducer.Produce(emailMessage, "emailmessage-topic");

                var result = Console.ReadLine();
                if (!string.IsNullOrWhiteSpace(result) && result.ToLower().Equals("q")) break;
            }
        }
    }
}