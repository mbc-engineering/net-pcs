﻿using FluentAssertions;
using System;
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
                cfg => cfg.ForAllSourceMember(opt => opt.RemovePrefix('f', 'n', 'b', 'a', 'e'))
                  .ForMember(dest => dest.DoubleValue4MappedName, opt => opt.MapFrom("fdoublevalue4"))
                  .ForMember(dest => dest.DoubleValue4MappedName, opt => opt.ConvertUsing(value => Math.Min(100.0, (double)value))));

            // Act
            AdsMapper<DestinationDataObject> mapper = config.CreateAdsMapper(_fakePlcData.AdsSymbolInfo);
            DestinationDataObject mappedResult = mapper.MapStream(_fakePlcData.AdsStream);

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
                  .ForMember(dest => dest.DoubleValue4MappedName, opt => opt.ConvertUsing(value => Math.Min(100.0, (double)value))));

            // Act
            AdsMapper<DestinationDataObject> mapper = config.CreateAdsMapper(_fakePlcData.AdsSymbolInfo);
            DestinationDataObject mappedResult = mapper.MapStream(_fakePlcData.AdsStream);

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
                  .ForMember(dest => dest.DoubleValue4MappedName, opt => opt.ConvertUsing(value => Math.Min(100.0, (double)value))));

            // Act
            AdsMapper<DestinationDataObject> mapper = config.CreateAdsMapper(_fakePlcData.AdsSymbolInfo);
            DestinationDataObject mappedResult = mapper.MapStream(_fakePlcData.AdsStream);

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
            DestinationDataObject mappedResult = mapper.MapStream(_fakePlcData.AdsStream);

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
            DestinationDataObject mappedResult = mapper.MapStream(_fakePlcData.AdsStream);

            // Assert
            mappedResult.MotorObject.Should().NotBeNull();
            mappedResult.MotorObject.ActualSpeed.Should().Be(double.MaxValue);
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