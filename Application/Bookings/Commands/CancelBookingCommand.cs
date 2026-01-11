using Application.Common.Interfaces.IMessaging;
using Domain.Interfaces.IRepository;
using MediatR;
using Microsoft.Extensions.Configuration;
using System.Text.Json;

namespace Application.Bookings.Commands
{
    public class CancelBookingCommand : IRequest<bool>
    {
        public Guid Id { get; set; }
        public string? CancelReason { get; set; }
        public class CancelBookingHandler : IRequestHandler<CancelBookingCommand, bool>
        {
            private readonly IBookingRepository _repo;
            private readonly IMessagePublisher _publisher;
            private readonly IConfiguration _config;

            public CancelBookingHandler(IBookingRepository repo, IMessagePublisher publisher, IConfiguration config)
            {
                _repo = repo;
                _publisher = publisher;
                _config = config;
            }

            public async Task<bool> Handle(CancelBookingCommand request, CancellationToken cancellationToken)
            {
                var booking = await _repo.GetByIdAsync(request.Id);
                if (booking == null)
                    throw new KeyNotFoundException("Booking not found");

                booking.Cancel(request.CancelReason?.Trim());

                await _repo.UpdateAsync(booking);

                try
                {
                    await _publisher.PublishAsync(
                        JsonSerializer.Serialize(new
                        {
                            Id = request.Id,
                            Status = "Cancelled",

                            ApartmentId = booking.ApartmentId,
                            UserId = booking.UserId,
                            Guest = booking.Guests,
                            CancelReason = booking.CancelReason,
                            CreatedAt = booking.CreatedAt,

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
