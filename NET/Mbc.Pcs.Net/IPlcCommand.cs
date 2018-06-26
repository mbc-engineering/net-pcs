using System;
using System.Threading;
using System.Threading.Tasks;

namespace Mbc.Pcs.Net
{
    public interface IPlcCommand
    {
        /// <summary>
        /// Maximale time to wait for command completion.
        /// </summary>
        TimeSpan Timeout { get; set; }

        /// <summary>
        /// The PLC Variable 
        /// </summary>
        string AdsCommandFbPath { get; }

        /// <summary>
        /// Defines the beavior how to react to parallel exection of this command. 
        /// Default is locking the second caller and wait for the end of the first command.
        /// </summary>
        ExecutionBehavior ExecutionBehavior { get; set; }

        /// <summary>
        /// Occurs when the state of a command is changed.
        /// </summary>
        event EventHandler<PlcCommandEventArgs> StateChanged;

        /// <summary>
        /// Executes a PLC command.
        /// </summary>
        void Execute(ICommandInput input = null, ICommandOutput output = null);

        /// <summary>
        /// Executes a PLC command asynchronously.
        /// </summary>
        Task ExecuteAsync(ICommandInput input = null, ICommandOutput output = null);

        /// <summary>
        /// Executes a PLC command asynchronously.
        /// </summary>
        Task ExecuteAsync(CancellationToken cancellationToken, ICommandInput input = null, ICommandOutput output = null);
    }
}