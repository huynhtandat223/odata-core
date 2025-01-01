using CFW.Core.Entities;

namespace CFW.ODataCore.Testings.Models;

[Entity("customers")]
public class Customer : IEntity<int>
{
    public int Id { get; set; }

    public string? Name { get; set; }

    public string? Address { get; set; }
}

[Entity("vouchers")]
public class Voucher : IEntity<string>
{
    public string Id { get; set; } = string.Empty;

    public string? Code { get; set; }

    public decimal Discount { get; set; }
}