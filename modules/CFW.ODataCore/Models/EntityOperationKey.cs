namespace CFW.ODataCore.Models;

internal record EntityOperationKey
{
    public required Type EntityType { get; set; }

    public required string OperationName { get; set; }

    public required OperationType OperationType { get; set; }
}


