using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Raft.States;

namespace Raft.API
{
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

            _server.AgentIP = ip;

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

                            var state = _server.CurrentState as FollowerState;
                            var agentIP = (state != null && state.CurrentLeader != null && state.CurrentLeader.AgentIP != null) ? state.CurrentLeader.AgentIP : null;

                            //timeout the upload or a follower
                            if (request.Timeout <= _server.Tick || agentIP != null)
                            {
                                _uploads.Dequeue();
                                request.Index = 0;
                                request.Completed.Set();
                                sleep = false;
                                request = null;
                            }
                            //waiting for the commit
                            else if (request.Index > 0)
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

                        if (request != null && _server.CurrentState is LeaderState)
                        {
                            using (var fs = new FileStream(request.FilePath, FileMode.Open, FileAccess.Read))
                            {
                                var data = new byte[fs.Length];
                                fs.Read(data, 0, data.Length);
                                request.Index = _server.PersistedStore.CreateData(_server, data);
                            }
                            sleep = false;
                        }
                    }

                    _server.Advance();
                    lastTick++;
                }

                //if (sleep && !_server.Clients.Any(x => x.WaitingForResponse))
                //    System.Threading.Thread.Sleep(5);
                //else
                System.Threading.Thread.Sleep(1);

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

            //Console.WriteLine("{0}: Upload request", _server.ID);
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

                //Console.WriteLine("{0}: Wrote {1} bytes to file '{2}'", _server.ID, fs.Length, tempFile);
            }

            var upload = new UploadQueue()
            {
                Completed = new ManualResetEvent(false),
                FilePath = tempFile,
                Timeout = _server.Tick + 60000 //1 minute timeout
            };

            lock (_uploads)
            {
                _uploads.Enqueue(upload);
            }

            upload.Completed.WaitOne();

            var state = _server.CurrentState as FollowerState;
            if (upload.Index == 0 || state != null)
            {
                if (state.CurrentLeader != null && state.CurrentLeader.AgentIP != null)
                {
                    //forward the request
                    Console.WriteLine("{0}: Forwarding upload to {1}", _server.ID, state.CurrentLeader.AgentIP);
                    var proxy = CreateClient<IDataService>(state.CurrentLeader.AgentIP);
                    using (var fs = new FileStream(upload.FilePath, FileMode.Open, FileAccess.Read))
                        return proxy.UploadFile(new RemoteStream() { Stream = fs });
                }
                else
                {
                    //we don't know who the leader is!
                    Console.WriteLine("{0}: Upload failed! We don't know who the leader is", _server.ID);
                    return new FileIndex() { Index = 0 };
                }
            }

            upload.Dispose();
            return new FileIndex() { Index = upload.Index };
        }

        public RemoteStream DownloadFile(FileIndex index)
        {
            Console.WriteLine("{0}: Download request for {1}", _server.ID, index);

            //we may not have it yet
            if (index.Index > _server.CommitIndex)
            {

                //check if we are a follower, if so we want to forward to leader
                var state = _server.CurrentState as FollowerState;
                if (state != null)
                {
                    if (state.CurrentLeader != null && state.CurrentLeader.AgentIP != null)
                    {
                        //forward the request
                        Console.WriteLine("{0}: Forwarding download to {1}", _server.ID, state.CurrentLeader.AgentIP);
                        var proxy = CreateClient<IDataService>(state.CurrentLeader.AgentIP);
                        return proxy.DownloadFile(index);
                    }
                    else
                    {
                        //we don't know who the leader is!
                        Console.WriteLine("{0}: Download failed! We don't know who the leader is", _server.ID);
                        return new RemoteStream() { Stream = Stream.Null };
                    }
                }
                Console.WriteLine("{0}: Download failed! We don't have this index {1}", _server.ID, index.Index);
                return new RemoteStream() { Stream = Stream.Null };
            }


            //if commitindex > id, data is committed
            //safe to return a RemoteStream to offset + length
            //get a free filestream reader to return
            var logIndex = _server.PersistedStore[index.Index];
            if (logIndex.Term == 0)
                return new RemoteStream() { Stream = Stream.Null };

            var stream = _server.PersistedStore.GetDataStream();
            return new RemoteStream()
            {
                Stream = new LogStreamReader(stream, (int)logIndex.Flag3, (int)logIndex.Flag4)
            };
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
