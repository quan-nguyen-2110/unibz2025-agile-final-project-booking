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
    public class GetAllBookingsQuery : IRequest<List<Booking>>
    {

        public class GetAllBookingsHandler : IRequestHandler<GetAllBookingsQuery, List<Booking>>
        {
            private readonly IBookingRepository _repo;

            public GetAllBookingsHandler(IBookingRepository repo)
            {
                _repo = repo;
            }

            public async Task<List<Booking>> Handle(GetAllBookingsQuery request, CancellationToken cancellationToken)
            {
                return await _repo.GetAllAsync();
            }
        }


    }

}
