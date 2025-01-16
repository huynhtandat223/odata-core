using CFW.ODataCore.Testings.Models;
using System.ComponentModel.DataAnnotations;

namespace CFW.ODataCore.Testings.Features.Categories;

public class CategoriesPingPongWithKey
{
    public record RequestPing
    {
        [Key]
        public Guid RequestId { get; set; }

        public string Name { get; set; } = string.Empty;
    }

    public record ResponsePong : RequestPing
    {

    }

    [EntityAction<Category>(nameof(CategoriesPingPongWithKey), EntityName = "categories")]
    public class Handler : IOperationHandler<RequestPing, ResponsePong>
    {
        public Task<Result<ResponsePong>> Handle(RequestPing request, CancellationToken cancellationToken)
        {
            var result = new ResponsePong
            {
                RequestId = request.RequestId,
                Name = request.Name
            }.Success();

            return Task.FromResult(result);
        }
    }
}