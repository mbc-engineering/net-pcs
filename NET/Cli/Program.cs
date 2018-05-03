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
        static void Main(string[] args)
        {
            var client = new TcAdsClient()
            {
                Synchronize = false
            };
            client.Connect("172.16.23.55.1.1", 851);


            var count = 0;
            while (true)
            {
                var command = new PlcCommand(client, "Commands.fbAddCommand1");
                command.Execute();

                if (count++ % 75 == 0)
                {
                    Console.Write("\r\n{0}: ", count);
                }
                Console.Write(".");
            }
        }
    }
}
