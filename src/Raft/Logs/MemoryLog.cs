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

        protected override Stream OpenIndexFile()
        {
            if (_indexFile == null)
                _indexFile = new MemoryStream();

            return _indexFile;
        }

        protected override Stream OpenDataFile()
        {
            if (_dataFile == null)
                _dataFile = new MemoryStream();

            return _dataFile;
        }
    }
}
