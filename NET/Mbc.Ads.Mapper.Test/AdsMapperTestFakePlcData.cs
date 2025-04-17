using FakeItEasy;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using TwinCAT.Ads.TypeSystem;
using TwinCAT.PlcOpen;
using TwinCAT.TypeSystem;

namespace Mbc.Ads.Mapper.Test
{
    public class AdsMapperTestFakePlcData : IDisposable
    {
        public AdsMapperTestFakePlcData()
        {
            AdsSymbolInfo = CreateFakeIAdsSymbolInfo();
            Data = CreateAdsStream();
        }

        public IAdsSymbolInfo AdsSymbolInfo { get; private set; }

        public byte[] Data { get; private set; }

        private IAdsSymbolInfo CreateFakeIAdsSymbolInfo()
        {
            /* PLC ST_Test deklaration
            TYPE ST_Test :
            STRUCT
                bBoolValue1 : BOOL := TRUE;
                nByteValue1 : BYTE := 255;
                nSbyteValue1 : SINT := 127;
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
                sPlcVersion : STRING(10) := '21.08.30.0';
                sUtf7String : STRING(6) := 'ÄÖö@Ü7';
                wsUnicodeString : WSTRING(6) := "ÄÖö@Ü8";
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

            IAdsSymbolInfo fakeSymbolInfo = A.Fake<IAdsSymbolInfo>();

            IAdsSymbol fakeSymbol = A.Fake<IAdsSymbol>(x => x.Implements<IStructInstance>());
            A.CallTo(() => fakeSymbolInfo.Symbol).Returns(fakeSymbol);
            A.CallTo(() => fakeSymbol.ByteSize).Returns(120);

            IStructType fakeStructType = A.Fake<IStructType>();
            A.CallTo(() => fakeSymbol.DataType).Returns(fakeStructType);

            var fakeMembers = new List<IMember>();
            var fakeMemberCollection = new MockMemberCollection(fakeMembers);
            A.CallTo(() => fakeStructType.Members).Returns(fakeMemberCollection);

            fakeMembers.Add(CreateFakePrimitiveMember<bool>("bBoolValue1", 0));
            fakeMembers.Add(CreateFakePrimitiveMember<byte>("nByteValue1", 1));
            fakeMembers.Add(CreateFakePrimitiveMember<sbyte>("nSbyteValue1", 2));
            fakeMembers.Add(CreateFakePrimitiveMember<ushort>("nUshortValue1", 4));
            fakeMembers.Add(CreateFakePrimitiveMember<short>("nShortValue1", 6));
            fakeMembers.Add(CreateFakePrimitiveMember<uint>("nUintValue1", 8));
            fakeMembers.Add(CreateFakePrimitiveMember<int>("nIntValue1", 12));
            fakeMembers.Add(CreateFakePrimitiveMember<float>("fFloatValue1", 16));
            fakeMembers.Add(CreateFakePrimitiveMember<double>("fDoubleValue1", 24));
            fakeMembers.Add(CreateFakePrimitiveMember<double>("fDoubleValue2", 32));
            fakeMembers.Add(CreateFakePrimitiveMember<double>("fDoubleValue3", 40));
            fakeMembers.Add(CreateFakePrimitiveMember<double>("fDoubleValue4", 48));

            fakeMembers.Add(CreateFakePrimitiveMember<TIME>("tPlcTimeValue1", 56));
            fakeMembers.Add(CreateFakePrimitiveMember<DATE>("dPlcDateValue1", 60));
            fakeMembers.Add(CreateFakePrimitiveMember<DT>("dtPlcDateTimeValue1", 64));

            fakeMembers.Add(CreateFakePrimitiveArrayMember<int>("aIntArrayValue", 68));

            fakeMembers.Add(CreateFakeEnumMember<short>("eEnumStateValue", 80));

            fakeMembers.Add(CreateFakeStringMember("sPlcVersion", 82, StringMarshaler.DefaultEncoding, 10, 11));
            fakeMembers.Add(CreateFakeStringMember("sUtf7String", 93, StringMarshaler.DefaultEncoding, 6, 7));
            fakeMembers.Add(CreateFakeStringMember("wsUnicodeString", 100, StringMarshaler.UTF16, 6, 14));

            return fakeSymbolInfo;
        }

        private IMember CreateFakePrimitiveMember<T>(string instName, int offset)
        {
            var fakeMember = A.Fake<IMember>();
            A.CallTo(() => fakeMember.InstanceName).Returns(instName);
            A.CallTo(() => fakeMember.Offset).Returns(offset);

            var fakeType = A.Fake<IPrimitiveType>(x => x.Implements<IManagedMappableType>());
            A.CallTo(() => fakeMember.DataType).Returns(fakeType);
            A.CallTo(() => fakeType.Category).Returns(DataTypeCategory.Primitive);
            A.CallTo(() => ((IManagedMappableType)fakeType).ManagedType).Returns(typeof(T));

            return fakeMember;
        }

        private IMember CreateFakePrimitiveArrayMember<T>(string instName, int offset)
        {
            var fakeMember = A.Fake<IMember>();
            A.CallTo(() => fakeMember.InstanceName).Returns(instName);
            A.CallTo(() => fakeMember.Offset).Returns(offset);

            var fakeType = A.Fake<IArrayType>();
            A.CallTo(() => fakeMember.DataType).Returns(fakeType);
            A.CallTo(() => fakeType.Category).Returns(DataTypeCategory.Array);
            A.CallTo(() => fakeType.Dimensions.ElementCount).Returns(3);

            var fakeElementType = A.Fake<IDataType>(x => x.Implements<IManagedMappableType>());
            A.CallTo(() => fakeType.ElementType).Returns(fakeElementType);
            A.CallTo(() => fakeElementType.Category).Returns(DataTypeCategory.Primitive);
            A.CallTo(() => fakeElementType.ByteSize).Returns(4);
            A.CallTo(() => ((IManagedMappableType)fakeElementType).ManagedType).Returns(typeof(T));

            return fakeMember;
        }

        private IMember CreateFakeEnumMember<T>(string instName, int offset)
        {
            var fakeMember = A.Fake<IMember>();
            A.CallTo(() => fakeMember.InstanceName).Returns(instName);
            A.CallTo(() => fakeMember.Offset).Returns(offset);

            var fakeType = A.Fake<IEnumType>();
            A.CallTo(() => fakeMember.DataType).Returns(fakeType);
            A.CallTo(() => fakeType.Category).Returns(DataTypeCategory.Enum);

            var fakeElementType = A.Fake<IDataType>(x => x.Implements<IManagedMappableType>());
            A.CallTo(() => fakeType.BaseType).Returns(fakeElementType);
            A.CallTo(() => ((IManagedMappableType)fakeElementType).ManagedType).Returns(typeof(T));

            /* Enum values */

            var fakeEnumValues = new List<IEnumValue>();

            IEnumValue fakeEnumValue_eNone = A.Fake<IEnumValue>();
            A.CallTo(() => fakeEnumValue_eNone.Primitive).Returns((short)0);
            A.CallTo(() => fakeEnumValue_eNone.Name).Returns("eNone");
            fakeEnumValues.Add(fakeEnumValue_eNone);

            IEnumValue fakeEnumValue_eStartup = A.Fake<IEnumValue>();
            A.CallTo(() => fakeEnumValue_eStartup.Primitive).Returns((short)1);
            A.CallTo(() => fakeEnumValue_eStartup.Name).Returns("eStartup");
            fakeEnumValues.Add(fakeEnumValue_eStartup);

            IEnumValue fakeEnumValue_eRunning = A.Fake<IEnumValue>();
            A.CallTo(() => fakeEnumValue_eRunning.Primitive).Returns((short)2);
            A.CallTo(() => fakeEnumValue_eRunning.Name).Returns("eRunning");
            fakeEnumValues.Add(fakeEnumValue_eRunning);

            IEnumValue fakeEnumValue_eStop = A.Fake<IEnumValue>();
            A.CallTo(() => fakeEnumValue_eStop.Primitive).Returns((short)3);
            A.CallTo(() => fakeEnumValue_eStop.Name).Returns("eStop");
            fakeEnumValues.Add(fakeEnumValue_eStop);

            A.CallTo(() => fakeType.EnumValues).Returns(new ReadOnlyEnumValueCollection(new EnumValueCollection(fakeEnumValues)));

            return fakeMember;
        }

