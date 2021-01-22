//-----------------------------------------------------------------------------
// Copyright (c) 2020 by mbc engineering, CH-6015 Luzern
// Licensed under the Apache License, Version 2.0
//-----------------------------------------------------------------------------

using System.Collections.Generic;

namespace Mbc.Pcs.Net.Command
{
    public interface ICommandOutput
    {
        IEnumerable<string> GetOutputNames();

        bool HasOutputName(string name);

        void SetOutputData<T>(string name, T value);

        T GetOutputData<T>(string name);
    }
}
