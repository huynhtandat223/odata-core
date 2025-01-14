using CFW.Core.Entities;

namespace CFW.ODataCore.Testings.Models;

[EntityV2("customers")]
public class Customer : IEntity<int>
{
    public int Id { get; set; }

    public string? Name { get; set; }

    public string? Address { get; set; }

    public IEnumerable<Order>? Orders { get; set; }

    public Address? ShippingAddress { get; set; }
}

public class Address : IEntity<int>
{
    public int Id { get; set; }

    public string? Street { get; set; }

    public string? City { get; set; }

    public string? State { get; set; }

    public string? ZipCode { get; set; }
}
