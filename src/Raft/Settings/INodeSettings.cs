using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Raft.Settings
{
    public interface INodeSettings : IDisposable
    {
        int ID { get; }

        int Port { get; }

        Stream OpenIndexFile();

        Stream OpenDataFile();
    }

}
