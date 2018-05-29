using System;

namespace Mbc.Pcs.Net
{
    /// <summary>
    /// The exception is thrown if the execution of a <see cref="PlcCommand"/>
    /// results in an error.
    /// </summary>
    public class PlcCommandErrorException : PlcCommandException
    {
        public PlcCommandErrorException(string commandVariable, ushort resultCode, string message)
            : base(commandVariable, message)
        {
            ResultCode = resultCode;
        }

        public ushort ResultCode { get; }

        public override string Message
        {
            get
            {
                var message = base.Message;
                if (!string.IsNullOrEmpty(CommandVariable))
                {
                    message += Environment.NewLine + $"Result Code: {ResultCode}";
                }
                return message;
            }
        }
    }
}