        private IMember CreateFakeStringMember(string instName, int offset, Encoding encoding, int length, int byteSize)
        {
            var fakeMember = A.Fake<IMember>();
            A.CallTo(() => fakeMember.InstanceName).Returns(instName);
            A.CallTo(() => fakeMember.Offset).Returns(offset);

            var fakeType = A.Fake<IStringType>(x => x.Implements<IManagedMappableType>());
            A.CallTo(() => fakeMember.DataType).Returns(fakeType);
            A.CallTo(() => fakeType.Category).Returns(DataTypeCategory.String);
            A.CallTo(() => fakeType.IsFixedLength).Returns(true);
            A.CallTo(() => ((IManagedMappableType)fakeType).ManagedType).Returns(typeof(string));
            A.CallTo(() => fakeType.Encoding).Returns(encoding);
            A.CallTo(() => fakeType.Length).Returns(length);
            A.CallTo(() => fakeType.ByteSize).Returns(byteSize);

            return fakeMember;
        }

        private byte[] CreateAdsStream()
        {
            var buffer = new MemoryStream();
            var writer = new BinaryWriter(buffer);

            writer.Write(true);                 // offset 0
            writer.Write(byte.MaxValue);        // offset 1
            writer.Write(sbyte.MaxValue);       // offset 2
            writer.Write((sbyte)0);             // offset 3 auffüllen mit 1 Byte
            writer.Write(ushort.MaxValue);      // offset 4
            writer.Write(short.MaxValue);       // offset 6
            writer.Write(uint.MaxValue);        // offset 8
            writer.Write(int.MaxValue);         // offset 12
            writer.Write(float.MaxValue);       // offset 16
            writer.Write(0f);                   // offset 20 auffüllen mit 4 Byte
            writer.Write(default(double));      // offset 24
            writer.Write(double.MaxValue);      // offset 32
            writer.Write(double.MaxValue);      // offset 40
            writer.Write(200d);                 // offset 48

            writer.Write(new TIME(new TimeSpan(19, 33, 44)).Ticks);                // offset 56 (TIME mit 4 Byte)
            writer.Write(new DATE(new DateTime(2018, 08, 30)).Ticks);                // offset 60 (Date mit 4 Byte)
            writer.Write(new DT(new DateTime(2018, 08, 30, 19, 33, 44)).Ticks);  // offset 64 (Date_AND_TIME mit 4 Byte)

            // Write Array
            writer.Write(100);                  // offset 68
            writer.Write(101);                  // offset 72
            writer.Write(102);                  // offset 76

            // Write enum
            writer.Write((ushort)2);            // offset 80 (Enum of type INT16)

            // Write string
            var str1 = new byte[11];
            StringMarshaler.DefaultEncoding.GetBytes("21.08.30.0", 0, 10, str1, 0);
            writer.Write(str1);                 // offset 82

            var str2 = new byte[7];
            StringMarshaler.DefaultEncoding.GetBytes("ÄÖö@Ü7", 0, 6, str2, 0);
            writer.Write(str2);                 // offset 93

            var str3 = new byte[14];
            StringMarshaler.UTF16.GetBytes("ÄÖö@Ü8", 0, 6, str3, 0);
            writer.Write(str3);                 // offset 100

            // Write Motor Object
            writer.Write(double.MaxValue);      // offset 114

            buffer.Position = 0;
            return buffer.ToArray();
        }

