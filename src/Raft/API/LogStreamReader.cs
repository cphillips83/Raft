using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Raft.API
{
    public class LogStreamReader : Stream
    {
        private int _remaining;
        private int _offset, _length;
        private Stream _internalStream;
        public LogStreamReader(Stream internalStream, int offset, int length)
        {
            _offset = offset;
            _length = length;
            _remaining = length;
            _internalStream = internalStream;
            _internalStream.Seek(offset, SeekOrigin.Begin);
        }

        public override bool CanRead
        {
            get { return true; }
        }

        public override bool CanSeek
        {
            get { throw new NotImplementedException(); }
        }

        public override bool CanWrite
        {
            get { throw new NotImplementedException(); }
        }

        public override void Flush()
        {
            throw new NotImplementedException();
        }

        public override long Length
        {
            get { return _length; }
        }

        public override long Position
        {
            get
            {
                return _internalStream.Position - _offset;
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (_remaining <= 0)
                return 0;

            if (_remaining < count)
                count = _remaining;

            _remaining -= count;
            _internalStream.Read(buffer, offset, count);
            return count;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotImplementedException();
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            //we'd recycle the filestream to the log manager here
            _internalStream = null;
        }


    }

}
