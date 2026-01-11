using Application.Common.Interfaces.IMessaging;
using Domain.Interfaces.IRepository;
using MediatR;
using Microsoft.Extensions.Configuration;
using System.Text.Json;

namespace Application.Bookings.Commands
{
    public class RescheduleBookingCommand : IRequest<bool>
    {
        public Guid Id { get; set; }
        public DateTime CheckIn { get; set; }
        public DateTime CheckOut { get; set; }
        public decimal TotalPrice { get; set; }

        public class RescheduleBookingHandler : IRequestHandler<RescheduleBookingCommand, bool>
        {
            private readonly IBookingRepository _repo;
            private readonly IMessagePublisher _publisher;
            private readonly IConfiguration _config;

            public RescheduleBookingHandler(IBookingRepository repo, IMessagePublisher publisher, IConfiguration config)
            {
                _repo = repo;
                _publisher = publisher;
                _config = config;
            }

            public async Task<bool> Handle(RescheduleBookingCommand request, CancellationToken cancellationToken)
            {
                // Prevent double-booking
                var booking = await _repo.GetByIdAsync(request.Id);
                if (booking == null)
                    throw new KeyNotFoundException("Booking not found");

                var bookings = await _repo.GetByApartmentIdAsync(booking.ApartmentId);
                if (bookings.Any(x => x.Id != request.Id && x.Status != Domain.Enums.BookingStatus.Cancelled &&
                    request.CheckIn < x.CheckOut && x.CheckOut < request.CheckOut))
                {
                    throw new Exception("Double-booking");
                }

                booking.CheckIn = request.CheckIn;
                booking.CheckOut = request.CheckOut;
                booking.TotalPrice = request.TotalPrice;
                booking.Reshedule();

                await _repo.UpdateAsync(booking);

                try
                {
                    await _publisher.PublishAsync(
                        JsonSerializer.Serialize(new
                        {
                            Id = request.Id,
                            Status = "Pending",
                            CheckIn = request.CheckIn,
                            CheckOut = request.CheckOut,
                            TotalPrice = request.TotalPrice,

                            UpdatedAt = DateTime.UtcNow
                        }),
                        _config["RabbitMQ:RK:UpdateBooking"] ?? "rk-update-bk");
                }
                catch (Exception ex)
                {
                    // Log the exception (logging mechanism not shown here)
                    Console.WriteLine($"Failed to publish message: {ex.Message}");
                }

                return true;
            }
        }
    }
}
