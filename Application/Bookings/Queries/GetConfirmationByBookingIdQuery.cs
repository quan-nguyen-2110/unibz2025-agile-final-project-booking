using Application.Bookings.Queries.DTOs;
using Domain.Entities;
using Domain.Interfaces.IRepository;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Application.Bookings.Queries
{
    public class GetConfirmationByBookingIdQuery : IRequest<string>
    {
        public Guid Id { get; set; }
        public class GetConfirmationByBookingIdHandler : IRequestHandler<GetConfirmationByBookingIdQuery, string>
        {
            private readonly IBookingRepository _repo;
            private readonly IUserCacheRepository _userRepo;
            private readonly IApartmentCacheRepository _aptRepo;

            public GetConfirmationByBookingIdHandler(
                IBookingRepository repo,
                IUserCacheRepository userRepo,
                IApartmentCacheRepository aptRepo)
            {
                _repo = repo;
                _userRepo = userRepo;
                _aptRepo = aptRepo;
            }

            public async Task<string> Handle(GetConfirmationByBookingIdQuery request, CancellationToken cancellationToken)
            {
                var booking = await _repo.GetByIdAsync(request.Id);
                if (booking != null)
                {
                    // LLM generate confirmation message
                    try
                    {
                        var user = await _userRepo.GetByIdAsync(booking.UserId);
                        var apt = await _aptRepo.GetByIdAsync(booking.ApartmentId);

                        var url = "http://10.12.203.1:8080/v1/chat/completions";

                        using var httpClient = new HttpClient
                        {
                            Timeout = TimeSpan.FromSeconds(60)
                        };

                        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "aab1db4e-4c24-4bcb-9e91-de464cc35df2");

                        // Prompt for AI
                        string prompt = $@"
                            Generate a friendly apartment reservation confirmation message for a guest.
                            Include guest name, apartment name, check-in/out dates, and total price.
                            Reservation details:
                            - Guest: {user?.Name}
                            - Apartment: {apt?.Title}
                            - Check-in: {booking.CheckIn:MMMM dd, yyyy}
                            - Check-out: {booking.CheckOut:MMMM dd, yyyy}
                            - Total price: ${booking.TotalPrice:F2}

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

                        var jsonContentResponse = await response.Content.ReadAsStringAsync();
                        using var doc = JsonDocument.Parse(jsonContentResponse);
                        var contentResponse = doc.RootElement
                            .GetProperty("choices")[0]
                            .GetProperty("message")
                            .GetProperty("content")
                            .GetString();

                        return contentResponse ?? "";
                    }
                    catch (Exception ex)
                    {
                        // Log the exception (logging mechanism not shown here)
                        Console.WriteLine($"Failed to generate confirmation message: {ex.Message}");
                        return "LLM Network Error generating confirmation message";
                    }
                }

                return "Booking not found";
            }
        }
    }
}
