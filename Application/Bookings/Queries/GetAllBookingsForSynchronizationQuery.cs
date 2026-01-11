using Application.Bookings.Queries.DTOs;
using Domain.Interfaces.IRepository;
using Domain.Entities;
using MediatR;

namespace Application.Bookings.Queries
{
    public class GetAllBookingsForSynchronizationQuery : IRequest<List<object>>
    {

        public class GetAllBookingsForSynchronizationHandler : IRequestHandler<GetAllBookingsForSynchronizationQuery, List<object>>
        {
            private readonly IBookingRepository _repo;

            public GetAllBookingsForSynchronizationHandler(IBookingRepository repo)
            {
                _repo = repo;
            }

            public async Task<List<object>> Handle(GetAllBookingsForSynchronizationQuery request, CancellationToken cancellationToken)
            {
                List<object> result = new List<object>();
                var bookings = await _repo.GetAllAsync();
                foreach (var booking in bookings)
                {
                    Object obj = new
                    {
                        Id = booking.Id,

                        // Apartment
                        ApartmentId = booking.ApartmentId,

                        // User
                        UserId = booking.UserId,

                        // Booking
                        CheckIn = booking.CheckIn,
                        CheckOut = booking.CheckOut,
                        Guests = booking.Guests,
                        TotalPrice = booking.TotalPrice,
                        Status = booking.Status.ToString(),
                        CreatedAt = booking.CreatedAt,
                    };

                    result.Add(obj);
                }

                return result;
            }
        }
    }
}
