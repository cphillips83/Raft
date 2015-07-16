using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace Raft.API
{
    [MessageContract]
    public class RemoteStream : IDisposable
    {
        [MessageBodyMember(Order = 1)]
        public Stream Stream { get; set; }

        public void Dispose()
        {
            if (Stream != null)
                Stream.Dispose();

            Stream = null;
        }
    }
}
