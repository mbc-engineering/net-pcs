using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AtomizerUI.link
{
    public class PcsCommandException : Exception
    {
        protected PcsCommandException()
        {
        }

        protected PcsCommandException(string message)
            : base(message)
        {
        }
    }
}
