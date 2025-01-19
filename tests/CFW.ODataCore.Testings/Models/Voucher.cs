using CFW.Core.Entities;

namespace CFW.ODataCore.Testings.Models;

public class Voucher : IEntity<string>
{
    public string Id { get; set; } = string.Empty;

    public string? Code { get; set; }

    public decimal Discount { get; set; }
}