        public void Dispose()
        {
            AdsSymbolInfo = null;
        }

        private class MockMemberCollection
            : IMemberCollection
        {
            private readonly List<IMember> _members;

            public MockMemberCollection(List<IMember> members)
            {
                _members = members;
            }

            public IMember this[string instancePath] => throw new NotImplementedException();

            public IMember this[int index] { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

            public IInstanceCollection<IMember> Statics => throw new NotImplementedException();

            public IInstanceCollection<IMember> Instances => throw new NotImplementedException();

            public InstanceCollectionMode Mode => throw new NotImplementedException();

            public int Count => throw new NotImplementedException();

            public bool IsReadOnly => throw new NotImplementedException();

            public void Add(IMember item)
            {
                throw new NotImplementedException();
            }

            public int CalcSize()
            {
                throw new NotImplementedException();
            }

            public void Clear()
            {
                throw new NotImplementedException();
            }

            public bool Contains(string instancePath)
            {
                throw new NotImplementedException();
            }

            public bool Contains(IMember item)
            {
                throw new NotImplementedException();
            }

            public bool ContainsName(string instanceName)
            {
                throw new NotImplementedException();
            }

            public void CopyTo(IMember[] array, int arrayIndex)
            {
                throw new NotImplementedException();
            }

            public IMember GetInstance(string instancePath)
            {
                throw new NotImplementedException();
            }

            public IList<IMember> GetInstanceByName(string instanceName)
            {
                throw new NotImplementedException();
            }

            public int IndexOf(IMember item)
            {
                throw new NotImplementedException();
            }

            public void Insert(int index, IMember item)
            {
                throw new NotImplementedException();
            }

            public bool Remove(IMember item)
            {
                throw new NotImplementedException();
            }

            public void RemoveAt(int index)
            {
                throw new NotImplementedException();
            }

            public bool TryGetInstance(string instancePath, out IMember symbol)
            {
                throw new NotImplementedException();
            }

            public bool TryGetInstanceByName(string instanceName, out IList<IMember> symbols)
            {
                throw new NotImplementedException();
            }

            public bool TryGetMember(string memberName, out IMember symbol)
            {
                throw new NotImplementedException();
            }

            public IEnumerator<IMember> GetEnumerator() => _members.GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() => _members.GetEnumerator();
        }
    }
}
