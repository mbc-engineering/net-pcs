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
            // TODO bridge to NLog to ILogger
            var client = new AdsClient();
            client.Connect(851);
#if true
            var command = new PcsCommand2<AddCommandParam.Output>(client, "Commands.AddCommand1");

            var input = new AddCommandParam.Input
            {
                Val1 = 3,
                Val2 = 5
            };
            var output = command.Execute(input);
            Console.WriteLine("Result: {0}", output.Result);
            Console.ReadKey();
#endif

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
#endif
#if false
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

    public class AddCommandParam
    {
        public class Input
        {
            public float Val1 { get; set; }
            public float Val2 { get; set; }
        }

        public class Output
        {
            public float Result { get; set; }
        }
    }
}
