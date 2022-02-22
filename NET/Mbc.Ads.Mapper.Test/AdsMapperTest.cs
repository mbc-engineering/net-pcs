//-----------------------------------------------------------------------------
// Copyright (c) 2020 by mbc engineering, CH-6015 Luzern
// Licensed under the Apache License, Version 2.0
//-----------------------------------------------------------------------------

using FluentAssertions;
using System;
using System.Diagnostics;
using Xunit;
using Xunit.Abstractions;

namespace Mbc.Ads.Mapper.Test
{
    public class AdsMapperTest : IClassFixture<AdsMapperTestFakePlcData>
    {
        private readonly ITestOutputHelper _testOutput;
        private readonly AdsMapperTestFakePlcData _fakePlcData;

        public AdsMapperTest(ITestOutputHelper testOutput, AdsMapperTestFakePlcData fakePlcData)
        {
            _testOutput = testOutput;
            _fakePlcData = fakePlcData;
        }

        [Fact]
        public void AdsMappingConfigurationShouldMapAdsStreamToDataObject()
        {
            // Arrange
            AdsMapperConfiguration<DestinationDataObject> config = new AdsMapperConfiguration<DestinationDataObject>(
                cfg => cfg.ForAllSourceMember(opt => opt.RemovePrefix("f", "n", "b", "a", "e", "t", "d", "dt", "s", "ws"))
                  .ForMember(dest => dest.DoubleValue4MappedName, opt => opt.MapFrom("fdoublevalue4"))
                  .ForMember(dest => dest.DoubleValue4MappedName, opt => opt.ConvertFromSourceUsing(value => ((double)value) / 2)));

            // Act
            AdsMapper<DestinationDataObject> mapper = config.CreateAdsMapper(_fakePlcData.AdsSymbolInfo);

            var buffer = new ReadOnlySpan<byte>(_fakePlcData.Data);
            DestinationDataObject mappedResult = mapper.MapData(buffer);

            // Assert
            mappedResult.Should().NotBeNull();
            mappedResult.BoolValue1.Should().Be(true);
            mappedResult.ByteValue1.Should().Be(byte.MaxValue);
            mappedResult.SbyteValue1.Should().Be(sbyte.MaxValue);
            mappedResult.UshortValue1.Should().Be(ushort.MaxValue);
            mappedResult.ShortValue1.Should().Be(short.MaxValue);
            mappedResult.UintValue1.Should().Be(uint.MaxValue);
            mappedResult.IntValue1.Should().Be(int.MaxValue);
            mappedResult.FloatValue1.Should().Be(float.MaxValue);
            mappedResult.DoubleValue1.Should().Be(default);
            mappedResult.DoubleValue2.Should().Be(double.MaxValue);
            mappedResult.DoubleValue3.Should().Be(double.MaxValue);
            mappedResult.DoubleValue4MappedName.Should().Be(100.0);

            mappedResult.IntArrayValue.Should().NotBeNull();
            mappedResult.IntArrayValue.Length.Should().Be(3);
            mappedResult.IntArrayValue[0].Should().Be(100);
            mappedResult.IntArrayValue[1].Should().Be(101);
            mappedResult.IntArrayValue[2].Should().Be(102);

            mappedResult.EnumStateValue.Should().Be(State.Running);

            mappedResult.PlcVersion.Should().Be("21.08.30.0");
            mappedResult.Utf7String.Should().Be("ÄÖö@Ü7");
            mappedResult.UnicodeString.Should().Be("ÄÖö@Ü8");
        }

        [Fact]
        public void AdsMappingConfigurationPerformance()
        {
            // Arrange
            AdsMapperConfiguration<DestinationDataObject> config = new AdsMapperConfiguration<DestinationDataObject>(
                cfg => cfg.ForAllSourceMember(opt => opt.RemovePrefix("f", "n", "b", "a", "e", "t", "d", "dt", "s", "ws"))
                  .ForMember(dest => dest.DoubleValue4MappedName, opt => opt.MapFrom("fdoublevalue4"))
                  .ForMember(dest => dest.DoubleValue4MappedName, opt => opt.ConvertFromSourceUsing(value => ((double)value) / 2)));
            AdsMapper<DestinationDataObject> mapper = config.CreateAdsMapper(_fakePlcData.AdsSymbolInfo);

            // Act
            var stopwatch = Stopwatch.StartNew();
            var buffer = new ReadOnlySpan<byte>(_fakePlcData.Data);
            for (int i = 0; i < 50000; i++)
            {
                DestinationDataObject mappedResult = mapper.MapData(buffer);
            }

            stopwatch.Stop();

            // Assert
            _testOutput.WriteLine("50'000 Mappings = {0}", stopwatch.Elapsed);
        }

