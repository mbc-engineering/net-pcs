using Mbc.Pcs.Net.Command;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TwinCAT.Ads;

namespace Cli
{
    class Program
    {
        static async Task Main(string[] a)
        {
            var client = new TcAdsClient()
            {
                Synchronize = false
            };
            client.Connect("172.16.23.2.1.1", 851);


            var input = new Dictionary<string, object>
            {
                { "eInverter", 1 },
                { "sInverterErrorFilePath", "c:/Foo" },
            };

            var output = new Dictionary<string, object>
            {
                { "Result", null }
            };

            var count = 0;
            while (true)
            {
                //var command = new PlcCommand(client, "Commands.fbAddCommand1");
                var command = new PlcCommand(client, "GVL_UIData.stUiCommands.fbRequestAutomaticModeCommand");
                command.Timeout = TimeSpan.FromSeconds(60);
                command.StateChanged += (sender, args) => PrintProgress(args.Progress, args.SubTask);
                CancellationTokenSource cancellationToken = new CancellationTokenSource();
                Task.Factory.StartNew(() =>
                 {
                     bool quit = false;
                     do
                     {
                         quit = string.Equals(Console.ReadLine(), "q", StringComparison.OrdinalIgnoreCase);
                         if (quit)
                         {
                             cancellationToken.Cancel();
                         }
                     } while (!quit);
                 });
                try
                {
                    Console.WriteLine(" press q + ENTER to cancel");
                    await command.ExecuteAsync(cancellationToken.Token, input: CommandInputBuilder.FromDictionary(input));
                }
                catch (Exception ex)
                {
                    Console.WriteLine(" canceled by user.");
                }
                
                Console.WriteLine(" => " + output["Result"]);
                Console.WriteLine(" press key to continue ");
                Console.ReadLine();
                count++;
            }            
        }

        private static void PrintProgress(int progress, int subTask)
        {
            Console.CursorLeft = 0;
            var progressString = string.Join(string.Empty, Enumerable.Range(1, progress / 2).Select(x => "#"));
            Console.Write("{0}: {1}", subTask, progressString);
        }
    }
}
