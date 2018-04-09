using AtomizerUI.link;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TwinCAT.Ads;

namespace CallAds
{
    class Program
    {
        static void Main(string[] args)
        {
            var client = new TcAdsClient
            {
                Synchronize = false
            };

            client.Connect(851);

#if false
            var command = new PcsCommand(client, "Commands.stAddCommand")
            {
                ResultType = typeof(AddOut)
            };

            var input = new AddIn
            {
                a = 4, 
                b = 8
            };
            var output = (AddOut)command.Execute(input);
            Console.WriteLine("Result: {0}", output.result);
            Console.ReadKey();
#else
            var command = new PcsCommand(client, "Commands.stAddLongCommand")
            {
                ResultType = typeof(AddOut)
            };

            var input = new AddIn
            {
                a = 4, 
                b = 8
            };

            var cancelSource = new CancellationTokenSource();

            command.ExecuteAsync(input, cancelSource.Token)
                .ContinueWith(x => {
                    if (x.Exception != null)
                        Console.WriteLine("Error {0}", x.Exception);
                    else
                        Console.WriteLine("Result {0}", ((AddOut)x.Result).result);
                });

            var key = Console.ReadKey();
            if (key.Key == ConsoleKey.C)
            {
                Console.WriteLine("Cancel");
                cancelSource.Cancel();
                Console.ReadKey();
            }
#endif
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    class AddIn
    {
        public float a;
        public float b;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    class AddOut
    {
        public float result;
    }
}
