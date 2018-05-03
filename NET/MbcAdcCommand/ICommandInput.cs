using System.Collections.Generic;

namespace MbcAdcCommand
{
    /// <summary>
    /// Provides input data for a <see cref="PlcCommand"/>:
    /// </summary>
    public interface ICommandInput
    {
        /// <summary>
        /// Returns the command input data.
        /// </summary>
        IDictionary<string, object> GetInput();
    }
}
