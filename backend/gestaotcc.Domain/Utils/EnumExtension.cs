using System.ComponentModel;

namespace gestaotcc.Domain.Utils;

public static class EnumExtension
{
    public static string GetDescription(this Enum? value)
    {
        if (value is null) return null;
        
        var fieldInfo = value.GetType().GetField(value.ToString());
        if (fieldInfo == null) return null;

        var attributes = (DescriptionAttribute[])fieldInfo.GetCustomAttributes(
            typeof(DescriptionAttribute), false);

        return attributes.Length > 0 ? attributes[0].Description : value.ToString();
    }
}
