using Application.Common.Interfaces;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Bookings.Commands
{
    public class UpdateBookingCommand : IRequest<bool>
    {
        public Guid Id { get; set; }
        public DateTime CheckIn { get; set; }
        public DateTime CheckOut { get; set; }
        public decimal TotalPrice { get; set; }

        public class UpdateBookingHandler : IRequestHandler<UpdateBookingCommand, bool>
        {
            private readonly IBookingRepository _repo;

            public UpdateBookingHandler(IBookingRepository repo)
            {
                _repo = repo;
            }

            public async Task<bool> Handle(UpdateBookingCommand request, CancellationToken cancellationToken)
            {
                var booking = await _repo.GetByIdAsync(request.Id);
                if (booking == null)
                    throw new KeyNotFoundException("Booking not found");

                booking.CheckIn = request.CheckIn;
                booking.CheckOut = request.CheckOut;
                booking.TotalPrice = request.TotalPrice;

                await _repo.UpdateAsync(booking);
                return true;
            }
        }
    }
}
