//-----------------------------------------------------------------------------
// Copyright (c) 2020 by mbc engineering, CH-6015 Luzern
// Licensed under the Apache License, Version 2.0
//-----------------------------------------------------------------------------

using FakeItEasy;
using Mbc.Pcs.Net.Command;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using TwinCAT.Ads;
using TwinCAT.Ads.TypeSystem;
using TwinCAT.TypeSystem;
using static Mbc.Pcs.Net.Command.PlcCommand;

namespace Mbc.Pcs.Net.Test.Util.Command
{
    public class AdsCommandConnectionFake
    {
        private static IAdsConnection _systemTestAdsConnection = null;
        private readonly IAdsConnection _adsConnection = A.Fake<IAdsConnection>();
        private readonly IAdsSymbol _adsSymbols = A.Fake<IAdsSymbol>(x => x.Implements<IStructInstance>());
        private readonly List<IMember> _fakedVariables = new List<IMember>();
        private readonly Dictionary<uint, string> _variableHandles = new Dictionary<uint, string>();
        private Tuple<PlcCommand, DataExchange<CommandChangeData>> _userData = null;
        private CommandHandshakeStruct _handshakeStruct;

        private object _userDataLock = new object();

        public AdsCommandConnectionFake()
            : this(PlcCommandFakeOption.ResponseImmediatelyFinished)
        {
        }

