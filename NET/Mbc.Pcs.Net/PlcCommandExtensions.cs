using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Mbc.Pcs.Net
{
    public static class PlcCommandExtensions
    {
        /// <summary>
        /// Only one execution per time
        /// Declare sempahore like: <code>private static readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);</code>
        /// </summary>
        /// <param name="command"></param>
        /// <param name="semaphore"></param>
        /// <param name="messageOnFail"></param>
        /// <returns></returns>
        public static async Task LockedExecuteAsync(this PlcCommand command, SemaphoreSlim semaphore, string messageOnFail)
        {
            await semaphore.WaitAsync();

            try
            {
                await command.ExecuteAsync();
            }
            catch (PlcCommandException e)
            {
                if (string.IsNullOrWhiteSpace(messageOnFail))
                {
                    throw new PlcCommandException(command.AdsCommandFbPath, e);
                }
                else
                {
                    throw new PlcCommandException(command.AdsCommandFbPath, messageOnFail, e);
                }
            }
            finally
            {
                semaphore.Release();
            }
        }
    }
}
