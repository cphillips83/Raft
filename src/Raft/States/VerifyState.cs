using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Raft.Logs;
using Raft.Messages;

namespace Raft.States
{
    public class VerifyState : AbstractState
    {
        private uint index = 0;
        protected Client targetClient;
        public bool IsVerified = false;

        public VerifyState(uint _index, Client _targetClient, Server _server) : base(_server)
        {
            index = _index + 1;
            targetClient = _targetClient;
        }

        public override void Enter()
        {
            base.Enter();
            targetClient.SendEntryRequest(index);
        }

        protected override bool EntryRequestReply(Client client, EntryRequestReply reply)
        {
            var entry = _server.PersistedStore.GetEntry(index);

            if (entry.HasValue && !reply.Entry.HasValue)
            {
                //we have more logs
                Console.WriteLine("{0}: Verify failed at index {1} - we have more logs", _server.Name, index);
                Console.WriteLine("{0}: Expected {1}", _server.Name, _server.PersistedStore.GetEntry(index));
                Console.WriteLine("{0}: Got      {1}", _server.Name, reply.Entry);
            }
            else if (!entry.HasValue && reply.Entry.HasValue)
            {
                //they have more logs
                Console.WriteLine("{0}: Verify failed at index {1} - they have more logs", _server.Name, index);
                Console.WriteLine("{0}: Expected {1}", _server.Name, _server.PersistedStore.GetEntry(index));
                Console.WriteLine("{0}: Got      {1}", _server.Name, reply.Entry);
            }
            else if (!entry.HasValue && !entry.HasValue)
            {
                //logs matched
                Console.WriteLine("{0}: logs matched, finished at {1}", _server.Name, index - 2);
                IsVerified = true;
            }
            else
            {
                //both have a log, lets compare
                var left = entry.Value;
                var right = reply.Entry.Value;

                if (LogEntry.AreEqual(left, right))
                {
                    //Console.WriteLine("{0}: Verified index {1}", _server.Name, index);
                    client.SendEntryRequest(++index);
                    return true;
                }
                else
                {
                    Console.WriteLine("{0}: Verify failed at index {1}", _server.Name, index);
                    Console.WriteLine("{0}: Expected {1}", _server.Name, _server.PersistedStore.GetEntry(index));
                    Console.WriteLine("{0}: Got      {1}", _server.Name, reply.Entry);
                }
            }
            _server.ChangeState(new StoppedState(_server));
            return base.EntryRequestReply(client, reply);
        }



        protected override bool VoteReply(Client client, VoteReply reply)
        {
            return true;
        }

        protected override bool VoteRequest(Client client, VoteRequest request)
        {
            return true;
        }

        protected override bool AppendEntriesRequest(Client client, AppendEntriesRequest request)
        {
            return true;
        }

        protected override bool AppendEntriesReply(Client client, AppendEntriesReply reply)
        {
            return true;
        }
    }
}
