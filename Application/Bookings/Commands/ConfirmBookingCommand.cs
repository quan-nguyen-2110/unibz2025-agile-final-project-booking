using Domain.Enums;
using Domain.Interfaces.IRepository;
using MediatR;

namespace Application.Bookings.Commands
{
    public class ConfirmBookingCommand : IRequest<bool>
    {
        public Guid Id { get; set; }
        public class ConfirmBookingHandler : IRequestHandler<ConfirmBookingCommand, bool>
        {
            private readonly IBookingRepository _repo;

            public ConfirmBookingHandler(IBookingRepository repo)
            {
                _repo = repo;
            }

            public async Task<bool> Handle(ConfirmBookingCommand request, CancellationToken cancellationToken)
            {
                var booking = await _repo.GetByIdAsync(request.Id);
                if (booking == null)
                    throw new KeyNotFoundException("Booking not found");

                booking.Confirm();

                await _repo.UpdateAsync(booking);

                return true;
            }
        }
    }
}
