using System.Collections.Generic;

namespace Mbc.Pcs.Net
{
    public interface ICommandOutput
    {
        IEnumerable<string> GetOutputNames();

        void SetOutputData(string name, object value);
    }
}
