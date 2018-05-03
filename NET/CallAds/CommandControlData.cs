using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace AtomizerUI.link
{
    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    struct CommandControlData
    {
        [MarshalAs(UnmanagedType.I1)]
        public bool Execute;
        [MarshalAs(UnmanagedType.I1)]
        public bool Busy;
        public ushort ResultCode;
    }
}
