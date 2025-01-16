using CFW.ODataCore.Models;
using CFW.ODataCore.Testings.Models;

namespace CFW.ODataCore.Testings.Features.Categories;

public class CategoriesPingPongPatchMethodNoResponseData
{
    public record RequestPing
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = string.Empty;
    }

    [EntityAction<Category>(nameof(CategoriesPingPongPatchMethodNoResponseData)
        , ActionMethod = ApiMethod.Patch
        , EntityName = "categories")]
    public class Handler : IOperationHandler<RequestPing>
    {
        public Task<Result> Handle(RequestPing request, CancellationToken cancellationToken)
        {
            var result = this.Success() as Result;
            return Task.FromResult(result);
        }
    }
}
