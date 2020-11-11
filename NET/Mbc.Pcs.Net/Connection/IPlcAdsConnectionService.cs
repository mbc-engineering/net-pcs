//-----------------------------------------------------------------------------
// Copyright (c) 2020 by mbc engineering GmbH, CH-6015 Luzern
// Licensed under the Apache License, Version 2.0
//-----------------------------------------------------------------------------

using System;
using TwinCAT.Ads;

namespace Mbc.Pcs.Net.Connection
{
    /// <summary>
    /// A service which provides access to an <see cref="IAdsConnection"/>.
    /// </summary>
    public interface IPlcAdsConnectionService
    {
        event EventHandler<PlcConnectionChangeArgs> ConnectionStateChanged;

        /// <summary>
        /// Ergibt true, falls eine Verbindung zur PLC besteht, sonst false.
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// Liefert die Verbindung zur ADS-Schnittstelle zurück.
        /// </summary>
        /// <remarks>
        /// Löst eine Exception aus, wenn noch keine Verbindung existiert.
        /// (was vor dem Start des Dienstes immer der Fall ist).
        /// </remarks>
        /// <exception cref="PlcAdsException">wenn keine Verbindung besteht</exception>
        IAdsConnection Connection { get; }
    }
}
