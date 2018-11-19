//-----------------------------------------------------------------------------
// Copyright (c) 2018 by mbc engineering GmbH, CH-6015 Luzern
// Licensed under the Apache License, Version 2.0
//-----------------------------------------------------------------------------

using FluentAssertions;
using Mbc.Ads.Mapper;
using Mbc.Pcs.Net.Command;
using System;
using System.Collections;
using System.Collections.Generic;
using TwinCAT.Ads;
using Xbehave;

namespace Mbc.Pcs.Net.Test.Command
{
    public class CommandWithMapperArgsTest : IDisposable
    {
        private readonly TcAdsClient _connection;

        public CommandWithMapperArgsTest()
        {
            _connection = new TcAdsClient();
            _connection.Connect(851);
        }

        public void Dispose()
        {
            _connection.Disconnect();
            _connection.Dispose();
        }

        [Scenario()]
        public void ExecuteCommandWithStructArgument()
        {
            AdsMapperConfiguration<CommandArgs> mapperConfig = null;
            AdsMapper<CommandArgs> mapperInput = null;
            AdsMapper<CommandArgs> mapperOutput = null;
            PlcCommand command = null;
            CommandArgs inputData = null;
            CommandArgs outputData = null;
            double outputDataFloat = double.NaN;

            "Given a ADS mapper configuration"
                .x(() => mapperConfig = new AdsMapperConfiguration<CommandArgs>(cfg => cfg.ForAllSourceMember(opt => opt.RemovePrefix("f", "n", "e"))));

            "And a ADS mapper from the configuration"
                .x(() =>
                {
                    mapperInput = mapperConfig.CreateAdsMapper(AdsSymbolReader.Read(_connection, "Commands.fbStructCommand.stInputArgs"));
                    mapperOutput = mapperConfig.CreateAdsMapper(AdsSymbolReader.Read(_connection, "Commands.fbStructCommand.stInputArgs"));
                });

            "And a command with an ADS stream argument handler"
                .x(() => command = new PlcCommand(_connection, "Commands.fbStructCommand", commandArgumentHandler: new AdsStreamCommandArgumentHandler()));

            "And the given input arguments"
                .x(() => inputData = new CommandArgs { Number = 42, Float = 0.42f, Enum = EnumType.Value1 });

            "When the command is executed with the input class data"
                .x(() =>
                {
                    var input = CommandInputBuilder.FromDictionary(new Dictionary<string, object>
                    {
                        ["stInputArgs"] = mapperInput.MapDataObject(inputData),
                        ["nNumber"] = 420,
                        ["eEnum"] = EnumType.Value2,
                    });

                    var output = CommandOutputBuilder.FromDictionary(new Dictionary<string, object>
                    {
                        ["stOutputArgs"] = null,
                        ["fFloat"] = AdsStreamCommandArgumentHandler.ReadAsPrimitiveMarker,
                    });

                    command.Execute(input: input, output: output);

                    outputData = mapperOutput.MapStream(output.GetOutputData<AdsStream>("stOutputArgs"));
                    outputDataFloat = output.GetOutputData<double>("fFloat");
                });

            "Then the output must match the input."
                .x(() =>
                {
                    outputData.Number.Should().Be(42);
                    outputData.Float.Should().Be(0.42f);
                    outputData.Enum.Should().Be(EnumType.Value1);
                    outputDataFloat.Should().Be(0.42f);
                });
        }


        /// <summary>
        /// Testclass for arguments.
        /// </summary>
        public class CommandArgs
        {
            public int Number { get; set; }
            public float Float { get; set; }
            public EnumType Enum { get; set; }
        }

        public enum EnumType
        {
            Value0, Value1, Value2,
        }
    }
}
