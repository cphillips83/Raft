using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Raft.Settings
{
    public class TestNodeSettings : INodeSettings
    {
        private int _id, _port;

        private MemoryStream _indexFile, _dataFile;

        public int ID { get { return _id; } }

        public int Port { get { return _port; } }

        public TestNodeSettings(int serverID, int port)
        {
            _id = serverID;
            _port = port;
        }

        public Stream OpenIndexFile()
        {
            System.Diagnostics.Debug.Assert(_indexFile == null);

            _indexFile = new MemoryStream();
            return _indexFile;
        }

        public Stream OpenDataFile()
        {
            System.Diagnostics.Debug.Assert(_dataFile == null);

            _dataFile = new MemoryStream();
            return _dataFile;
        }

        public void Dispose()
        {
            if (_indexFile != null)
                _indexFile.Dispose();

            if (_dataFile != null)
                _dataFile.Dispose();

            _indexFile = null;
            _dataFile = null;
        }
    }

}
