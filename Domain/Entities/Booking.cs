using Domain.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public class Booking
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid ApartmentId { get; set; }

        [Required]
        public Guid UserId { get; set; }

        [Required]
        public DateTime CheckIn { get; set; }

        [Required]
        public DateTime CheckOut { get; set; }

        public int Guests { get; set; } = 1;

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalPrice { get; set; }

        [Required]
        public BookingStatus Status { get; private set; } = BookingStatus.Pending;

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        public string? CancelReason { get; set; }

        public virtual ApartmentCache? Apartment { get; set; }
        public virtual UserCache? User { get; set; }

        public int CalculateNights()
        {
            return (this.CheckOut - this.CheckIn).Days;
        }

        public void Confirm()
        {
            if (this.Status == BookingStatus.Cancelled)
                throw new InvalidOperationException("Cannot confirm a cancelled booking.");
            this.Status = BookingStatus.Confirmed;
            this.UpdatedAt = DateTime.UtcNow;
        }

        public void Cancel(string? cancelReason)
        {
            if (this.Status == BookingStatus.Cancelled)
                throw new InvalidOperationException("This booking has been already cancelled");
            this.Status = BookingStatus.Confirmed;
            this.UpdatedAt = DateTime.UtcNow;
            this.CancelReason = cancelReason;
        }

        public void Reshedule()
        {
            if (this.Status == BookingStatus.Cancelled)
                throw new InvalidOperationException("Cannot reschedule a cancelled booking.");
            this.Status = BookingStatus.Pending;
            this.UpdatedAt = DateTime.UtcNow;
        }
    }
}
