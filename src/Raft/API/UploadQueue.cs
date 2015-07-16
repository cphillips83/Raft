using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Raft.API
{
    public class UploadQueue : IDisposable
    {
        public string FilePath;
        public uint Index;
        public long Timeout;
        //public LogIndex Index;
        public ManualResetEvent Completed;
        public void Dispose()
        {
            try
            {
                //Console.WriteLine("Deleting file: {0}", FilePath);
                //try to delete the temp file to keep disk usage low
                System.IO.File.Delete(FilePath);
            }
            catch { }

            Completed = null;
        }

    }
}
