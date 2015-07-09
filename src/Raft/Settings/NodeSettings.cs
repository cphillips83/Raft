using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Raft.Settings
{
    public class NodeSettings : INodeSettings
    {
        private int _id, _port;
        private string _dataDir, _indexFilePath, _dataFilePath;

        private FileStream _indexFile, _dataFile;

        public int ID { get { return _id; } }

        public int Port { get { return _port; } }

        public NodeSettings(int serverID, int port, string dataDir)
        {
            _id = serverID;
            _port = port;
            _dataDir = dataDir;

            if (!System.IO.Directory.Exists(_dataDir))
                System.IO.Directory.CreateDirectory(_dataDir);

            _indexFilePath = System.IO.Path.Combine(dataDir, "index");
            _dataFilePath = System.IO.Path.Combine(dataDir, "data");
        }

        public Stream OpenIndexFile()
        {
            System.Diagnostics.Debug.Assert(_indexFile == null);

            _indexFile = File.Open(_indexFilePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            return _indexFile;
        }

        public Stream OpenDataFile()
        {
            System.Diagnostics.Debug.Assert(_dataFile == null);

            _dataFile = File.Open(_dataFilePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
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
