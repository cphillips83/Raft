using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Raft.Messages;

namespace Raft.States
{
    public abstract class AbstractState
    {
        protected Server _server;
        protected AbstractState(Server server)
        {
            _server = server;
        }

        public virtual bool StepDown(uint term)
        {
            if (_server.PersistedStore.Term < term)
            {
                _server.PersistedStore.UpdateState(term, null);
                _server.ChangeState(new FollowerState(_server));
                return true;
            }

            return false;
        }

        public virtual void Enter() { }
        public virtual void Update() { }
        public virtual void Exit() { }

        public virtual void CommittedAddServer(IPEndPoint endPoint)
        {

        }

        public virtual void CommittedRemoveServer(IPEndPoint endPoint) { 

}

        public bool VoteRequest(VoteRequest request)
        {
            var client = _server.GetClient(request.From);
            return VoteRequest(client, request);
        }

        public bool VoteReply(VoteReply reply)
        {
            var client = _server.GetClient(reply.From);
            return VoteReply(client, reply);
        }

        public bool AppendEntriesRequest(AppendEntriesRequest request)
        {
            var client = _server.GetClient(request.From);
            return AppendEntriesRequest(client, request);
        }

        public bool AppendEntriesReply(AppendEntriesReply reply)
        {
            var client = _server.GetClient(reply.From);
            return AppendEntriesReply(client, reply);
        }

        public bool AddServerRequest(AddServerRequest request)
        {
            var client = _server.GetClient(request.From);
            return AddServerRequest(client, request);
        }

        public bool AddServerReply(AddServerReply reply)
        {
            if (reply.Status == AddServerStatus.NotLeader && reply.LeaderHint == null)
            {
                return AddServerReply(null, reply);
            }
            else
            {
                var client = new Client(_server, reply.LeaderHint);
                return AddServerReply(client, reply);
            }
        }

        public bool RemoveServerRequest(RemoveServerRequest request)
        {
            var client = _server.GetClient(request.From);
            return RemoveServerRequest(client, request);
        }

        public bool RemoveServerReply(RemoveServerReply reply)
        {
            if (reply.Status == RemoveServerStatus.NotLeader && reply.LeaderHint == null)
            {
                return RemoveServerReply(null, reply);
            }
            else
            {
                var client = new Client(_server, reply.LeaderHint);
                return RemoveServerReply(client, reply);
            }
        }
        
        protected abstract bool VoteRequest(Client client, VoteRequest request);
        protected abstract bool VoteReply(Client client, VoteReply reply);

        protected abstract bool AppendEntriesRequest(Client client, AppendEntriesRequest request);
        protected abstract bool AppendEntriesReply(Client client, AppendEntriesReply reply);

        protected virtual bool AddServerRequest(Client client, AddServerRequest request)
        {
            client.SendAddServerReply(AddServerStatus.NotLeader, null);
            return true;
        }

        protected virtual bool AddServerReply(Client client, AddServerReply reply)
        {
            return true;
        }

        protected virtual bool RemoveServerRequest(Client client, RemoveServerRequest request)
        {
            client.SendRemoveServerReply(RemoveServerStatus.NotLeader, null);
            return true;
        }

        protected virtual bool RemoveServerReply(Client client, RemoveServerReply reply)
        {
            return true;
        }

    }

}
