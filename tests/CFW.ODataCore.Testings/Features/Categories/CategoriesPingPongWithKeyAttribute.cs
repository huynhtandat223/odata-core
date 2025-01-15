using CFW.ODataCore.Testings.Models;
using System.ComponentModel.DataAnnotations;

namespace CFW.ODataCore.Testings.Features.Categories;

public class CategoriesPingPongWithKeyAttribute
{
    public record RequestPing
    {
        [Key]
        public Guid Id { get; set; }

        public string Name { get; set; } = string.Empty;
    }

    public record ResponsePong
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    [EntityAction<Category>(nameof(CategoriesPingPongWithKeyAttribute))]
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