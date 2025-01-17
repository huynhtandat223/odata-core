using System.Reflection;
using System.Reflection.Emit;

namespace CFW.Core.Builders;
public static class ViewModelTypeBuilder
{

    /// <summary>
    /// Creates a new ViewModel type that mirrors the structure of the original type.
    /// </summary>
    public static Type BuildViewModelType(ModuleBuilder moduleBuilder, Type originalType, string typeName)
    {
        var typeBuilder = moduleBuilder.DefineType(typeName, TypeAttributes.Public | TypeAttributes.Class);

        foreach (var property in originalType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            // Define a property in the ViewModel that matches the original type
            var propertyType = property.PropertyType;

            DefineProperty(typeBuilder, property.Name, propertyType);
        }

        return typeBuilder.CreateType();
    }

    private static void DefineProperty(TypeBuilder typeBuilder, string propertyName, Type propertyType)
    {
        var fieldBuilder = typeBuilder.DefineField($"_{propertyName.ToLower()}", propertyType, FieldAttributes.Private);

        var propertyBuilder = typeBuilder.DefineProperty(propertyName, PropertyAttributes.HasDefault, propertyType, null);

        // Getter
        var getterMethod = typeBuilder.DefineMethod(
            $"get_{propertyName}",
            MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig,
            propertyType,
            Type.EmptyTypes);

        var getterIL = getterMethod.GetILGenerator();
        getterIL.Emit(OpCodes.Ldarg_0);
        getterIL.Emit(OpCodes.Ldfld, fieldBuilder);
        getterIL.Emit(OpCodes.Ret);

        propertyBuilder.SetGetMethod(getterMethod);

        // Setter
        var setterMethod = typeBuilder.DefineMethod(
            $"set_{propertyName}",
            MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig,
            null,
            new[] { propertyType });

        var setterIL = setterMethod.GetILGenerator();
        setterIL.Emit(OpCodes.Ldarg_0);
        setterIL.Emit(OpCodes.Ldarg_1);
        setterIL.Emit(OpCodes.Stfld, fieldBuilder);
        setterIL.Emit(OpCodes.Ret);

        propertyBuilder.SetSetMethod(setterMethod);
    }
}

