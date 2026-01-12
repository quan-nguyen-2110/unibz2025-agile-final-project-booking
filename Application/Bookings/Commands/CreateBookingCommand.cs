using Application.Common.Interfaces.IMessaging;
using Domain.Entities;
using Domain.Interfaces.IRepository;
using MediatR;
using Microsoft.Extensions.Configuration;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Application.Bookings.Commands
{
    public class CreateBookingCommand : IRequest<Guid>
    {
        public Guid ApartmentId { get; set; }
        public Guid UserId { get; set; }
        public DateTime CheckIn { get; set; }
        public DateTime CheckOut { get; set; }
        public decimal TotalPrice { get; set; }
        public int Guests { get; set; }
        public class CreateBookingHandler : IRequestHandler<CreateBookingCommand, Guid>
        {
            private readonly IBookingRepository _repo;
            private readonly IMessagePublisher _publisher;
            private readonly IConfiguration _config;

            public CreateBookingHandler(
                IBookingRepository repo,
                IMessagePublisher publisher,
                IConfiguration config)
            {
                _repo = repo;
                _publisher = publisher;
                _config = config;
            }

            public async Task<Guid> Handle(CreateBookingCommand request, CancellationToken cancellationToken)
            {
                // Prevent double-booking
                var bookings = await _repo.GetByApartmentIdAsync(request.ApartmentId);
                if (bookings.Any(x => x.Status != Domain.Enums.BookingStatus.Cancelled &&
                    request.CheckIn < x.CheckOut && x.CheckOut < request.CheckOut))
                {
                    throw new Exception("Double-booking");
                }

                // Add new booking
                var booking = new Booking
                {
                    Id = Guid.NewGuid(),
                    ApartmentId = request.ApartmentId,
                    UserId = request.UserId,
                    CheckIn = request.CheckIn,
                    CheckOut = request.CheckOut,
                    TotalPrice = request.TotalPrice,
                    Guests = request.Guests,

                    CreatedAt = DateTime.Now
                };

                await _repo.AddAsync(booking);

                try
                {
                    await _publisher.PublishAsync(
                        JsonSerializer.Serialize(new
                        {
                            Id = booking.Id,
                            ApartmentId = request.ApartmentId,
                            UserId = request.UserId,
                            CheckIn = request.CheckIn,
                            CheckOut = request.CheckOut,
                            TotalPrice = request.TotalPrice,
                            Guests = request.Guests,
                            Status = "Pending",

                            CreatedAt = DateTime.UtcNow
                        }),
                        _config["RabbitMQ:RK:CreateBooking"] ?? "rk-create-bk");
                }
                catch (Exception ex)
                {
                    // Log the exception (logging mechanism not shown here)
                    Console.WriteLine($"Failed to publish message: {ex.Message}");
                }
                return booking.Id;
            }
        }
    }
}
