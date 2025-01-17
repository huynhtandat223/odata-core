using CFW.ODataCore.Testings.Models;

namespace CFW.ODataCore.Testings.Features.Categories;

public class CategoriesPingPongWithNonKey
{
    public record RequestPing
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = string.Empty;
    }

    public record ResponsePong : RequestPing
    {

    }

    [EntityAction<Category>(nameof(CategoriesPingPongWithNonKey), EntityName = "categories")]
    public class Handler : IOperationHandler<RequestPing, ResponsePong>
    {
        public Task<Result<ResponsePong>> Handle(RequestPing request, CancellationToken cancellationToken)
        {
            var result = new ResponsePong
            {
                Id = request.Id,
                Name = request.Name
            }.Success();

            return Task.FromResult(result);
        }
    }
}
