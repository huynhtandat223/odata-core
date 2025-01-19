using System.Linq.Expressions;
using System.Reflection;

namespace CFW.ODataCore.Models;

public abstract class EntityEndpoint<TEntity>
{
    public virtual Expression<Func<TEntity, object>>? Model { get; }

    public PropertyInfo[] GetAllowedProperties()
    {
        if (Model is null)
        {
            return typeof(TEntity).GetProperties();
        }

        //Get properties from new expression x => new { x.Property1, x.Property2 }
        if (Model.Body is NewExpression newExpression)
        {
            return newExpression.Members
                .Select(x => typeof(TEntity).GetProperty(x.Name))
                .ToArray();
        }

        throw new NotImplementedException();
    }
}
