using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Raft.Logs
{
    public class FileLog : Log
    {
        private string _dataDir, _indexFilePath, _dataFilePath;
        private FileStream _indexFile, _dataFile;

        public FileLog(string dataDir)
            : base()
        {
            if (!System.IO.Directory.Exists(_dataDir))
                System.IO.Directory.CreateDirectory(_dataDir);

            _indexFilePath = System.IO.Path.Combine(dataDir, "index");
            _dataFilePath = System.IO.Path.Combine(dataDir, "data");
        }

        protected override System.IO.Stream OpenIndexFile()
        {
            System.Diagnostics.Debug.Assert(_indexFile == null);

            _indexFile = File.Open(_indexFilePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            return _indexFile;
        }

        protected override System.IO.Stream OpenDataFile()
        {
            System.Diagnostics.Debug.Assert(_dataFile == null);

            _dataFile = File.Open(_dataFilePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            return _dataFile;
        }
    }
}