        public AdsCommandConnectionFake(PlcCommandFakeOption option)
        {
            A.CallTo(() => _adsConnection.IsConnected)
                .Invokes(() => Debug.WriteLine("call faked AdsConnection.IsConnected and return true"))
                .Returns(true);

            A.CallTo(() => _adsConnection.Address)
                .Invokes(() => Debug.WriteLine("call faked AdsConnection.Address and return faked local ams adress"))
                .Returns(new AmsAddress(851));

            var fakeStructType = A.Fake<IStructType>();
            A.CallTo(() => _adsSymbols.DataType)
                .Returns(fakeStructType);
            A.CallTo(() => fakeStructType.Members)
                .ReturnsLazily(() => new ReadOnlyMemberCollection(new MemberCollection(_fakedVariables)));

            A.CallTo(() => _adsConnection.ReadSymbol(A<string>._))
                .ReturnsLazily(parm =>
                {
                    string symbolPath = (string)parm.Arguments[0];

                    if (option == PlcCommandFakeOption.ResponseFbPathNotExist)
                    {
                        Debug.WriteLine($"call faked AdsConnection.ReadSymbolInfo(name={symbolPath}) and return no symbols because simulation of command does not exist");
                        return null;
                    }

                    Debug.WriteLine($"call faked AdsConnection.ReadSymbolInfo(name={symbolPath}) and return faked symbols");
                    return _adsSymbols;
                });

            if (option == PlcCommandFakeOption.ResponseFbPathNotExist)
            {
                A.CallTo(() => _adsConnection.CreateVariableHandle(A<string>._))
                    .Invokes(parm => Debug.WriteLine($"call faked AdsConnection.CreateVariableHandle(variableName={parm.Arguments[0].ToString()}) and throw AdsErrorException because simulation of command does not exist"))
                    .Throws(parm => throw new AdsErrorException($"simulation symbol {parm.Arguments[0].ToString()} does not exist", AdsErrorCode.DeviceSymbolNotFound));
            }
            else
            {
                A.CallTo(() => _adsConnection.CreateVariableHandle(A<string>._))
                    .ReturnsLazily(parm =>
                    {
                        Debug.WriteLine($"call faked AdsConnection.CreateVariableHandle(variableName={parm.Arguments[0].ToString()})");

                        uint hndl = (uint)parm.Arguments[0].GetHashCode();
                        // save handle
                        _variableHandles[hndl] = parm.Arguments[0].ToString();

                        Debug.WriteLine($"Return faked AdsConnection.CreateVariableHandle(variableName={parm.Arguments[0].ToString()}) value => {hndl}");
                        return hndl;
                    });

                // Sum-Variable Handle in SumCommandBase
                // TODO muss noch fertig implementiert werden
                int readBytes;
                A.CallTo(() => _adsConnection.TryReadWrite(0xF082U, A<uint>.Ignored, A<Memory<byte>>.Ignored, A<ReadOnlyMemory<byte>>.Ignored, out readBytes))
                    .Invokes(param =>
                    {
                        // 0 = 61570    (Sum Read/Write)
                        // 1 = 2        (Number of sum sub-commands)
                        // 2 = 24 bytes
                        // 3 = Sum-Request

                        uint numberSubCmds = (uint)param.Arguments[1];
                        ReadOnlyMemory<byte> writeBuffer = (ReadOnlyMemory<byte>)param.Arguments[3];
                        var reader = new BinaryReader(new MemoryStream(writeBuffer.ToArray()));

                        for (int i = 0; i < numberSubCmds; i++)
                        {
                            var indexGroup = reader.ReadUInt32();
                            var indexOffset = reader.ReadUInt32();
                        }


                    })
                    .Returns(AdsErrorCode.Succeeded)
                    .AssignsOutAndRefParameters(10);
            }

            A.CallTo(() => _adsConnection.WriteAny(A<uint>._, A<object>._))
                .Invokes(parm =>
                {
                    Debug.WriteLine($"call faked AdsConnection.WriteAny(variableHandle={parm.Arguments[0].ToString()}, value={parm.Arguments[1].ToString()})");
                    lock (_userDataLock)
                    {
                        // Dedect Cancel Request from PlcCommand over CancellationToken
                        if ((parm.Arguments[1] is bool cancelValue1 && cancelValue1 == false && _userData != null)
                            || (_variableHandles.TryGetValue((uint)parm.Arguments[0], out string variable)
                                && variable.EndsWith(".stHandshake.bExecute")
                                && parm.Arguments[1] is bool cancelValue
                                && cancelValue == false && _userData != null))
                        {
                            // Raise Cancel Data Exchange from SPS
                            _handshakeStruct.Progress = 50;
                            _handshakeStruct.ResultCode = (ushort)CommandResultCode.Cancelled;
                            _handshakeStruct.Busy = true;
                            _handshakeStruct.Execute = false;
                            var notification = new Notification(0 /*handle*/, new DateTimeOffset(1, TimeSpan.Zero), _userData, null);
                            var eventArgs = new AdsNotificationExEventArgs(notification, _handshakeStruct);

                            Debug.WriteLine("Raise Faked AdsConnection.AdsNotificationEx");
                            _adsConnection.AdsNotificationEx += Raise.FreeForm<EventHandler<AdsNotificationExEventArgs>>
                                .With(_adsConnection, eventArgs);
                        }
                    }
                });

            A.CallTo(() => _adsConnection.AddDeviceNotificationEx(A<string>._, A<NotificationSettings>._, A<object>._, A<Type>._))
                .Invokes(parm =>
                {
                    Debug.WriteLine($"call faked AdsConnection.AddDeviceNotificationEx(variableName={parm.Arguments[0].ToString()}, userData={parm.Arguments[2].ToString()})");
                    lock (_userDataLock)
                    {
                        _userData = parm.Arguments[2] as Tuple<PlcCommand, DataExchange<CommandChangeData>>;

                        if (parm.Arguments[1] is NotificationSettings notificationSettings && notificationSettings.NotificationMode == AdsTransMode.OnChange)
                        {
                            var initalHandshakeStruct = new CommandHandshakeStruct
                            {
                                SubTask = 0,
                                Execute = true,
                                Busy = true,
                                Progress = 0,
                                ResultCode = (ushort)CommandResultCode.Initialized,
                            };

                            var notification = new Notification(80, new DateTimeOffset(0, TimeSpan.Zero), _userData, null);
                            var initialEventArgs = new AdsNotificationExEventArgs(notification, initalHandshakeStruct);

                            Debug.WriteLine("Raise faked initialevent AdsConnection.AdsNotificationEx");
                            _adsConnection.AdsNotificationEx += Raise.FreeForm<EventHandler<AdsNotificationExEventArgs>>
                                .With(_adsConnection, initialEventArgs);
                        }

                        // Send simulated Result Data
                        if (option != PlcCommandFakeOption.NoResponse)
                        {
                            _handshakeStruct = new CommandHandshakeStruct
                            {
                                SubTask = ResponseSubTask,
                            };

                            if (option == PlcCommandFakeOption.ResponseDelayedCancel)
                            {
                                Task.Delay(200);
                                _handshakeStruct.Progress = 50;
                                _handshakeStruct.ResultCode = (ushort)CommandResultCode.Cancelled;
                                _handshakeStruct.Busy = true;
                                _handshakeStruct.Execute = false;
                            }
                            else
                            {
                                if (option == PlcCommandFakeOption.ResponseDelayedFinished)
                                {
                                    Task.Delay(200);
                                }

                                _handshakeStruct.Progress = 100;
                                _handshakeStruct.ResultCode = ResponseStatusCode;
                            }

                            var notification = new Notification(80, new DateTimeOffset(2000, 1, 2, 3, 4, 5, TimeSpan.Zero), _userData, null);
                            var eventArgs = new AdsNotificationExEventArgs(notification, _handshakeStruct);

                            Debug.WriteLine("Raise faked AdsConnection.AdsNotificationEx");

                            _adsConnection.AdsNotificationEx += Raise.FreeForm<EventHandler<AdsNotificationExEventArgs>>
                                .With(_adsConnection, eventArgs);
                        }
                    }
                })
                .Returns(80u);
        }

