using CFW.Core.Entities;
using CFW.ODataCore.Models;

namespace CFW.ODataCore.Testings.Features.Retrictions;

[Entity("restrictions", Methods = [ApiMethod.Query, ApiMethod.GetByKey])]
[Entity("restrictions-postcreates", Methods = [ApiMethod.Post, ApiMethod.Patch, ApiMethod.Delete])]
public class Restriction : IEntity<Guid>
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;
}
