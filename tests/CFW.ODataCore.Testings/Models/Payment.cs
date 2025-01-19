using CFW.Core.Entities;

namespace CFW.ODataCore.Testings.Models;

public class Payment : IEntity<Guid>
{
    public Guid Id { get; set; }

    public string? PaymentMethod { get; set; }

    public decimal Amount { get; set; }

    public DateTime PaymentDate { get; set; }

    public string? Note { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public DateTime? DeletedAt { get; set; }
}