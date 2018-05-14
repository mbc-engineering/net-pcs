using System.Collections.Generic;

namespace MbcAdcCommand
{
    public interface ICommandOutput
    {
        IEnumerable<string> GetOutputNames();

        void SetOutputData(string name, object value);
    }
}
