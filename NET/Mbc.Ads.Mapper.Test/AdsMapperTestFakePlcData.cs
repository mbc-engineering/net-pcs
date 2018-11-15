using FakeItEasy;
using System;
using System.Collections.Generic;
using TwinCAT.Ads;
using TwinCAT.TypeSystem;

namespace Mbc.Ads.Mapper.Test
{
    public class AdsMapperTestFakePlcData : IDisposable
    {
        public AdsMapperTestFakePlcData()
        {
            AdsSymbolInfo = CreateFakeIAdsSymbolInfo();
            AdsStream = CreateAdsStream();
        }

        public IAdsSymbolInfo AdsSymbolInfo { get; private set; }

        public AdsStream AdsStream { get; private set; }

        private IAdsSymbolInfo CreateFakeIAdsSymbolInfo()
        {
            /* PLC ST_Test deklaration
            TYPE ST_Test :
            STRUCT
                bBoolValue1 : BOOL := TRUE;
                nByteValue1 : BYTE := 255;
                nSbyteValue1 : BYTE := 127;
                nUshortValue1 : UINT := 65535;
                nShortValue1 : INT := 32767;
                nUintValue1 : UDINT := 4294967295;
                nIntValue1 : DINT := 2147483647;
                fFloatValue1 : REAL;
                fDoubleValue1 : LREAL;
                fDoubleValue2 : LREAL;
                fDoubleValue3 : LREAL;
                fDoubleValue4 : LREAL;
                tPlcTimeValue1 : TIME;
                dPlcDateValue1 : DATE;
                dtPlcDateTimeValue1 : DATE_AND_TIME;
                aIntArrayValue : ARRAY[0..2] OF DINT;
                eEnumStateValue : E_State;
            END_STRUCT
            END_TYPE

            TYPE E_State :
            (
                eNone    := 0,
                eStartup := 1,
                eRunning := 2,
                eStop    := 3
            );
            END_TYPE
            */
            var tcAdsSymbol5Fake = A.Fake<ITcAdsSymbol5>();
            var subItemListFake_ST_Test = new List<ITcAdsSubItem>();

            A.CallTo(() => tcAdsSymbol5Fake.Category).Returns(DataTypeCategory.Struct);
            A.CallTo(() => tcAdsSymbol5Fake.Name).Returns("PCS_Status.aPcsTestPlace[1].stTest");
            A.CallTo(() => tcAdsSymbol5Fake.Size).Returns(90);
            A.CallTo(() => tcAdsSymbol5Fake.DataType.Category).Returns(DataTypeCategory.Struct);
            A.CallTo(() => tcAdsSymbol5Fake.DataType.HasSubItemInfo).Returns(true);
            A.CallTo(() => tcAdsSymbol5Fake.DataType.SubItems).Returns(new ReadOnlySubItemCollection(subItemListFake_ST_Test));

            IAdsSymbolInfo fakeSymbolInfo = A.Fake<IAdsSymbolInfo>();
            A.CallTo(() => fakeSymbolInfo.SymbolsSize).Returns(90);
            A.CallTo(() => fakeSymbolInfo.SymbolPath).Returns("PCS_Status.aPcsTestPlace[1].stTest");
            A.CallTo(() => fakeSymbolInfo.Symbol).Returns(tcAdsSymbol5Fake);

            // Start  of faked Sub items of Symbol PCS_Status.aPcsTestPlace[1].stTest of type ST_Test
            //----------------------------------------------------------
            var subItemFake_bBoolValue1 = A.Fake<ITcAdsSubItem>();
            A.CallTo(() => subItemFake_bBoolValue1.SubItemName).Returns("bBoolValue1");
            A.CallTo(() => subItemFake_bBoolValue1.Offset).Returns(0);
            A.CallTo(() => subItemFake_bBoolValue1.Size).Returns(1);
            A.CallTo(() => subItemFake_bBoolValue1.Category).Returns(DataTypeCategory.Alias);
            A.CallTo(() => subItemFake_bBoolValue1.BaseType.DataTypeId).Returns(AdsDatatypeId.ADST_BIT);
            A.CallTo(() => subItemFake_bBoolValue1.BaseType.Category).Returns(DataTypeCategory.Primitive);
            A.CallTo(() => subItemFake_bBoolValue1.BaseType.IsPrimitive).Returns(true);
            A.CallTo(() => subItemFake_bBoolValue1.BaseType.ManagedType).Returns(typeof(System.Boolean));
            A.CallTo(() => subItemFake_bBoolValue1.BaseType.HasSubItemInfo).Returns(false);
            A.CallTo(() => subItemFake_bBoolValue1.BaseType.HasArrayInfo).Returns(false);
            A.CallTo(() => subItemFake_bBoolValue1.BaseType.HasEnumInfo).Returns(false);
            subItemListFake_ST_Test.Add(subItemFake_bBoolValue1);

            var subItemFake_nByteValue1 = A.Fake<ITcAdsSubItem>();
            A.CallTo(() => subItemFake_nByteValue1.SubItemName).Returns("nByteValue1");
            A.CallTo(() => subItemFake_nByteValue1.Offset).Returns(1);
            A.CallTo(() => subItemFake_nByteValue1.Size).Returns(1);
            A.CallTo(() => subItemFake_nByteValue1.Category).Returns(DataTypeCategory.Alias);
            A.CallTo(() => subItemFake_nByteValue1.BaseType.DataTypeId).Returns(AdsDatatypeId.ADST_UINT8);
            A.CallTo(() => subItemFake_nByteValue1.BaseType.Category).Returns(DataTypeCategory.Primitive);
            A.CallTo(() => subItemFake_nByteValue1.BaseType.IsPrimitive).Returns(true);
            A.CallTo(() => subItemFake_nByteValue1.BaseType.ManagedType).Returns(typeof(System.Byte));
            A.CallTo(() => subItemFake_nByteValue1.BaseType.HasSubItemInfo).Returns(false);
            A.CallTo(() => subItemFake_nByteValue1.BaseType.HasArrayInfo).Returns(false);
            A.CallTo(() => subItemFake_nByteValue1.BaseType.HasEnumInfo).Returns(false);
            subItemListFake_ST_Test.Add(subItemFake_nByteValue1);

            var subItemFake_nSbyteValue1 = A.Fake<ITcAdsSubItem>();
            A.CallTo(() => subItemFake_nSbyteValue1.SubItemName).Returns("nSbyteValue1");
            A.CallTo(() => subItemFake_nSbyteValue1.Offset).Returns(2);
            A.CallTo(() => subItemFake_nSbyteValue1.Size).Returns(1);
            A.CallTo(() => subItemFake_nSbyteValue1.Category).Returns(DataTypeCategory.Alias);
            A.CallTo(() => subItemFake_nSbyteValue1.BaseType.DataTypeId).Returns(AdsDatatypeId.ADST_UINT8);
            A.CallTo(() => subItemFake_nSbyteValue1.BaseType.Category).Returns(DataTypeCategory.Primitive);
            A.CallTo(() => subItemFake_nSbyteValue1.BaseType.IsPrimitive).Returns(true);
            A.CallTo(() => subItemFake_nSbyteValue1.BaseType.ManagedType).Returns(typeof(System.Byte));
            A.CallTo(() => subItemFake_nSbyteValue1.BaseType.HasSubItemInfo).Returns(false);
            A.CallTo(() => subItemFake_nSbyteValue1.BaseType.HasArrayInfo).Returns(false);
            A.CallTo(() => subItemFake_nSbyteValue1.BaseType.HasEnumInfo).Returns(false);
            subItemListFake_ST_Test.Add(subItemFake_nSbyteValue1);

            var subItemFake_nUshortValue1 = A.Fake<ITcAdsSubItem>();
            A.CallTo(() => subItemFake_nUshortValue1.SubItemName).Returns("nUshortValue1");
            A.CallTo(() => subItemFake_nUshortValue1.Offset).Returns(4);
            A.CallTo(() => subItemFake_nUshortValue1.Size).Returns(2);
            A.CallTo(() => subItemFake_nUshortValue1.Category).Returns(DataTypeCategory.Alias);
            A.CallTo(() => subItemFake_nUshortValue1.BaseType.DataTypeId).Returns(AdsDatatypeId.ADST_UINT16);
            A.CallTo(() => subItemFake_nUshortValue1.BaseType.Category).Returns(DataTypeCategory.Primitive);
            A.CallTo(() => subItemFake_nUshortValue1.BaseType.IsPrimitive).Returns(true);
            A.CallTo(() => subItemFake_nUshortValue1.BaseType.ManagedType).Returns(typeof(System.UInt16));
            A.CallTo(() => subItemFake_nUshortValue1.BaseType.HasSubItemInfo).Returns(false);
            A.CallTo(() => subItemFake_nUshortValue1.BaseType.HasArrayInfo).Returns(false);
            A.CallTo(() => subItemFake_nUshortValue1.BaseType.HasEnumInfo).Returns(false);
            subItemListFake_ST_Test.Add(subItemFake_nUshortValue1);

            var subItemFake_nShortValue1 = A.Fake<ITcAdsSubItem>();
            A.CallTo(() => subItemFake_nShortValue1.SubItemName).Returns("nShortValue1");
            A.CallTo(() => subItemFake_nShortValue1.Offset).Returns(6);
            A.CallTo(() => subItemFake_nShortValue1.Size).Returns(2);
            A.CallTo(() => subItemFake_nShortValue1.Category).Returns(DataTypeCategory.Alias);
            A.CallTo(() => subItemFake_nShortValue1.BaseType.DataTypeId).Returns(AdsDatatypeId.ADST_INT16);
            A.CallTo(() => subItemFake_nShortValue1.BaseType.Category).Returns(DataTypeCategory.Primitive);
            A.CallTo(() => subItemFake_nShortValue1.BaseType.IsPrimitive).Returns(true);
            A.CallTo(() => subItemFake_nShortValue1.BaseType.ManagedType).Returns(typeof(System.Int16));
            A.CallTo(() => subItemFake_nShortValue1.BaseType.HasSubItemInfo).Returns(false);
            A.CallTo(() => subItemFake_nShortValue1.BaseType.HasArrayInfo).Returns(false);
            A.CallTo(() => subItemFake_nShortValue1.BaseType.HasEnumInfo).Returns(false);
            subItemListFake_ST_Test.Add(subItemFake_nShortValue1);

            var subItemFake_nUintValue1 = A.Fake<ITcAdsSubItem>();
            A.CallTo(() => subItemFake_nUintValue1.SubItemName).Returns("nUintValue1");
            A.CallTo(() => subItemFake_nUintValue1.Offset).Returns(8);
            A.CallTo(() => subItemFake_nUintValue1.Size).Returns(4);
            A.CallTo(() => subItemFake_nUintValue1.Category).Returns(DataTypeCategory.Alias);
            A.CallTo(() => subItemFake_nUintValue1.BaseType.DataTypeId).Returns(AdsDatatypeId.ADST_UINT32);
            A.CallTo(() => subItemFake_nUintValue1.BaseType.Category).Returns(DataTypeCategory.Primitive);
            A.CallTo(() => subItemFake_nUintValue1.BaseType.IsPrimitive).Returns(true);
            A.CallTo(() => subItemFake_nUintValue1.BaseType.ManagedType).Returns(typeof(System.UInt32));
            A.CallTo(() => subItemFake_nUintValue1.BaseType.HasSubItemInfo).Returns(false);
            A.CallTo(() => subItemFake_nUintValue1.BaseType.HasArrayInfo).Returns(false);
            A.CallTo(() => subItemFake_nUintValue1.BaseType.HasEnumInfo).Returns(false);
            subItemListFake_ST_Test.Add(subItemFake_nUintValue1);

            var subItemFake_nIntValue1 = A.Fake<ITcAdsSubItem>();
            A.CallTo(() => subItemFake_nIntValue1.SubItemName).Returns("nIntValue1");
            A.CallTo(() => subItemFake_nIntValue1.Offset).Returns(12);
            A.CallTo(() => subItemFake_nIntValue1.Size).Returns(4);
            A.CallTo(() => subItemFake_nIntValue1.Category).Returns(DataTypeCategory.Alias);
            A.CallTo(() => subItemFake_nIntValue1.BaseType.DataTypeId).Returns(AdsDatatypeId.ADST_INT32);
            A.CallTo(() => subItemFake_nIntValue1.BaseType.Category).Returns(DataTypeCategory.Primitive);
            A.CallTo(() => subItemFake_nIntValue1.BaseType.IsPrimitive).Returns(true);
            A.CallTo(() => subItemFake_nIntValue1.BaseType.ManagedType).Returns(typeof(System.Int32));
            A.CallTo(() => subItemFake_nIntValue1.BaseType.HasSubItemInfo).Returns(false);
            A.CallTo(() => subItemFake_nIntValue1.BaseType.HasArrayInfo).Returns(false);
            A.CallTo(() => subItemFake_nIntValue1.BaseType.HasEnumInfo).Returns(false);
            subItemListFake_ST_Test.Add(subItemFake_nIntValue1);

            var subItemFake_fFloatValue1 = A.Fake<ITcAdsSubItem>();
            A.CallTo(() => subItemFake_fFloatValue1.SubItemName).Returns("fFloatValue1");
            A.CallTo(() => subItemFake_fFloatValue1.Offset).Returns(16);
            A.CallTo(() => subItemFake_fFloatValue1.Size).Returns(4);
            A.CallTo(() => subItemFake_fFloatValue1.Category).Returns(DataTypeCategory.Alias);
            A.CallTo(() => subItemFake_fFloatValue1.BaseType.DataTypeId).Returns(AdsDatatypeId.ADST_REAL32);
            A.CallTo(() => subItemFake_fFloatValue1.BaseType.Category).Returns(DataTypeCategory.Primitive);
            A.CallTo(() => subItemFake_fFloatValue1.BaseType.IsPrimitive).Returns(true);
            A.CallTo(() => subItemFake_fFloatValue1.BaseType.ManagedType).Returns(typeof(System.Single));
            A.CallTo(() => subItemFake_fFloatValue1.BaseType.HasSubItemInfo).Returns(false);
            A.CallTo(() => subItemFake_fFloatValue1.BaseType.HasArrayInfo).Returns(false);
            A.CallTo(() => subItemFake_fFloatValue1.BaseType.HasEnumInfo).Returns(false);
            subItemListFake_ST_Test.Add(subItemFake_fFloatValue1);

            var subItemFake_fDoubleValue1 = A.Fake<ITcAdsSubItem>();
            A.CallTo(() => subItemFake_fDoubleValue1.SubItemName).Returns("fDoubleValue1");
            A.CallTo(() => subItemFake_fDoubleValue1.Offset).Returns(24);
            A.CallTo(() => subItemFake_fDoubleValue1.Size).Returns(8);
            A.CallTo(() => subItemFake_fDoubleValue1.Category).Returns(DataTypeCategory.Alias);
            A.CallTo(() => subItemFake_fDoubleValue1.BaseType.DataTypeId).Returns(AdsDatatypeId.ADST_REAL64);
            A.CallTo(() => subItemFake_fDoubleValue1.BaseType.Category).Returns(DataTypeCategory.Primitive);
            A.CallTo(() => subItemFake_fDoubleValue1.BaseType.IsPrimitive).Returns(true);
            A.CallTo(() => subItemFake_fDoubleValue1.BaseType.ManagedType).Returns(typeof(System.Double));
            A.CallTo(() => subItemFake_fDoubleValue1.BaseType.HasSubItemInfo).Returns(false);
            A.CallTo(() => subItemFake_fDoubleValue1.BaseType.HasArrayInfo).Returns(false);
            A.CallTo(() => subItemFake_fDoubleValue1.BaseType.HasEnumInfo).Returns(false);
            subItemListFake_ST_Test.Add(subItemFake_fDoubleValue1);

            var subItemFake_fDoubleValue2 = A.Fake<ITcAdsSubItem>();
            A.CallTo(() => subItemFake_fDoubleValue2.SubItemName).Returns("fDoubleValue2");
            A.CallTo(() => subItemFake_fDoubleValue2.Offset).Returns(32);
            A.CallTo(() => subItemFake_fDoubleValue2.Size).Returns(8);
            A.CallTo(() => subItemFake_fDoubleValue2.Category).Returns(DataTypeCategory.Alias);
            A.CallTo(() => subItemFake_fDoubleValue2.BaseType.DataTypeId).Returns(AdsDatatypeId.ADST_REAL64);
            A.CallTo(() => subItemFake_fDoubleValue2.BaseType.Category).Returns(DataTypeCategory.Primitive);
            A.CallTo(() => subItemFake_fDoubleValue2.BaseType.IsPrimitive).Returns(true);
            A.CallTo(() => subItemFake_fDoubleValue2.BaseType.ManagedType).Returns(typeof(System.Double));
            A.CallTo(() => subItemFake_fDoubleValue2.BaseType.HasSubItemInfo).Returns(false);
            A.CallTo(() => subItemFake_fDoubleValue2.BaseType.HasArrayInfo).Returns(false);
            A.CallTo(() => subItemFake_fDoubleValue2.BaseType.HasEnumInfo).Returns(false);
            subItemListFake_ST_Test.Add(subItemFake_fDoubleValue2);

            var subItemFake_fDoubleValue3 = A.Fake<ITcAdsSubItem>();
            A.CallTo(() => subItemFake_fDoubleValue3.SubItemName).Returns("fDoubleValue3");
            A.CallTo(() => subItemFake_fDoubleValue3.Offset).Returns(40);
            A.CallTo(() => subItemFake_fDoubleValue3.Size).Returns(8);
            A.CallTo(() => subItemFake_fDoubleValue3.Category).Returns(DataTypeCategory.Alias);
            A.CallTo(() => subItemFake_fDoubleValue3.BaseType.DataTypeId).Returns(AdsDatatypeId.ADST_REAL64);
            A.CallTo(() => subItemFake_fDoubleValue3.BaseType.Category).Returns(DataTypeCategory.Primitive);
            A.CallTo(() => subItemFake_fDoubleValue3.BaseType.IsPrimitive).Returns(true);
            A.CallTo(() => subItemFake_fDoubleValue3.BaseType.ManagedType).Returns(typeof(System.Double));
            A.CallTo(() => subItemFake_fDoubleValue3.BaseType.HasSubItemInfo).Returns(false);
            A.CallTo(() => subItemFake_fDoubleValue3.BaseType.HasArrayInfo).Returns(false);
            A.CallTo(() => subItemFake_fDoubleValue3.BaseType.HasEnumInfo).Returns(false);
            subItemListFake_ST_Test.Add(subItemFake_fDoubleValue3);

            var subItemFake_fDoubleValue4 = A.Fake<ITcAdsSubItem>();
            A.CallTo(() => subItemFake_fDoubleValue4.SubItemName).Returns("fDoubleValue4");
            A.CallTo(() => subItemFake_fDoubleValue4.Offset).Returns(48);
            A.CallTo(() => subItemFake_fDoubleValue4.Size).Returns(8);
            A.CallTo(() => subItemFake_fDoubleValue4.Category).Returns(DataTypeCategory.Alias);
            A.CallTo(() => subItemFake_fDoubleValue4.BaseType.DataTypeId).Returns(AdsDatatypeId.ADST_REAL64);
            A.CallTo(() => subItemFake_fDoubleValue4.BaseType.Category).Returns(DataTypeCategory.Primitive);
            A.CallTo(() => subItemFake_fDoubleValue4.BaseType.IsPrimitive).Returns(true);
            A.CallTo(() => subItemFake_fDoubleValue4.BaseType.ManagedType).Returns(typeof(System.Double));
            A.CallTo(() => subItemFake_fDoubleValue4.BaseType.HasSubItemInfo).Returns(false);
            A.CallTo(() => subItemFake_fDoubleValue4.BaseType.HasArrayInfo).Returns(false);
            A.CallTo(() => subItemFake_fDoubleValue4.BaseType.HasEnumInfo).Returns(false);
            subItemListFake_ST_Test.Add(subItemFake_fDoubleValue4);

            var subItemFake_tPlcTimeValue1 = A.Fake<ITcAdsSubItem>();
            A.CallTo(() => subItemFake_tPlcTimeValue1.SubItemName).Returns("tPlcTimeValue1");
            A.CallTo(() => subItemFake_tPlcTimeValue1.Offset).Returns(56);
            A.CallTo(() => subItemFake_tPlcTimeValue1.Size).Returns(4);
            A.CallTo(() => subItemFake_tPlcTimeValue1.Category).Returns(DataTypeCategory.Alias);
            A.CallTo(() => subItemFake_tPlcTimeValue1.BaseType.DataTypeId).Returns(AdsDatatypeId.ADST_BIGTYPE);
            A.CallTo(() => subItemFake_tPlcTimeValue1.BaseType.Category).Returns(DataTypeCategory.Primitive);
            A.CallTo(() => subItemFake_tPlcTimeValue1.BaseType.IsPrimitive).Returns(true);
            A.CallTo(() => subItemFake_tPlcTimeValue1.BaseType.ManagedType).Returns(typeof(TwinCAT.PlcOpen.TIME));
            A.CallTo(() => subItemFake_tPlcTimeValue1.BaseType.HasSubItemInfo).Returns(false);
            A.CallTo(() => subItemFake_tPlcTimeValue1.BaseType.HasArrayInfo).Returns(false);
            A.CallTo(() => subItemFake_tPlcTimeValue1.BaseType.HasEnumInfo).Returns(false);
            subItemListFake_ST_Test.Add(subItemFake_tPlcTimeValue1);

            var subItemFake_dPlcDateValue1 = A.Fake<ITcAdsSubItem>();
            A.CallTo(() => subItemFake_dPlcDateValue1.SubItemName).Returns("dPlcDateValue1");
            A.CallTo(() => subItemFake_dPlcDateValue1.Offset).Returns(60);
            A.CallTo(() => subItemFake_dPlcDateValue1.Size).Returns(4);
            A.CallTo(() => subItemFake_dPlcDateValue1.Category).Returns(DataTypeCategory.Alias);
            A.CallTo(() => subItemFake_dPlcDateValue1.BaseType.DataTypeId).Returns(AdsDatatypeId.ADST_BIGTYPE);
            A.CallTo(() => subItemFake_dPlcDateValue1.BaseType.Category).Returns(DataTypeCategory.Primitive);
            A.CallTo(() => subItemFake_dPlcDateValue1.BaseType.IsPrimitive).Returns(true);
            A.CallTo(() => subItemFake_dPlcDateValue1.BaseType.ManagedType).Returns(typeof(TwinCAT.PlcOpen.DATE));
            A.CallTo(() => subItemFake_dPlcDateValue1.BaseType.HasSubItemInfo).Returns(false);
            A.CallTo(() => subItemFake_dPlcDateValue1.BaseType.HasArrayInfo).Returns(false);
            A.CallTo(() => subItemFake_dPlcDateValue1.BaseType.HasEnumInfo).Returns(false);
            subItemListFake_ST_Test.Add(subItemFake_dPlcDateValue1);

            var subItemFake_dtPlcDateTimeValue1 = A.Fake<ITcAdsSubItem>();
            A.CallTo(() => subItemFake_dtPlcDateTimeValue1.SubItemName).Returns("dtPlcDateTimeValue1");
            A.CallTo(() => subItemFake_dtPlcDateTimeValue1.Offset).Returns(64);
            A.CallTo(() => subItemFake_dtPlcDateTimeValue1.Size).Returns(4);
            A.CallTo(() => subItemFake_dtPlcDateTimeValue1.Category).Returns(DataTypeCategory.Alias);
            A.CallTo(() => subItemFake_dtPlcDateTimeValue1.BaseType.DataTypeId).Returns(AdsDatatypeId.ADST_BIGTYPE);
            A.CallTo(() => subItemFake_dtPlcDateTimeValue1.BaseType.Category).Returns(DataTypeCategory.Alias);
            A.CallTo(() => subItemFake_dtPlcDateTimeValue1.BaseType.IsPrimitive).Returns(true);
            A.CallTo(() => subItemFake_dtPlcDateTimeValue1.BaseType.ManagedType).Returns(typeof(TwinCAT.PlcOpen.DT));
            A.CallTo(() => subItemFake_dtPlcDateTimeValue1.BaseType.HasSubItemInfo).Returns(false);
            A.CallTo(() => subItemFake_dtPlcDateTimeValue1.BaseType.HasArrayInfo).Returns(false);
            A.CallTo(() => subItemFake_dtPlcDateTimeValue1.BaseType.HasEnumInfo).Returns(false);
            A.CallTo(() => subItemFake_dtPlcDateTimeValue1.BaseType.BaseType.DataTypeId).Returns(AdsDatatypeId.ADST_BIGTYPE);
            A.CallTo(() => subItemFake_dtPlcDateTimeValue1.BaseType.BaseType.Category).Returns(DataTypeCategory.Primitive);
            A.CallTo(() => subItemFake_dtPlcDateTimeValue1.BaseType.BaseType.IsPrimitive).Returns(true);
            A.CallTo(() => subItemFake_dtPlcDateTimeValue1.BaseType.BaseType.ManagedType).Returns(typeof(TwinCAT.PlcOpen.DT));
            A.CallTo(() => subItemFake_dtPlcDateTimeValue1.BaseType.BaseType.HasSubItemInfo).Returns(false);
            A.CallTo(() => subItemFake_dtPlcDateTimeValue1.BaseType.BaseType.Size).Returns(4);
            subItemListFake_ST_Test.Add(subItemFake_dtPlcDateTimeValue1);

            var subItemFake_aIntArrayValue = A.Fake<ITcAdsSubItem>();
            A.CallTo(() => subItemFake_aIntArrayValue.SubItemName).Returns("aIntArrayValue");
            A.CallTo(() => subItemFake_aIntArrayValue.Offset).Returns(68);
            A.CallTo(() => subItemFake_aIntArrayValue.Size).Returns(12);
            A.CallTo(() => subItemFake_aIntArrayValue.Category).Returns(DataTypeCategory.Alias);
            A.CallTo(() => subItemFake_aIntArrayValue.BaseType.DataTypeId).Returns(AdsDatatypeId.ADST_INT32);
            A.CallTo(() => subItemFake_aIntArrayValue.BaseType.Category).Returns(DataTypeCategory.Array);
            A.CallTo(() => subItemFake_aIntArrayValue.BaseType.HasSubItemInfo).Returns(false);
            A.CallTo(() => subItemFake_aIntArrayValue.BaseType.HasArrayInfo).Returns(true);

            DimensionCollection subItemFake_aIntArrayValueDimensions = new DimensionCollection();
            subItemFake_aIntArrayValueDimensions.Add(new Dimension(0, 3));
            A.CallTo(() => subItemFake_aIntArrayValue.BaseType.Dimensions).Returns(new ReadOnlyDimensionCollection(subItemFake_aIntArrayValueDimensions));
            A.CallTo(() => subItemFake_aIntArrayValue.BaseType.HasEnumInfo).Returns(false);
            A.CallTo(() => subItemFake_aIntArrayValue.BaseType.BaseType.DataTypeId).Returns(AdsDatatypeId.ADST_INT32);
            A.CallTo(() => subItemFake_aIntArrayValue.BaseType.BaseType.Category).Returns(DataTypeCategory.Primitive);
            A.CallTo(() => subItemFake_aIntArrayValue.BaseType.BaseType.IsPrimitive).Returns(true);
            A.CallTo(() => subItemFake_aIntArrayValue.BaseType.BaseType.ManagedType).Returns(typeof(System.Int32));
            A.CallTo(() => subItemFake_aIntArrayValue.BaseType.BaseType.HasSubItemInfo).Returns(false);
            A.CallTo(() => subItemFake_aIntArrayValue.BaseType.BaseType.Size).Returns(4);
            subItemListFake_ST_Test.Add(subItemFake_aIntArrayValue);

            var subItemFake_eEnumStateValue = A.Fake<ITcAdsSubItem>();
            A.CallTo(() => subItemFake_eEnumStateValue.SubItemName).Returns("eEnumStateValue");
            A.CallTo(() => subItemFake_eEnumStateValue.Offset).Returns(80);
            A.CallTo(() => subItemFake_eEnumStateValue.Size).Returns(2);
            A.CallTo(() => subItemFake_eEnumStateValue.Category).Returns(DataTypeCategory.Alias);
            A.CallTo(() => subItemFake_eEnumStateValue.BaseType.DataTypeId).Returns(AdsDatatypeId.ADST_INT16);
            A.CallTo(() => subItemFake_eEnumStateValue.BaseType.Category).Returns(DataTypeCategory.Enum);
            A.CallTo(() => subItemFake_eEnumStateValue.BaseType.HasSubItemInfo).Returns(false);
            A.CallTo(() => subItemFake_eEnumStateValue.BaseType.HasArrayInfo).Returns(false);
            A.CallTo(() => subItemFake_eEnumStateValue.BaseType.HasEnumInfo).Returns(true);

            var subItemFake_eEnumStateValueEnumValues = new List<IEnumValue>();
            IEnumValue subItemFake_eEnumStateValue_EnumValue_eNone = A.Fake<IEnumValue>();
            A.CallTo(() => subItemFake_eEnumStateValue_EnumValue_eNone.Primitive).Returns((short)0);
            A.CallTo(() => subItemFake_eEnumStateValue_EnumValue_eNone.Name).Returns("eNone");
            subItemFake_eEnumStateValueEnumValues.Add(subItemFake_eEnumStateValue_EnumValue_eNone);
            IEnumValue subItemFake_eEnumStateValue_EnumValue_eStartup = A.Fake<IEnumValue>();
            A.CallTo(() => subItemFake_eEnumStateValue_EnumValue_eStartup.Primitive).Returns((short)1);
            A.CallTo(() => subItemFake_eEnumStateValue_EnumValue_eStartup.Name).Returns("eStartup");
            subItemFake_eEnumStateValueEnumValues.Add(subItemFake_eEnumStateValue_EnumValue_eStartup);
            IEnumValue subItemFake_eEnumStateValue_EnumValue_eRunning = A.Fake<IEnumValue>();
            A.CallTo(() => subItemFake_eEnumStateValue_EnumValue_eRunning.Primitive).Returns((short)2);
            A.CallTo(() => subItemFake_eEnumStateValue_EnumValue_eRunning.Name).Returns("eRunning");
            subItemFake_eEnumStateValueEnumValues.Add(subItemFake_eEnumStateValue_EnumValue_eRunning);
            IEnumValue subItemFake_eEnumStateValue_EnumValue_eStop = A.Fake<IEnumValue>();
            A.CallTo(() => subItemFake_eEnumStateValue_EnumValue_eStop.Primitive).Returns((short)3);
            A.CallTo(() => subItemFake_eEnumStateValue_EnumValue_eStop.Name).Returns("eStop");
            subItemFake_eEnumStateValueEnumValues.Add(subItemFake_eEnumStateValue_EnumValue_eStop);
            A.CallTo(() => subItemFake_eEnumStateValue.BaseType.EnumValues).Returns(new ReadOnlyEnumValueCollection(new EnumValueCollection(subItemFake_eEnumStateValueEnumValues)));
            A.CallTo(() => subItemFake_eEnumStateValue.BaseType.BaseType.DataTypeId).Returns(AdsDatatypeId.ADST_INT16);
            A.CallTo(() => subItemFake_eEnumStateValue.BaseType.BaseType.Category).Returns(DataTypeCategory.Primitive);
            A.CallTo(() => subItemFake_eEnumStateValue.BaseType.BaseType.IsPrimitive).Returns(true);
            A.CallTo(() => subItemFake_eEnumStateValue.BaseType.BaseType.ManagedType).Returns(typeof(System.Int16));
            A.CallTo(() => subItemFake_eEnumStateValue.BaseType.BaseType.HasSubItemInfo).Returns(false);
            A.CallTo(() => subItemFake_eEnumStateValue.BaseType.BaseType.Size).Returns(2);
            subItemListFake_ST_Test.Add(subItemFake_eEnumStateValue);

            return fakeSymbolInfo;
        }

