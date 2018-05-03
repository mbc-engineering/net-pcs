using MbcAdcCommand;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TwinCAT.Ads;

namespace Cli
{
    class Program
    {
        static void Main(string[] a)
        {
            var client = new TcAdsClient()
            {
                Synchronize = false
            };
            client.Connect("172.16.23.55.1.1", 851);


            var input = new Dictionary<string, object>
            {
                { "Val1", 0 },
                { "Val2", 0 },
            };

            var output = new Dictionary<string, object>
            {
                { "Result", null }
            };

            var count = 0;
            while (true)
            {
                input["Val1"] = count;
                input["Val2"] = 2 * count;

                //var command = new PlcCommand(client, "Commands.fbAddCommand1");
                var command = new PlcCommand(client, "Commands.fbDelayedAddCommand1");
                command.StateChanged += (sender, args) => PrintProgress(args.Progress, args.SubTask);
                command.Execute(input: CommandInputBuilder.FromDictionary(input), output: CommandOutputBuilder.FromDictionary(output));


                Console.WriteLine(" => " + output["Result"]);
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
