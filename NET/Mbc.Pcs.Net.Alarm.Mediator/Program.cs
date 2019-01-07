using Microsoft.Extensions.CommandLineUtils;
using Newtonsoft.Json;
using System;
using TwinCAT.Ads;

namespace Mbc.Pcs.Net.Alarm.Mediator
{
    public static class Program
    {
        /// <summary>
        /// Startup arguments:
        /// '-?' -> display command arguments
        /// '-adsnetid' -> connection id for the alarm service
        /// ---
        /// stdin Parameters when process is running:
        /// 'quit' -> disconnect the PlcAlarmProxy from plc
        /// </summary>
        /// <param name="args">the start arguments</param>
        public static void Main(string[] args)
        {
            CommandLineApplication commandLineApplication = new CommandLineApplication(throwOnUnexpectedArg: false);

            commandLineApplication.HelpOption("-? | -h | --help");
            var adsNetId = commandLineApplication.Option("-$|-ads |--adsnetid <adsnetid>", "Enter the ADS Net id of the TwinCat Alarm & Event Server.", CommandOptionType.SingleValue, false);

            commandLineApplication.OnExecute(() =>
                {
                    // Missing parameter exit application
                    if (!adsNetId.HasValue() || !AmsNetId.TryParse(adsNetId.Value(), out AmsNetId netId))
                    {
                        return 2; // Closed with error
                    }

                    using (var plcAlarm = new PlcAlarmProxy(netId.ToString()))
                    {
                        plcAlarm.AlarmChanged += OnAlarmChanged;
                        plcAlarm.Connect();

                        // wait for quit command
                        string input = string.Empty;
                        do
                        {
                            input = Console.ReadLine();
                            // Dedect close of input stream -> Close of parent application
                            if (input == null)
                            {
                                return 1; // Cancel
                            }
                        } while (!string.Equals(input, "quit", StringComparison.OrdinalIgnoreCase));

                        plcAlarm.Disconnect();
                    }

                    Console.WriteLine($"disconected");

                    return 0; // OK
                });

            // Parse input arguments
            Environment.ExitCode = commandLineApplication.Execute(args);
        }

        private static void OnAlarmChanged(object sender, PlcAlarmChangeEventArgs e)
        {
            Console.WriteLine(JsonConvert.SerializeObject(e));
        }
    }
}
