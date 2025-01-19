using CFW.ODataCore.Models;
using CFW.ODataCore.Testings.Models;
using System.Linq.Expressions;

namespace CFW.ODataCore.Testings.Features.Payments;

[Entity<Payment>("payments")]
public class PaymentEndpointConfiguration : EntityEndpoint<Payment>
{
    public override Expression<Func<Payment, object>>? Model
        => x => new { x.Id, x.Amount, x.PaymentDate, x.PaymentMethod };
}