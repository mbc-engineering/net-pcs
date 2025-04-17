//-----------------------------------------------------------------------------
// Copyright (c) 2020 by mbc engineering, CH-6015 Luzern
// Licensed under the Apache License, Version 2.0
//-----------------------------------------------------------------------------

using FluentAssertions;
using Mbc.Ads.Mapper;
using Mbc.Pcs.Net.Command;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Generic;
using TwinCAT.Ads;
using Xunit;

namespace Mbc.Pcs.Net.Test.Command
{
    public class CommandWithMapperArgsTest : IDisposable
    {
        private readonly ILogger _logger;
        private readonly AdsClient _connection;

        public CommandWithMapperArgsTest()
        {
            _logger = NullLogger<CommandWithMapperArgsTest>.Instance;

            _connection = new AdsClient();
            _connection.Connect("204.35.225.246.1.1", 851);
        }

        public void Dispose()
        {
            _connection.Disconnect();
            _connection.Dispose();
        }

        [Fact(Skip = "Nur mit SPS möglich")]
        public void ExecuteCommandWithStructArgument()
        {
            AdsMapperConfiguration<CommandArgs> mapperConfig = null;
            AdsMapper<CommandArgs> mapperInput = null;
            AdsMapper<CommandArgs> mapperOutput = null;
            PlcCommand command = null;
            CommandArgs inputData = null;
            CommandArgs outputData = null;
            double outputDataFloat = double.NaN;

            // "Given a ADS mapper configuration"
            mapperConfig = new AdsMapperConfiguration<CommandArgs>(cfg => cfg.ForAllSourceMember(opt => opt.RemovePrefix("f", "n", "e")));

            // "And a ADS mapper from the configuration"
            mapperInput = mapperConfig.CreateAdsMapper(AdsSymbolReader.Read(_connection, "Commands.fbStructCommand.stInputArgs"));
            mapperOutput = mapperConfig.CreateAdsMapper(AdsSymbolReader.Read(_connection, "Commands.fbStructCommand.stInputArgs"));

            // "And a command with an ADS stream argument handler"
            command = new PlcCommand(_connection, "Commands.fbStructCommand", _logger, commandArgumentHandler: new AdsStreamCommandArgumentHandler());

            // "And the given input arguments"
            inputData = new CommandArgs { Number = 42, Float = 0.42f, Enum = EnumType.Value1 };

            //"When the command is executed with the input class data"
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

            outputData = mapperOutput.MapData(output.GetOutputData<ReadOnlyMemory<byte>>("stOutputArgs").Span);
            outputDataFloat = output.GetOutputData<double>("fFloat");

            // "Then the output must match the input."
            outputData.Number.Should().Be(42);
            outputData.Float.Should().Be(0.42f);
            outputData.Enum.Should().Be(EnumType.Value1);
            outputDataFloat.Should().Be(0.42f);
        }

#pragma warning disable CA1034 // Nested types should not be visible
#pragma warning disable CA1720 // Identifier contains type name
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
            Value0,
            Value1,
            Value2,
        }
#pragma warning restore CA1720 // Identifier contains type name
#pragma warning restore CA1034 // Nested types should not be visible
    }
}
