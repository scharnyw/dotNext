using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace DotNext.Net.Cluster.Consensus.Raft.TransportServices
{
    using IO.Log;
    using static Runtime.Intrinsics;

    internal partial class ServerExchange
    {
        private static readonly ILogEntryProducer<ReceivedLogEntry> EmptyProducer = new LogEntryProducer<ReceivedLogEntry>();

        private void BeginProcessHeartbeat(ReadOnlyMemory<byte> payload, EndPoint sender, CancellationToken token)
        {
            HeartbeatExchange.Parse(payload.Span, out var remotePort, out var term, out var prevLogIndex, out var prevLogTerm, out var commitIndex);
            ChangePort(ref sender, remotePort);
            task = server.ReceiveEntriesAsync(sender, term, EmptyProducer, prevLogIndex, prevLogTerm, commitIndex, token);
        }

        private async ValueTask<(PacketHeaders, int, bool)> EndProcessHearbeat(Memory<byte> output)
        {
            var result = await Cast<Task<Result<bool>>>(Interlocked.Exchange(ref task, null)).ConfigureAwait(false);
            return (new PacketHeaders(MessageType.Heartbeat, FlowControl.Ack), IExchange.WriteResult(result, output.Span), false);
        }
    }
}