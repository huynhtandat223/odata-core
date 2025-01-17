namespace CFW.ODataCore.Attributes;

[AttributeUsage(AttributeTargets.Property)]
public class EntityPropertyNameAttribute : Attribute
{
    internal string DbModelPropertyName { get; }

    public EntityPropertyNameAttribute(string dbModelPropertyName)
    {
        DbModelPropertyName = dbModelPropertyName;
    }
}
