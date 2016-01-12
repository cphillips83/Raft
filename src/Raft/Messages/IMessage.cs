using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Raft.Messages
{
    public interface IMessage
    {
        void Write(BinaryWriter msg);
        void Read(BinaryReader msg);
        //void HandleMessageAsync
    }
}
