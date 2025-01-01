namespace CFW.ODataCore.Models;

internal record EntityKey
{
    public required Type EntityType { get; set; }

    public required Type KeyType { get; set; }

    public required string Name { get; set; }
}


