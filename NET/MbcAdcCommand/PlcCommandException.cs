using System;

namespace MbcAdcCommand
{
    /// <summary>
    /// The exception is thrown if the execution of a <see cref="PlcCommand"/>
    /// fails for some reason.
    /// </summary>
    public class PlcCommandException : Exception
    {
        
        public PlcCommandException(string commandVariable)
        {
            CommandVariable = commandVariable;
        }

        public PlcCommandException(string commandVariable, string message)
            : base(message)
        {
            CommandVariable = commandVariable;
        }

        public PlcCommandException(string commandVariable, string message, Exception innerException)
            : base(message, innerException)
        {
            CommandVariable = commandVariable;
        }

        public string CommandVariable { get; }

        public override string Message
        {
            get
            {
                var message = base.Message;
                if (!string.IsNullOrEmpty(CommandVariable))
                {
                    message += Environment.NewLine + $"Command: {CommandVariable}";
                }
                return message;
            }
        }

    }
}
