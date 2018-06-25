﻿namespace Mbc.Pcs.Net
{
    /// <summary>
    /// Provides information of state change events of a <see cref="PlcCommand"/>.
    /// </summary>
    /// <seealso cref="PlcCommand"/>
    public class PlcCommandEventArgs
    {
        /// <summary>
        /// Gets the progress value from 0 to 100 percent.
        /// </summary>
        public int Progress { get; }

        /// <summary>
        /// Gets the command specific sub task identifier.
        /// </summary>
        public int SubTask { get; }

        public bool IsFinished { get; }

        public bool IsCancelled { get; }

        internal PlcCommandEventArgs(int progress, int subTask, bool finished, bool cancelled)
        {
            Progress = progress;
            SubTask = subTask;
            IsFinished = finished;
            IsCancelled = cancelled;
        }

    }
}