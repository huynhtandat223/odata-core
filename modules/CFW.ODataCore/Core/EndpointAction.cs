namespace CFW.ODataCore.Core;

public enum EndpointAction
{
    Query = 1,
    GetByKey,
    PostCreate,
    PatchUpdate,
    Delete,
    BoundAction,
    BoundFunction,
    UnboundAction,
    UnboundFunction,
    CRUD
}