        /// <summary>
        /// The value of the ResultCode. Works only with PlcCommandFakeOption.ResponseImmediatelyFinished.
        /// Default is Done, but can be used for simulation of user specifc codes
        /// </summary>
        public ushort ResponseStatusCode { get; set; } = (ushort)CommandResultCode.Done;

        public ushort ResponseSubTask { get; set; } = 0;

        public DateTime ResponseTimestamp { get; set; } = DateTime.FromFileTime(1);

        public IAdsConnection AdsConnection
        {
            get
            {
                return _systemTestAdsConnection ?? _adsConnection;
            }
        }

        /// <summary>
        /// When a real system test will be executed, all fakes are ignored and a real PLC Connection will be established
        /// </summary>
        public static void SetSystemTestConnection(IAdsConnection adsConnection)
        {
            _systemTestAdsConnection = adsConnection;
        }

        public void AddAdsSubItem(string itemName, Type managedType, bool input)
        {
            var fakeMember = A.Fake<IMember>(x => x.Implements<IManagedMappableType>());

            A.CallTo(() => fakeMember.InstanceName)
                .Returns(itemName);

            A.CallTo(() => fakeMember.ByteSize)
                .Returns(Marshal.SizeOf(managedType));

            // TODO is this called?
            A.CallTo(() => ((IManagedMappableType)fakeMember).ManagedType)
                .Returns(managedType);

            var fakeDataType = A.Fake<IDataType>(x => x.Implements<IManagedMappableType>());
            A.CallTo(() => fakeMember.DataType)
                .Returns(fakeDataType);
            A.CallTo(() => fakeDataType.Category)
                .Returns(DataTypeCategory.Primitive);
            A.CallTo(() => ((IManagedMappableType)fakeDataType).ManagedType)
                .Returns(managedType);

            if (input)
            {
                A.CallTo(() => fakeMember.Attributes)
                    .Returns(new ReadOnlyTypeAttributeCollection(new TypeAttributeCollection(new List<ITypeAttribute>() { new InputTypeAttributeFake() })));
            }
            else
            {
                A.CallTo(() => fakeMember.Attributes)
                    .Returns(new ReadOnlyTypeAttributeCollection(new TypeAttributeCollection(new List<ITypeAttribute>() { new OutputTypeAttributeFake() })));
            }

            _fakedVariables.Add(fakeMember);
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
