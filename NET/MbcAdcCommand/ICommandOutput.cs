using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MbcAdcCommand
{
    public interface ICommandOutput
    {
        IEnumerable<string> GetOutputNames();

        void SetOutputData(string name, object value);
    }
}
