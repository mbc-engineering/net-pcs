using System;

namespace Mbc.Pcs.Net.Command
{
    public static class TypeExtension
    {
        public static object GetDefaultValue(this Type type)
        {
            if (type == typeof(string))
            {
                return string.Empty;
            }
            else if (type.IsValueType)
            {
                return Activator.CreateInstance(type);
            }
            else
            {
                return null;
            }
        }
    }
}
