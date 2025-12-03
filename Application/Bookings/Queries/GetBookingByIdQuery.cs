using Application.Common.Interfaces;
using Domain.Entities;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Bookings.Queries
{
    public class GetBookingByIdQuery : IRequest<Booking?>
    {
        public Guid Id { get; set; }
        public class GetBookingByIdHandler : IRequestHandler<GetBookingByIdQuery, Booking?>
        {
            private readonly IBookingRepository _repo;

            public GetBookingByIdHandler(IBookingRepository repo)
            {
                _repo = repo;
            }

            public async Task<Booking?> Handle(GetBookingByIdQuery request, CancellationToken cancellationToken)
            {
                return await _repo.GetByIdAsync(request.Id);
            }
        }
    }
}
