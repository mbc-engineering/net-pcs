using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AtomizerUI.link
{
    public class PcsCommandErrorException : PcsCommandException
    {
        public PcsCommandErrorException(ushort resultCode, string message)
            : base(message)
        {
            ResultCode = resultCode;
        }

        public ushort ResultCode { get; private set; }
    }
}