        [Fact(Skip = "Currently not implemented")]
        public void AdsMappingConfigurationShouldMapAdsStreamToDataObjectWithRequired()
        {
            // Arrange
            AdsMapperConfiguration<DestinationDataObject> config = new AdsMapperConfiguration<DestinationDataObject>(cfg =>
                cfg.ForAllSourceMember(opt => opt.RemovePrefix('f', 'n', 'b', 'a', 'e'))
                  .ForMember(dest => dest.DoubleValue2, opt => opt.Require())
                  .ForSourceMember("symbolName", opt => opt.Require())
                  .ForMember(dest => dest.DoubleValue4MappedName, opt => opt.MapFrom("fdoublevalue4"))
                  .ForMember(dest => dest.DoubleValue4MappedName, opt => opt.ConvertFromSourceUsing(value => Math.Min(100.0, (double)value))));

            // Act
            AdsMapper<DestinationDataObject> mapper = config.CreateAdsMapper(_fakePlcData.AdsSymbolInfo);
            var buffer = new ReadOnlySpan<byte>(_fakePlcData.Data);
            DestinationDataObject mappedResult = mapper.MapData(buffer);

            // Assert
            throw new NotImplementedException("Required fields not yet implemented");
        }

        [Fact(Skip = "Currently not implemented")]
        public void AdsMappingConfigurationShouldMapAdsStreamToDataObjectWithIgnored()
        {
            // Arrange
            AdsMapperConfiguration<DestinationDataObject> config = new AdsMapperConfiguration<DestinationDataObject>(cfg =>
                cfg.ForAllSourceMember(opt => opt.RemovePrefix('f', 'n', 'b', 'a', 'e'))
                  .ForMember(dest => dest.DoubleValue1, opt => opt.Ignore())
                  .ForSourceMember("symbolName", opt => opt.Ignore())
                  .ForMember(dest => dest.DoubleValue4MappedName, opt => opt.MapFrom("fdoublevalue4"))
                  .ForMember(dest => dest.DoubleValue4MappedName, opt => opt.ConvertFromSourceUsing(value => Math.Min(100.0, (double)value))));

            // Act
            AdsMapper<DestinationDataObject> mapper = config.CreateAdsMapper(_fakePlcData.AdsSymbolInfo);
            var buffer = new ReadOnlySpan<byte>(_fakePlcData.Data);
            DestinationDataObject mappedResult = mapper.MapData(buffer);

            // Assert
            throw new NotImplementedException("Ignored fields not yet implemented");
        }

        [Fact(Skip = "Currently not implemented")]
        public void AdsMappingConfigurationShouldMapAdsStreamToDataObjectWithTimeDataTypes()
        {
            // Arrange
            AdsMapperConfiguration<DestinationDataObject> config = new AdsMapperConfiguration<DestinationDataObject>(cfg =>
                cfg.ForAllSourceMember(opt => opt.RemovePrefix('f', 'n', 'b', 'a', 'e')));

            // Act
            AdsMapper<DestinationDataObject> mapper = config.CreateAdsMapper(_fakePlcData.AdsSymbolInfo);
            var buffer = new ReadOnlySpan<byte>(_fakePlcData.Data);
            DestinationDataObject mappedResult = mapper.MapData(buffer);

            // Assert
            mappedResult.Should().NotBeNull();
            mappedResult.PlcTimeValue1.Should().Be(new TimeSpan(19, 33, 44));
            mappedResult.PlcDateValue1.Should().Be(new DateTime(2018, 08, 30));
            mappedResult.PlcDateTimeValue1.Should().Be(new DateTime(2018, 08, 30, 19, 33, 44));
        }

