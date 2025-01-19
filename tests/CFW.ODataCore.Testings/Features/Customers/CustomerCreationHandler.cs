using CFW.ODataCore.Models.Requests;
using CFW.ODataCore.Testings.Models;

namespace CFW.ODataCore.Testings.Features.Customers;

[EntityHandler<Customer>]
public class CustomerCreationHandler : IEntityCreationHandler<Customer>
{
    public Task<Result> Handle(CreationCommand<Customer> command, CancellationToken cancellationToken)
    {
        var result = command.Delta.Instance.Success() as Result;
        return Task.FromResult(result);
    }
}
