using Mbc.Pcs.Net.State;
using System;
using System.Collections.Generic;

namespace Mbc.Pcs.Net.Test.TestUtils
{
    public abstract class PlcStateSamplerMock : IPlcStateSampler<TestState>
    {
        public PlcStateSamplerMock()
        {
        }

        public abstract uint SampleRate { get; }

        public abstract TestState CurrentSample { get; }

#pragma warning disable 67
        public event EventHandler<PlcStateChangedEventArgs<TestState>> StateChangedActual;

#pragma warning disable 67
        public event EventHandler<PlcStateChangedEventArgs<TestState>> StateChanged;

        public void OnStateChange(TestState testState, DateTime plcTimeStamp)
        {
            testState.PlcTimeStamp = plcTimeStamp;
            StatesChangedActual(this, new PlcMultiStateChangedEventArgs<TestState>(new List<TestState> { { testState } }));
        }

        public abstract void StatesChangedHandlerAdded(EventHandler<PlcMultiStateChangedEventArgs<TestState>> handler);

        public abstract void StatesChangedHandlerRemoved(EventHandler<PlcMultiStateChangedEventArgs<TestState>> handler);

        public event EventHandler<PlcMultiStateChangedEventArgs<TestState>> StatesChangedActual;

        public event EventHandler<PlcMultiStateChangedEventArgs<TestState>> StatesChanged
        {
            add
            {
                StatesChangedActual += value;
                StatesChangedHandlerAdded(value);
            }
            remove
            {
                StatesChangedHandlerRemoved(value);
                StatesChangedActual -= value;
            }
        }
    }
}