        [Fact(Skip = "Currently not implemented")]
        public void AdsMappingConfigurationShouldMapAdsStreamToDataObjectWithSubStructures()
        {
            // Arrange
            AdsMapperConfiguration<DestinationDataObject> config = new AdsMapperConfiguration<DestinationDataObject>(cfg =>
                cfg.ForAllSourceMember(opt => opt.RemovePrefix('f', 'n', 'b', 'a', 'e')));

            // Act
            AdsMapper<DestinationDataObject> mapper = config.CreateAdsMapper(_fakePlcData.AdsSymbolInfo);
            var buffer = new ReadOnlySpan<byte>(_fakePlcData.Data);
            DestinationDataObject mappedResult = mapper.MapData(buffer);

            // Assert
            mappedResult.MotorObject.Should().NotBeNull();
            mappedResult.MotorObject.ActualSpeed.Should().Be(double.MaxValue);
        }

        [Fact]
        public void AdsMappingConfigurationShouldMapDataObjectToAdsStream()
        {
            // Arrange
            AdsMapperConfiguration<DestinationDataObject> config = new AdsMapperConfiguration<DestinationDataObject>(
                cfg => cfg.ForAllSourceMember(opt => opt.RemovePrefix("f", "n", "b", "a", "e", "t", "d", "dt", "s", "ws"))
                  .ForMember(dest => dest.DoubleValue4MappedName, opt => opt.MapFrom("fdoublevalue4"))
                  .ForMember(dest => dest.DoubleValue4MappedName, opt => opt.ConvertToSourceUsing((value, type) => value * 2)));
            var dataObject = new DestinationDataObject
            {
                BoolValue1 = true,
                ByteValue1 = byte.MaxValue,
                SbyteValue1 = sbyte.MaxValue,
                UshortValue1 = ushort.MaxValue,
                ShortValue1 = short.MaxValue,
                UintValue1 = uint.MaxValue,
                IntValue1 = int.MaxValue,
                FloatValue1 = float.MaxValue,
                DoubleValue1 = default,
                DoubleValue2 = double.MaxValue,
                DoubleValue3 = double.MaxValue,
                DoubleValue4MappedName = 100,
                PlcTimeValue1 = new TimeSpan(19, 33, 44),
                PlcDateValue1 = new DateTime(2018, 08, 30),
                PlcDateTimeValue1 = new DateTime(2018, 08, 30, 19, 33, 44),
                IntArrayValue = new int[] { 100, 101, 102 },
                EnumStateValue = State.Running,
                PlcVersion = "21.08.30.0",
                Utf7String = "ÄÖö@Ü7",
                UnicodeString = "ÄÖö@Ü8",
            };
            byte[] expectedData = _fakePlcData.Data;
            Array.Clear(expectedData, 114, 8); // nested MotorObject

            var buffer = new byte[expectedData.Length];

            // Act
            AdsMapper<DestinationDataObject> mapper = config.CreateAdsMapper(_fakePlcData.AdsSymbolInfo);
            mapper.MapDataObject(new Span<byte>(buffer), dataObject);

            // Assert
            buffer.Should().BeEquivalentTo(expectedData);
        }

        #region " Test Data "

        private class DestinationDataObject
        {
            public bool BoolValue1 { get; set; }
            public byte ByteValue1 { get; set; }
            public sbyte SbyteValue1 { get; set; }
            public ushort UshortValue1 { get; set; }
            public short ShortValue1 { get; set; }
            public uint UintValue1 { get; set; }
            public int IntValue1 { get; set; }
            public float FloatValue1 { get; set; }
            public double DoubleValue1 { get; set; }
            public double DoubleValue2 { get; set; }
            public double DoubleValue3 { get; set; }
            public double DoubleValue4MappedName { get; set; }
            public TimeSpan PlcTimeValue1 { get; set; }
            public DateTime PlcDateValue1 { get; set; }
            public DateTime PlcDateTimeValue1 { get; set; }
            public int[] IntArrayValue { get; set; } = new int[3];
            public State EnumStateValue { get; set; }
            public string PlcVersion { get; set; }
            public string Utf7String { get; set; }
            public string UnicodeString { get; set; }
            public Motor MotorObject { get; set; }
        }

        private class Motor
        {
            public double ActualSpeed { get; set; }
        }

        private enum State
        {
            None = 0,
            Startup = 1,
            Running = 2,
            Stop = 3,
        }

        #endregion
    }
}
