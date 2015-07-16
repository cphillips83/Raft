using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace Raft.API
{
    [MessageContract]
    public class FileIndex
    {
        [MessageBodyMember(Order = 1)]
        public uint Index { get; set; }

        public override string ToString()
        {
            return Index.ToString();
        }
    }
}
