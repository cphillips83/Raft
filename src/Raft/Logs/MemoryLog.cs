using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Raft.Logs
{
    public class MemoryLog : Log
    {
        private MemoryStream _indexFile, _dataFile;

        public MemoryLog()
            : base()
        {

        }

        protected override Stream OpenIndexFileWriter()
        {
            if (_indexFile == null)
                _indexFile = new MemoryStream();

            return _indexFile;
        }

        protected override Stream OpenDataFileWriter()
        {
            if (_dataFile == null)
                _dataFile = new MemoryStream();

            return _dataFile;
        }

        protected override Stream OpenIndexFileReader()
        {
            throw new NotImplementedException();
        }

        protected override Stream OpenDataFileReader()
        {
            var data = new byte[_dataFile.Length];
            _dataFile.Seek(0, SeekOrigin.Begin);
            _dataFile.Read(data, 0, data.Length);
            return new MemoryStream(data);
        }
    }
}
