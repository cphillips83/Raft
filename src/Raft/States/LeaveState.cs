//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using Raft.Messages;

//namespace Raft.States
//{
//    public class LeaveState : FollowerState
//    {
//        public LeaveState(Server server)
//            : base(server)
//        {
//            _bootstrap = bootstrap;
//        }

//        public override void Enter()
//        {
//            base.Enter();
//            HeartbeatTimeout();
//        }

//        protected override void HeartbeatTimeout()
//        {
//            _bootstrap.SendAddServerRequest();
//        }

//        protected override bool VoteReply(Client client, VoteReply reply)
//        {
//            return true;
//        }

//        protected override bool AddServerRequest(Client client, AddServerRequest request)
//        {
//            return true;
//        }

//        protected override bool AppendEntriesReply(Client client, AppendEntriesReply reply)
//        {
//            return true;
//        }
//    }
//}
