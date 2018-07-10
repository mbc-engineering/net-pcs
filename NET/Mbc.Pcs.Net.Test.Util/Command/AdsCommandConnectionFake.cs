//-----------------------------------------------------------------------------
// Copyright (c) 2018 by mbc engineering GmbH, CH-6015 Luzern
// Licensed under the Apache License, Version 2.0
//-----------------------------------------------------------------------------

using FakeItEasy;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using TwinCAT.Ads;
using TwinCAT.TypeSystem;
using Mbc.Pcs.Net.Command;
using static Mbc.Pcs.Net.Command.PlcCommand;
using System.Threading.Tasks;
using System.Diagnostics;

namespace Mbc.Pcs.Net.Test.Util.Command
{
    public class AdsCommandConnectionFake
    {
        private static IAdsConnection _systemTestAdsConnection = null;
        private readonly IAdsConnection _adsConnection = A.Fake<IAdsConnection>();
        private readonly ITcAdsSymbol5 _adsSymbols = A.Fake<ITcAdsSymbol5>();
        private readonly List<ITcAdsSubItem> _fakedVariables = new List<ITcAdsSubItem>();
        private readonly Dictionary<int, string> _variableHandles = new Dictionary<int, string>();
        Tuple<PlcCommand, DataExchange<CommandHandshakeStruct>> _userData = null;

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

            A.CallTo(() => _adsSymbols.DataType.SubItems)
                .Returns(new ReadOnlySubItemCollection(_fakedVariables));

            A.CallTo(() => _adsConnection.ReadSymbolInfo(A<string>._))
                .ReturnsLazily(parm =>
                {
                    string symbolPath = (string)parm.Arguments[0];
                    
                    if(option == PlcCommandFakeOption.ResponseFbPathNotExist)
                    {
                        Debug.WriteLine($"call faked AdsConnection.ReadSymbolInfo(name={symbolPath}) and return no symbols because simulation of command does not exist");
                        return null;
                    }

                    Debug.WriteLine($"call faked AdsConnection.ReadSymbolInfo(name={symbolPath}) and return faked symbols");
                    return _adsSymbols;
                });

            A.CallTo(() => _adsConnection.CreateVariableHandle(A<string>._))
                .ReturnsLazily(parm =>
                {
                    if (option == PlcCommandFakeOption.ResponseFbPathNotExist)
                    {
                        Debug.WriteLine($"call faked AdsConnection.CreateVariableHandle(variableName={parm.Arguments[0].ToString()}) and throw AdsErrorException because simulation of command does not exist");
                        throw new AdsErrorException($"simulation symbol {parm.Arguments[0].ToString()} does not exist", AdsErrorCode.DeviceSymbolNotFound);
                    }

                    Debug.WriteLine($"call faked AdsConnection.CreateVariableHandle(variableName={parm.Arguments[0].ToString()})");

                    int hndl = parm.Arguments[0].GetHashCode();
                    // save handle
                    _variableHandles[hndl] = parm.Arguments[0].ToString();

                    Debug.WriteLine($"Return faked AdsConnection.CreateVariableHandle(variableName={parm.Arguments[0].ToString()}) value => {hndl}");
                    return hndl;
                });

            A.CallTo(() => _adsConnection.WriteAny(A<int>._, A<object>._))
                .Invokes(parm =>
                {
                    Debug.WriteLine($"call faked AdsConnection.WriteAny(variableHandle={parm.Arguments[0].ToString()}, value={parm.Arguments[1].ToString()})");
                    lock (_userDataLock)
                    {
                        // Dedect Cancel Request from PlcCommand over CancellationToken
                        if (parm.Arguments[1] is bool cancelValue1 && cancelValue1 == false && _userData != null
                            || _variableHandles.TryGetValue((int)parm.Arguments[0], out string variable) && variable.EndsWith(".stHandshake.bExecute")
                            && parm.Arguments[1] is bool cancelValue && cancelValue == false && _userData != null)
                        {
                            // Raise Cancel Data Exchange from SPS
                            var handshake = _userData.Item2.Data;
                            handshake.Progress = 50;
                            handshake.ResultCode = (ushort)CommandResultCode.Cancelled;
                            handshake.Busy = true;
                            handshake.Execute = false;

                            var eventArgs = new AdsNotificationExEventArgs(1, _userData, 80, handshake);

                            Debug.WriteLine("Raise Faked AdsConnection.AdsNotificationEx");
                            _adsConnection.AdsNotificationEx += Raise.FreeForm<AdsNotificationExEventHandler>
                                .With(_adsConnection, eventArgs);
                        }
                    }
                });

                A.CallTo(() => _adsConnection.AddDeviceNotificationEx(A<string>._, A<AdsTransMode>._, A<int>._, A<int>._, A<object>._, A<Type>._))
                    .Invokes(parm =>
                    {
                        Debug.WriteLine($"call faked AdsConnection.AddDeviceNotificationEx(variableName={parm.Arguments[0].ToString()}, userData={parm.Arguments[4].ToString()})");
                        lock (_userDataLock)
                        {
                            _userData = parm.Arguments[4] as Tuple<PlcCommand, DataExchange<CommandHandshakeStruct>>;


                            if (option != PlcCommandFakeOption.NoResponse)
                            {
                                var handshake = _userData.Item2.Data; // Return the blank strukture, this is like the finish command
                                handshake.SubTask = ResponseSubTask;

                                if (option == PlcCommandFakeOption.ResponseDelayedCancel)
                                {
                                    Task.Delay(200);
                                    handshake.Progress = 50;
                                    handshake.ResultCode = (ushort)CommandResultCode.Cancelled;
                                    handshake.Busy = true;
                                    handshake.Execute = false;
                                }
                                else
                                {
                                    if (option == PlcCommandFakeOption.ResponseDelayedFinished)
                                    {
                                        Task.Delay(200);
                                    }

                                    handshake.Progress = 100;
                                    handshake.ResultCode = ResponseStatusCode;
                                }

                                var eventArgs = new AdsNotificationExEventArgs(1, _userData, 80, handshake);

                                Debug.WriteLine("Raise faked AdsConnection.AdsNotificationEx");
                                _adsConnection.AdsNotificationEx += Raise.FreeForm<AdsNotificationExEventHandler>
                                    .With(_adsConnection, eventArgs);
                            }
                        }
                    })
                    .Returns(80);            
        }

        /// <summary>
        /// The value of the ResultCode. Works only with PlcCommandFakeOption.ResponseImmediatelyFinished. 
        /// Default is Done, but can be used for simulation of user specifc codes
        /// </summary>
        public ushort ResponseStatusCode { get; set; } = (ushort)CommandResultCode.Done;
        
        public ushort ResponseSubTask { get; set; } = 0;

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
        /// <param name="adsClient"></param>
        public static void SetSystemTestConnection(IAdsConnection adsConnection)
        {
            _systemTestAdsConnection = adsConnection;
        }

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