        private AdsStream CreateAdsStream()
        {
            AdsStream adsStream = new AdsStream();
            var adsWriter = new AdsBinaryWriter(adsStream);

            adsWriter.Write(true);                // offset 0
            adsWriter.Write(byte.MaxValue);       // offset 1
            adsWriter.Write(sbyte.MaxValue);     // offset 2
            adsWriter.Write((sbyte)0);                  // offset 3 auffüllen mit 1 Byte
            adsWriter.Write(ushort.MaxValue);   // offset 4
            adsWriter.Write(short.MaxValue);     // offset 6
            adsWriter.Write(uint.MaxValue);       // offset 8
            adsWriter.Write(int.MaxValue);         // offset 12
            adsWriter.Write(float.MaxValue);     // offset 16
            adsWriter.Write(0f);                  // offset 20 auffüllen mit 4 Byte
            adsWriter.Write(default(double));   // offset 24
            adsWriter.Write(double.MaxValue);   // offset 32
            adsWriter.Write(double.MaxValue);   // offset 40
            adsWriter.Write(double.MaxValue);   // offset 48
            adsWriter.WritePlcType(new TimeSpan(19, 33, 44));               // offset 56 (TIME mit 4 Byte)
            adsWriter.WritePlcType(new DateTime(2018, 08, 30));             // offset 60 (Date mit 4 Byte)
            adsWriter.WritePlcType(new DateTime(2018, 08, 30, 19, 33, 44)); // offset 64 (Date_AND_TIME mit 4 Byte)

            // Write Array
            adsWriter.Write(100);                  // offset 68
            adsWriter.Write(101);                  // offset 72
            adsWriter.Write(102);                  // offset 76

            // Write enum
            adsWriter.Write((ushort)2);            // offset 80 (Enum of type INT16)

            // Write Motor Object
            adsWriter.Write(double.MaxValue);      // offset 82

            adsStream.Position = 0;
            return adsStream;
        }

        public void Dispose()
        {
            AdsStream?.Dispose();
            AdsStream = null;
            AdsSymbolInfo = null;
        }
    }
}
