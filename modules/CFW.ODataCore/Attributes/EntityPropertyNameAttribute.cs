namespace CFW.ODataCore.Attributes;

[AttributeUsage(AttributeTargets.Property)]
public class EntityPropertyNameAttribute : Attribute
{
    public string DbModelPropertyName { get; }

    public EntityPropertyNameAttribute(string dbModelPropertyName)
    {
        DbModelPropertyName = dbModelPropertyName;
    }
}
