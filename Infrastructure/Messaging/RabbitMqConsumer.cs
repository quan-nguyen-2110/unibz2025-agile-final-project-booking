using Application.Interfaces.IRepository;
using Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;

namespace Infrastructure.Messaging
{
    public class RabbitMqConsumer : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IConnection _connection;
        private readonly IChannel _channel;

        private readonly string _exchangeName;
        private readonly string _bookingQueue;

        private readonly string _RKCreatedApt;
        private readonly string _RKUpdateddApt;
        public RabbitMqConsumer(IConfiguration config, IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;

            var factory = new ConnectionFactory()
            {
                HostName = config["RabbitMQ:Host"] ?? "localhost",
                UserName = config["RabbitMQ:User"] ?? "guest",
                Password = config["RabbitMQ:Pass"] ?? "guest"
            };

            _exchangeName = config["RabbitMQ:ExchangeName"] ?? "rent-hub";
            _bookingQueue = config["RabbitMQ:BookingQueueName"] ?? "booking-queue";

            _RKCreatedApt = config["RabbitMQ:RK.CreateApartment"] ?? "rk-create-apt";
            _RKUpdateddApt = config["RabbitMQ:RK.UpdateApartment"] ?? "rk-update-apt";

            _connection = factory.CreateConnectionAsync().GetAwaiter().GetResult();
            _channel = _connection.CreateChannelAsync().GetAwaiter().GetResult();

            // declare exchange
            _channel.ExchangeDeclareAsync(exchange: _exchangeName, type: ExchangeType.Direct).Wait();

            // declare queue
            _channel.QueueDeclareAsync(
                queue: _bookingQueue,
                durable: false,
                exclusive: false,
                autoDelete: false,
                arguments: null
            ).Wait();

            // binding queue to exchange
            _channel.QueueBindAsync(queue: _bookingQueue, exchange: _exchangeName, routingKey: _RKCreatedApt).Wait();
            _channel.QueueBindAsync(queue: _bookingQueue, exchange: _exchangeName, routingKey: _RKUpdateddApt).Wait();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var consumer = new AsyncEventingBasicConsumer(_channel);

            consumer.ReceivedAsync += async (model, ea) =>
            {
                try
                {
                    // Get routing key
                    var routingKey = ea.RoutingKey;

                    var aptJson = Encoding.UTF8.GetString(ea.Body.ToArray());
                    var apt = JsonSerializer.Deserialize<ApartmentCache>(aptJson);

                    if (apt != null)
                    {
                        using var scope = _scopeFactory.CreateScope();

                        var _aptRepository = scope.ServiceProvider.GetRequiredService<IApartmentCacheRepository>();
                        var isExisting = await _aptRepository.IsExistingAsync(apt.Id);
                        if (!isExisting)
                        {
                            // handle create event
                            await _aptRepository.AddAsync(apt);
                        }
                        else
                        {
                            // handle update event
                            await _aptRepository.UpdateAsync(apt);
                        }

                        //if (routingKey == _RKCreatedApt)
                        //{
                        //    // handle create event
                        //    _aptRepository.AddAsync(apt).Wait();
                        //}
                        //else if (routingKey == _RKUpdateddApt)
                        //{
                        //    // handle update event
                        //    var result = await _aptRepository.UpdateAsync(apt);
                        //}
                    }

                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }


                await Task.Yield();
            };

            await _channel.BasicConsumeAsync(_bookingQueue, autoAck: true, consumer);
        }
    }
}
