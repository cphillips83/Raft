using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Web;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Raft.Logs;

namespace Raft
{
    [MessageContract]
    public class RemoteStream : IDisposable
    {
        [MessageBodyMember(Order = 1)]
        public Stream Stream { get; set; }

        public void Dispose()
        {
            if(Stream != null)
                Stream.Dispose();

            Stream = null;
        }
    }

    [MessageContract]
    public class FileIndex
    {
         [MessageBodyMember(Order = 1)]
         public uint Index { get; set; }

         public override string ToString()
         {
             return Index.ToString();
         }
    }

    [ServiceContract(SessionMode = SessionMode.NotAllowed)]
    public interface IDataService
    {
        [OperationContract]
        [WebInvoke(UriTemplate = "upload")]
        FileIndex UploadFile(RemoteStream stream);

        [OperationContract]
        [WebInvoke(UriTemplate = "download")]
        RemoteStream DownloadFile(FileIndex index);
    }

    public class UploadQueue : IDisposable
    {
        public string FilePath;
        public uint Index;
        //public LogIndex Index;
        public ManualResetEvent Completed;
        public void Dispose()
        {
            try
            {
                Console.WriteLine("Deleting file: {0}", FilePath);
                //try to delete the temp file to keep disk usage low
                System.IO.File.Delete(FilePath);
            }
            catch { }

            Completed = null;
        }

        //public void Process(Server server)
        //{
        //    try
        //    {
        //        using (var fs = new FileStream(FilePath, FileMode.Open, FileAccess.Read))
        //        {
        //            var data = new byte[fs.Length];
        //            fs.Read(data, 0, data.Length);
        //            Index = server.PersistedStore.Length + 1;
        //            server.PersistedStore.Create(server, data);
        //        }
        //    }
        //    catch
        //    {
        //        Index = 0;
        //    }
        //    Completed.Set();
        //    Dispose();
        //}
    }

    [ServiceBehavior(ConcurrencyMode = ConcurrencyMode.Multiple,
                        InstanceContextMode = InstanceContextMode.Single)]
    public class Agent : IDataService
    {
        public const int MESSAGE_BUFFER_LENGTH = 1024 * 128;
        private bool _isRunning = false;
        private Server _server;
        private Queue<UploadQueue> _uploads = new Queue<UploadQueue>();

        public Agent(Server server)
        {
            _server = server;
        }

        public void Run(IPEndPoint ip)
        {
            var uri = new Uri(string.Format("http://{0}/agent", ip));
            Console.WriteLine("{0}: Agent started on {1}", _server.ID, uri);


            var binding = CreateDefaultBinding();

            var host = new ServiceHost(this, uri);
            //host
            host.AddServiceEndpoint(typeof(IDataService), binding, uri);
            host.Description.Behaviors.Add(new ServiceMetadataBehavior()
            {
                HttpGetEnabled = true,
                MetadataExporter = { PolicyVersion = PolicyVersion.Policy15 },
            });
            host.Description.Behaviors.Add(new ServiceThrottlingBehavior()
            {
                MaxConcurrentCalls = 100,
                MaxConcurrentInstances = 1,
                MaxConcurrentSessions = int.MaxValue
            });
            host.Open();


            _isRunning = true;
            var timer = Stopwatch.StartNew();
            var lastTick = 0L;
            while (_isRunning)
            {
                var sleep = true;
                var currentTick = timer.ElapsedMilliseconds;
                while (lastTick < currentTick)
                {
                    if (_uploads.Count > 0)
                    {
                        UploadQueue request;
                        lock (_uploads)
                        {
                            request = _uploads.Peek();

                            //waiting for the commit
                            if (request.Index > 0)
                            {
                                if (_server.CommitIndex >= request.Index)
                                {
                                    _uploads.Dequeue();
                                    request.Completed.Set();
                                    sleep = false;
                                }

                                request = null;
                            }
                        }

                        if (request != null)
                        {
                            using (var fs = new FileStream(request.FilePath, FileMode.Open, FileAccess.Read))
                            {
                                var data = new byte[fs.Length];
                                fs.Read(data, 0, data.Length);
                                request.Index = _server.PersistedStore.Create(_server, data);
                            }
                            sleep = false;
                        }
                    }

                    _server.Advance();
                    lastTick++;
                }

                if (sleep)
                    System.Threading.Thread.Sleep(5);

            }

            host.Close();

        }

        public FileIndex UploadFile(RemoteStream remoteStream)
        {
            //reply not leader with leader hint?
            //write data to temp file
            //create waithandle + filename and add to server for processing
            //  server processes file and waits for it to be committed
            //  server triggers waithandle as done with file id
            //wait for waithandle to complete
            //return fileid back to client

            Console.WriteLine("{0}: Upload request", _server.ID);
            //store the file locally so we can process it faster and keep the server
            //running faster
            var tempFile = System.IO.Path.GetTempFileName();
            using (var fs = new FileStream(tempFile, FileMode.Append, FileAccess.Write, FileShare.None))
            {
                var buffer = new byte[MESSAGE_BUFFER_LENGTH];
                int count = 0;
                while ((count = remoteStream.Stream.Read(buffer, 0, MESSAGE_BUFFER_LENGTH)) > 0)
                {
                    fs.Write(buffer, 0, count);
                }

                Console.WriteLine("{0}: Wrote {1} bytes to file '{2}'", _server.ID, fs.Length, tempFile);
            }

            var upload = new UploadQueue()
            {
                Completed = new ManualResetEvent(false),
                FilePath = tempFile
            };

            lock (_uploads)
            {
                _uploads.Enqueue(upload);
            }

            upload.Completed.WaitOne();
            upload.Dispose();
            return new FileIndex() { Index = upload.Index };
        }

        public RemoteStream DownloadFile(FileIndex index)
        {
            Console.WriteLine("{0}: Download request for {1}", _server.ID, index);

            //if commitindex > id, data is committed
            //safe to return a RemoteStream to offset + length
            //get a free filestream reader to return
            var logIndex = _server.PersistedStore[index.Index];
            if (logIndex.Term == 0)
                return new RemoteStream() { Stream = Stream.Null };

            var stream = _server.PersistedStore.GetDataStream();
            return new RemoteStream() { Stream = new LogStreamReader(stream, (int)logIndex.Offset, (int)logIndex.Size) };
        }

        public static BasicHttpBinding CreateDefaultBinding()
        {
            BasicHttpBinding binding = new BasicHttpBinding();
            binding.TransferMode = TransferMode.Streamed;
            binding.MaxReceivedMessageSize = 1024 * 1024 * 25;
            binding.MaxBufferSize = Agent.MESSAGE_BUFFER_LENGTH;
            binding.MessageEncoding = WSMessageEncoding.Mtom;
            binding.CloseTimeout = TimeSpan.FromMinutes(1);
            binding.OpenTimeout = TimeSpan.FromMinutes(1);
            binding.ReceiveTimeout = TimeSpan.FromMinutes(1);
            binding.SendTimeout = TimeSpan.FromMinutes(1);
            return binding;
        }
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

    //Factory class for client proxy
    public abstract class ClientFactory
    {
        public static T CreateClient<T>(IPEndPoint ip)
        {
            var uri = new Uri(string.Format("http://{0}/agent", ip));
            var binding = Agent.CreateDefaultBinding();

            //Get the address of the service from configuration or some other mechanism - Not shown here
            EndpointAddress address = new EndpointAddress(uri);

            var factory3 = new ChannelFactory<T>(binding, address);
            return factory3.CreateChannel();
        }
    }
}
