using System;

namespace Mbc.Pcs.Net.Command
{
    /// <summary>
    /// The exception is thrown if the execution of a <see cref="PlcCommand"/>
    /// timed out.
    /// </summary>
    public class PlcCommandLockException : PlcCommandException
    {
        public PlcCommandLockException(string commandVariable, ExecutionBehavior behavior, string message)
            : base(commandVariable, message)
        {
            Behavior = behavior;
        }

        public PlcCommandLockException(string commandVariable, ExecutionBehavior behavior, string message, Exception innerException)
            : base(commandVariable, message, innerException)
        {
            Behavior = behavior;
        }

        public ExecutionBehavior Behavior { get; }
    }
}
