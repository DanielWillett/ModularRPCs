using DanielWillett.ModularRpcs.Abstractions;
using System;
using System.Globalization;
using System.IO;
using System.IO.Pipes;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;

namespace DanielWillett.ModularRpcs.NamedPipes;

/// <summary>
/// The remote side of a named pipe client-side connection using a <see cref="NamedPipeClientStream"/>.
/// </summary>
public sealed class NamedPipeClientsideRemoteRpcConnection
    : NamedPipeRemoteRpcConnection<NamedPipeClientsideLocalRpcConnection, NamedPipeClientStream>,
        IModularRpcClientsideConnection
{
    private readonly CancellationTokenSource _cts;
    private readonly bool _ownsCts;
    private int _isAutoReconnecting;

    internal NamedPipeClientsideRemoteRpcConnection(NamedPipeEndpoint endpoint, NamedPipeClientStream client, CancellationTokenSource cts, bool ownsCts = false)
        : base(endpoint)
    {
        _cts = cts;
        _ownsCts = ownsCts;
        PipeStream = client;
    }

    internal override void TryStartAutoReconnecting()
    {
        if (!Endpoint.ShouldAutoReconnect || Interlocked.Exchange(ref _isAutoReconnecting, 1) != 0)
            return;

        Task.Factory.StartNew(async () =>
        {
            try
            {
                double nextDelaySec = Endpoint.DelaySettings.CalculateNext();

                Local.LogDebug($"Attempting to reconnect in {nextDelaySec.ToString("0.#", CultureInfo.InvariantCulture)} seconds...");

                await Task.Delay((int)(nextDelaySec * 1000), _cts.Token).ConfigureAwait(false);

                while (!_cts.IsCancellationRequested && PipeStream == null)
                {
                    if (await TryReconnectIntl())
                        break;

                    nextDelaySec = Endpoint.DelaySettings.CalculateNext();
                    Local.LogDebug($"Attempting to reconnect in {nextDelaySec.ToString("0.#", CultureInfo.InvariantCulture)} seconds...");
                    await Task.Delay((int)(nextDelaySec * 1000), _cts.Token).ConfigureAwait(false);
                }
            }
            finally
            {
                _isAutoReconnecting = 0;
            }
        }, TaskCreationOptions.LongRunning);
    }

    private async Task<bool> TryReconnectIntl()
    {
        NamedPipeClientStream clientStream = new NamedPipeClientStream(
            Endpoint.ServerName,
            Endpoint.PipeName,
            PipeDirection.InOut,
            PipeOptions.Asynchronous,
            TokenImpersonationLevel.None,
            HandleInheritability.None
        );

        Local.LogDebug("Attempting to reconnect...");

        try
        {
            await clientStream.ConnectAsync(2500, _cts.Token).ConfigureAwait(false);
            if (!clientStream.IsConnected)
            {
                clientStream.Dispose();
            }
            else
            {
                _isAutoReconnecting = 0;
                NamedPipeClientStream? str = Interlocked.Exchange(ref PipeStream, clientStream);
                if (str != null)
                {
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
                    str.DisposeAsync().ConfigureAwait(false);
#else
                    str.Dispose();
#endif
                }

                Local.LogDebug("Successfully reconnected.");
                Local.StartListening();
            }
        }
        catch (TimeoutException)
        {
            clientStream.Dispose();
            return false;
        }
        catch (Exception ex)
        {
            Local.LogWarning(ex, Properties.Resources.LogWarningReconnectingToPipeStream);
        }

        return false;
    }

    private protected override void Dispose(bool disposing)
    {
        if (!disposing || !_ownsCts)
            return;

        try
        {
            _cts.Cancel();
        }
        catch { /* ignored */ }
        _cts.Dispose();
    }

    /// <inheritdoc />
    public override string ToString() => "Named Pipes (Remote, Client)";
}
