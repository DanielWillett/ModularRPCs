using System;
using System.IO;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using DanielWillett.ModularRpcs.Connections;

namespace DanielWillett.ModularRpcs.WebSockets;

partial class WebSocketClientConnection
{
    /// <inheritdoc />
    public IDisposable ReportProgress(IProgress<DownloadProgressReport> progressSink)
    {

    }

    /// <inheritdoc />
    ListenerState IListener.State { get; set; }
}
