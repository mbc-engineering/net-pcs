using System;
using TwinCAT.Ads;

namespace Mbc.Ads.Utils.Connection
{
    /// <summary>
    /// Some extensions for <see cref="IAdsConnection"/>.
    /// </summary>
    public static class AdsConnectionExtensions
    {
        /// <summary>
        /// Similar to <see cref="IAdsAnyAccess.ReadAny(int, Type)"/> but expects a
        /// variable name instead of a handle.
        /// </summary>
        /// <param name="connection">A instance of <see cref="IAdsConnection"/>.</param>
        /// <param name="variable">The variable name to read.</param>
        /// <param name="type">The type of the variable to read.</param>
        public static object ReadyAny(this IAdsConnection connection, string variable, Type type)
        {
            var handle = connection.CreateVariableHandle(variable);
            try
            {
                return connection.ReadAny(handle, type);
            }
            finally
            {
                connection.DeleteVariableHandle(handle);
            }
        }

        /// <summary>
        /// Variant of <see cref="ReadyAny(IAdsConnection, string, Type)"/> with generic type
        /// parameter.
        /// </summary>
        public static T ReadyAny<T>(this IAdsConnection connection, string variable)
        {
            var handle = connection.CreateVariableHandle(variable);
            try
            {
                return (T)connection.ReadAny(handle, typeof(T));
            }
            finally
            {
                connection.DeleteVariableHandle(handle);
            }
        }
    }
}
