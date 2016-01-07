using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Raft.Logs;
using Raft.States;
using Raft.Transports;

namespace Raft.Tests
{
    public abstract class TransportImpl : IDisposable
    {
        public abstract Server CreateServer();
        public abstract void Dispose();
    }

    public class MemoryTransportImpl : TransportImpl
    {
        public static int id;

        private MemoryTransport transport = new MemoryTransport();
        private List<Server> servers = new List<Server>();

        public override Server CreateServer()
        {
            var sid = ++id;
            var port = sid + 7000;
            var log = new MemoryLog();
            var server = new Server(sid, new IPEndPoint(IPAddress.Loopback, port), log, transport);
            transport.Start(server.ID);
            log.Initialize();
            server.ChangeState(new FollowerState(server));


            servers.Add(server);

            return server;
        }

        public override void Dispose()
        {
            foreach (var s in servers)
                s.Dispose();

            servers.Clear();
        }
    }

    public class UdpTransportImpl : TransportImpl
    {
        public static int id;

        //private MemoryTransport transport = new MemoryTransport();
        private List<Server> servers = new List<Server>();

        public override Server CreateServer()
        {
            var sid = ++id;
            var port = sid + 7000;
            var log = new MemoryLog();
            var transport = new UdpTransport();
            var server = new Server(sid, new IPEndPoint(IPAddress.Loopback, port), log, transport);
            transport.Start(server.ID);
            log.Initialize();
            server.ChangeState(new FollowerState(server));


            servers.Add(server);

            return server;
        }

        public override void Dispose()
        {
            foreach (var s in servers)
                s.Dispose();

            servers.Clear();
        }
    }

    public class TcpTransportImpl : TransportImpl
    {
        public static int id;

        //private MemoryTransport transport = new MemoryTransport();
        private List<Server> servers = new List<Server>();

        public override Server CreateServer()
        {
            var sid = ++id;
            var port = sid + 9000;
            var log = new MemoryLog();
            var transport = new TcpTransport();
            var server = new Server(sid, new IPEndPoint(IPAddress.Loopback, port), log, transport);
            transport.Start(server.ID);
            log.Initialize();
            server.ChangeState(new FollowerState(server));


            servers.Add(server);

            return server;
        }

        public override void Dispose()
        {
            foreach (var s in servers)
                s.Dispose();

            servers.Clear();
        }
    }

}
