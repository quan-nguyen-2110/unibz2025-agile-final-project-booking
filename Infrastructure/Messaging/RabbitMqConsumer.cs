using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Threading.Channels;

namespace Infrastructure.Messaging
{
    public class RabbitMqConsumer : BackgroundService
    {
        private readonly IConnection _connection;
        private readonly IChannel _channel;

        private readonly string _exchangeName;
        private readonly string _aptRountingKey;
        private readonly string _bookingQueue;

        public RabbitMqConsumer(IConfiguration config)
        {
            var factory = new ConnectionFactory()
            {
                HostName = config["RabbitMQ:Host"] ?? "localhost",
                UserName = config["RabbitMQ:User"] ?? "guest",
                Password = config["RabbitMQ:Pass"] ?? "guest"
            };

            _exchangeName = config["RabbitMQ:ExchangeName"] ?? "rent-hub";
            _aptRountingKey = config["RabbitMQ:AptRountingKey"] ?? "apt-rounting-key";
            _bookingQueue = config["RabbitMQ:BookingQueueName"] ?? "booking-queue";

            _connection = factory.CreateConnectionAsync().GetAwaiter().GetResult();
            _channel = _connection.CreateChannelAsync().GetAwaiter().GetResult();

            // declare exchange
            _channel.ExchangeDeclareAsync(exchange: _exchangeName, type: ExchangeType.Fanout).Wait();

            // declare queue
            _channel.QueueDeclareAsync(
                queue: _bookingQueue,
                durable: false,
                exclusive: false,
                autoDelete: false,
                arguments: null
            ).Wait();

            // binding queue to exchange
            _channel.QueueBindAsync(queue: _bookingQueue, exchange: _exchangeName, routingKey: _aptRountingKey).Wait();
            //_channel.QueueBindAsync(queue: _bookingQueue, exchange: _exchangeName, routingKey: _bkRountingKey).Wait();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var consumer = new AsyncEventingBasicConsumer(_channel);

            consumer.ReceivedAsync += async (model, ea) =>
            {
                var message = Encoding.UTF8.GetString(ea.Body.ToArray());
                Console.WriteLine($"[x] Received: {message}");

                await Task.Yield();
            };

            await _channel.BasicConsumeAsync(_bookingQueue, autoAck: true, consumer);
        }
    }
}
