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
        /// '-languageId' -> the language id to use for the alarm messages
        /// ---
        /// stdin Parameters when process is running:
        /// 'quit' -> disconnect the PlcAlarmProxy from plc
        /// </summary>
        /// <param name="args">the start arguments</param>
        public static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.InputEncoding = System.Text.Encoding.UTF8;

            CommandLineApplication commandLineApplication = new CommandLineApplication(throwOnUnexpectedArg: false);

            commandLineApplication.HelpOption("-? | -h | --help");
            var adsNetId = commandLineApplication.Option("-$|-ads |--adsnetid <adsnetid>", "Enter the ADS Net id of the TwinCat Alarm & Event Server.", CommandOptionType.SingleValue, false);
            var langId = commandLineApplication.Option("-l |--languageid  <languageid>", "Enter the language-id to use for getting the alarm messages (default is english).", CommandOptionType.SingleValue, false);

            commandLineApplication.OnExecute(() =>
                {
                    // Missing parameter exit application
                    if (!adsNetId.HasValue() || !AmsNetId.TryParse(adsNetId.Value(), out AmsNetId netId))
                    {
                        return 2; // Closed with error
                    }

                    var languageId = langId.HasValue() ? int.Parse(langId.Value()) : 1033; // Get target language code or take english as default.
                    using (var plcAlarm = new PlcAlarmProxy(netId.ToString(), languageId))
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
