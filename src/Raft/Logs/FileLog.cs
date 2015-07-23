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
        private FileStream _indexFileWriter, _dataFileWriter;

        public FileLog(string dataDir)
            : base()
        {
            _dataDir = dataDir;
            if (!System.IO.Directory.Exists(_dataDir))
                System.IO.Directory.CreateDirectory(_dataDir);

            _indexFilePath = System.IO.Path.Combine(dataDir, "index");
            _dataFilePath = System.IO.Path.Combine(dataDir, "data");
        }

        protected override System.IO.Stream OpenIndexFileWriter()
        {
            System.Diagnostics.Debug.Assert(_indexFileWriter == null);

            //if (!_init && !System.IO.File.Exists(_indexFilePath))
            //    throw new Exception(string.Format("Index file: '{0}' is missing.", _indexFilePath));

            _indexFileWriter = File.Open(_indexFilePath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read);
            return _indexFileWriter;
        }

        protected override System.IO.Stream OpenDataFileWriter()
        {
            System.Diagnostics.Debug.Assert(_dataFileWriter == null);

            //if (!_init && !System.IO.File.Exists(_dataFilePath))
            //    throw new Exception(string.Format("Data file: '{0}' is missing.", _dataFilePath));

            _dataFileWriter = File.Open(_dataFilePath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read);
            return _dataFileWriter;
        }

        protected override Stream OpenIndexFileReader()
        {
            return File.Open(_indexFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        }

        protected override Stream OpenDataFileReader()
        {
            return File.Open(_dataFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        }


        //public static FileLog 
    }
}
