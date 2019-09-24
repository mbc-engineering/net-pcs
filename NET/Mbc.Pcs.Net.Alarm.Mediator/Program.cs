using Microsoft.Extensions.CommandLineUtils;
using Newtonsoft.Json;
using NLog;
using System;
using System.Reflection;
using TwinCAT.Ads;

namespace Mbc.Pcs.Net.Alarm.Mediator
{
    public static class Program
    {
        private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();

        private static PlcAlarmProxy _plcAlarmProxy;

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
            Logger.Info("Startup Application with Version={version} on host={host} with arguments={args}.", Assembly.GetExecutingAssembly().GetName().Version.ToString(), Environment.MachineName, args);

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
                        plcAlarm.OnDisconnect += OnDisconnect;
                        _plcAlarmProxy = plcAlarm;
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

            Logger.Info("Exit Application with with {exitCode}", Environment.ExitCode);
        }

        private static void OnDisconnect(object sender, EventArgs e)
        {
            try
            {
                _plcAlarmProxy?.Dispose(); // Release COM objects and clean-up.
            }
            finally
            {
                Environment.Exit(1); // Exit application
            }
        }

        private static void OnAlarmChanged(object sender, PlcAlarmChangeEventArgs e)
        {
            string data = JsonConvert.SerializeObject(e);
            Logger.Debug("On Alarm changed with data={json}", data);

            Console.WriteLine(JsonConvert.SerializeObject(e));
        }
    }
}
