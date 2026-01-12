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
            private readonly IUserCacheRepository _userRepo;
            private readonly IApartmentCacheRepository _aptRepo;
            private readonly IMessagePublisher _publisher;
            private readonly IConfiguration _config;

            public CreateBookingHandler(
                IBookingRepository repo,
                IUserCacheRepository userRepo,
                IApartmentCacheRepository aptRepo,
                IMessagePublisher publisher,
                IConfiguration config)
            {
                _repo = repo;
                _userRepo = userRepo;
                _aptRepo = aptRepo;
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

                // LLM generate confirmation message
                try
                {
                    var url = "http://10.12.203.1:8080/v1/chat/completions";

                    using var httpClient = new HttpClient
                    {
                        Timeout = TimeSpan.FromSeconds(60)
                    };

                    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "aab1db4e-4c24-4bcb-9e91-de464cc35df2");
                    var user = await _userRepo.GetByIdAsync(request.UserId);
                    var apt = await _aptRepo.GetByIdAsync(request.ApartmentId);
                    // Prompt for AI
                    string prompt = $@"
                        Generate a friendly apartment reservation confirmation message for a guest.
                        Include guest name, apartment name, check-in/out dates, and total price.
                        Reservation details:
                        - Guest: {user?.Name}
                        - Apartment: {apt?.Title}
                        - Check-in: {request.CheckIn:MMMM dd, yyyy}
                        - Check-out: {request.CheckOut:MMMM dd, yyyy}
                        - Total price: ${request.TotalPrice:F2}

                        The message should be polite and professional.";

                    var requestBody = new
                    {
                        model = "llama3:latest",
                        // temperature = 0.2,
                        messages = new[]
                        {
                            new
                            {
                                role = "user",
                                content = prompt
                            }
                        }
                    };

                    var json = JsonSerializer.Serialize(requestBody);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");

                    var response = await httpClient.PostAsync(url, content);

                    Console.WriteLine($"Status: {(int)response.StatusCode}");
                    Console.WriteLine("Response:");
                }
                catch (Exception ex)
                {
                    // Log the exception (logging mechanism not shown here)
                    Console.WriteLine($"Failed to generate confirmation message: {ex.Message}");
                }

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
