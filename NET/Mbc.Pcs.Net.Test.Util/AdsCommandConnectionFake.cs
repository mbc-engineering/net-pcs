using FakeItEasy;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using TwinCAT.Ads;
using TwinCAT.TypeSystem;
using static Mbc.Pcs.Net.PlcCommand;

namespace Mbc.Pcs.Net.Test.Util
{
    public class AdsCommandConnectionFake
    {
        private readonly IAdsConnection _adsConnection = A.Fake<IAdsConnection>();
        private readonly ITcAdsSymbol5 _adsSymbols = A.Fake<ITcAdsSymbol5>();
        private readonly List<ITcAdsSubItem> _fakedVariables = new List<ITcAdsSubItem>();

        public AdsCommandConnectionFake()
        {
            A.CallTo(() => _adsSymbols.DataType.SubItems)
                .Returns(new ReadOnlySubItemCollection(_fakedVariables));

            A.CallTo(() => _adsConnection.IsConnected)
                .Returns(true);

            A.CallTo(() => _adsConnection.Address)
                .Returns(new AmsAddress(851)); 

            A.CallTo(() => _adsConnection.ReadSymbolInfo(A<string>._))
                .ReturnsLazily(parm =>
                {
                    string symbolPath = (string)parm.Arguments[0];

                    return _adsSymbols;
                });

            A.CallTo(() => _adsConnection.AddDeviceNotificationEx(A<string>._, A<AdsTransMode>._, A<int>._, A<int>._, A<object>._, A<Type>._))
                .Invokes(parm =>
                {
                    var userData = parm.Arguments[4] as Tuple<PlcCommand, DataExchange<CommandHandshakeStruct>>;
                    var handshake = userData.Item2.Data; // Return the blank strukture, this is like the finish command
                    handshake.Progress = 100;
                    handshake.ResultCode = (ushort)CommandResultCode.Done;

                    var eventArgs = new AdsNotificationExEventArgs(1, userData, 80, handshake);

                    _adsConnection.AdsNotificationEx += Raise.FreeForm<AdsNotificationExEventHandler>
                        .With(_adsConnection, eventArgs);
                })
                .Returns(80);
        }

        public IAdsConnection AdsConnection => _adsConnection;

        public void AddAdsSubItem(string itemName, Type managedType, bool input)
        {
            var adsSubItem = A.Fake<ITcAdsSubItem>();

            A.CallTo(() => adsSubItem.SubItemName)
                .Returns(itemName);

            A.CallTo(() => adsSubItem.ByteSize)
                .Returns(Marshal.SizeOf(managedType));

            A.CallTo(() => adsSubItem.BaseType.ManagedType)
                .Returns(managedType);

            A.CallTo(() => adsSubItem.BaseType.Category)
                .Returns(DataTypeCategory.Primitive);

            if (input)
            {
                A.CallTo(() => adsSubItem.Attributes)
                    .Returns(new ReadOnlyTypeAttributeCollection(new TypeAttributeCollection(new List<ITypeAttribute>() { new InputTypeAttributeFake() })));
            }
            else
            {
                A.CallTo(() => adsSubItem.Attributes)
                    .Returns(new ReadOnlyTypeAttributeCollection(new TypeAttributeCollection(new List<ITypeAttribute>() { new OutputTypeAttributeFake() })));
            }

            _fakedVariables.Add(adsSubItem);
        }

        private class InputTypeAttributeFake : ITypeAttribute
        {
            public string Name => PlcAttributeNames.PlcCommandInput;

            public string Value => string.Empty;
        }

        private class OutputTypeAttributeFake : ITypeAttribute
        {
            public string Name => PlcAttributeNames.PlcCommandOutput;

            public string Value => string.Empty;
        }
    }
}
