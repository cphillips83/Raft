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
    [ServiceContract(SessionMode = SessionMode.NotAllowed)]
    public interface IDataService
    {
        [OperationContract]
        [WebInvoke(UriTemplate = "upload")]
        uint UploadFile(Stream stream);

        [OperationContract]
        [WebInvoke(UriTemplate = "download")]
        Stream DownloadFile(uint id);
    }

    public class UploadRequest : IDisposable
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
        private bool _isRunning = false;
        private Server _server;
        private Queue<UploadRequest> _uploads = new Queue<UploadRequest>();

        public Agent(Server server)
        {
            _server = server;
        }

        public void Run(IPEndPoint ip)
        {
            var uri = new Uri(string.Format("http://{0}/agent", ip));
            Console.WriteLine("{0}: Agent started on {1}", _server.ID, uri);


            var binding = new BasicHttpBinding();
            binding.TransferMode = TransferMode.Streamed;
            binding.MaxReceivedMessageSize = 1024 * 1024 * 25;

            var custom = new CustomBinding(binding);
            //custom.Elements.Find<HttpTransportBindingElement>().KeepAliveEnabled = false;

            var host = new ServiceHost(this, uri);
            //host
            host.AddServiceEndpoint(typeof(IDataService), custom, uri);
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
                        UploadRequest request;
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
                                    request.Dispose();
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

        public uint UploadFile(Stream stream)
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
                const int bufferLen = 65536;
                var buffer = new byte[bufferLen];
                int count = 0;
                while ((count = stream.Read(buffer, 0, bufferLen)) > 0)
                {
                    fs.Write(buffer, 0, count);
                }

                Console.WriteLine("{0}: Wrote {1} bytes to file '{2}'", _server.ID, fs.Length, tempFile);
            }

            var upload = new UploadRequest()
            {
                Completed = new ManualResetEvent(false),
                FilePath = tempFile
            };

            lock (_uploads)
            {
                _uploads.Enqueue(upload);
            }

            upload.Completed.WaitOne();
            return upload.Index;
        }

        public Stream DownloadFile(uint index)
        {
            Console.WriteLine("{0}: Download request for {1}", _server.ID, index);

            //if commitindex > id, data is committed
            //safe to return a RemoteStream to offset + length
            //get a free filestream reader to return
            var logIndex = _server.PersistedStore[index];
            if (logIndex.Term == 0)
                return Stream.Null;

            var stream = _server.PersistedStore.GetDataStream();
            return new RemoteStreamReader(stream, (int)logIndex.Offset, (int)logIndex.Size);
        }

        public class RemoteStreamReader : Stream
        {
            private int _remaining;
            private int _offset, _length;
            private Stream _internalStream;
            public RemoteStreamReader(Stream internalStream, int offset, int length)
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
            BasicHttpBinding binding = new BasicHttpBinding();
            binding.MaxReceivedMessageSize = 1024 * 1024 * 25;
            //Get the address of the service from configuration or some other mechanism - Not shown here
            EndpointAddress address = new EndpointAddress(uri);

            var factory3 = new ChannelFactory<T>(binding, address);
            return factory3.CreateChannel();
        }
    }
}
