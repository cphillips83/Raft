using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
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
        long UploadFile(Stream stream);

        [OperationContract]
        Stream DownloadFile(long id);

        void Tick();
    }

    public class UploadRequest : IDisposable
    {
        public string FilePath;
        public long Index;
        //public LogIndex Index;
        public ManualResetEvent Completed;
        public void Dispose()
        {
            try
            {
                //try to delete the temp file to keep disk usage low
                System.IO.File.Delete(FilePath);
            }
            catch { }

            Completed = null;
        }

        public void Process(Server server)
        {
            try
            {
                using (var fs = new FileStream(FilePath, FileMode.Open, FileAccess.Read))
                {
                    var data = new byte[fs.Length];
                    fs.Read(data, 0, data.Length);
                    Index = server.PersistedStore.Length;
                    server.PersistedStore.Create(server, data);
                }
            }
            catch
            {
                Index = -1;
            }
            Completed.Set();
            Dispose();
        }
    }

    public class DownloadRequest : IDisposable
    {
        public long Index;
        public ManualResetEvent Completed;
        public Stream ReadStream;

        public void Process(Server server)
        {
            //server.PersistedStore.GetData
        }

        public void Dispose()
        {

        }
    }

    [ServiceBehavior(ConcurrencyMode = ConcurrencyMode.Multiple,
                        InstanceContextMode = InstanceContextMode.Single)]
    public class Agent : IDataService
    {
        private Server _server;
        private Queue<UploadRequest> _uploads = new Queue<UploadRequest>();
        //do same for reads

        public Agent(Server server)
        {
            _server = server;
        }

        public void Tick() { }

        public long UploadFile(Stream stream)
        {
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
            }



            //reply not leader with leader hint?
            //write data to temp file
            //create waithandle + filename and add to server for processing
            //  server processes file and waits for it to be committed
            //  server triggers waithandle as done with file id
            //wait for waithandle to complete
            //return fileid back to client
            throw new NotImplementedException();
        }

        public Stream DownloadFile(long id)
        {
            //if commitindex > id, data is committed
            //safe to return a RemoteStream to offset + length
            //get a free filestream reader to return

            throw new NotImplementedException();
        }
    }

    //Factory class for client proxy
    public abstract class ClientFactory
    {
        public static IDataService CreateClient(Type targetType)
        {
            BasicHttpBinding binding = new BasicHttpBinding();
            //Get the address of the service from configuration or some other mechanism - Not shown here
            EndpointAddress address = new EndpointAddress("http://localhost:7741/CategoryServiceHost.svc");

            var factory3 = new ChannelFactory<IDataService>(binding, address);
            return factory3.CreateChannel();
        }

        public void test()
        {
            //var binding = new BasicHttpBinding();
            //var custom = new CustomBinding(binding);
            //custom.Elements.Find<HttpTransportBindingElement>().KeepAliveEnabled = false;

            //var host = new ServiceHost(this, endpoint);

            //host.AddServiceEndpoint(typeof(IDataService), custom, endpoint);

            //host.Description.Behaviors.Add(new ServiceMetadataBehavior()
            //{
            //    HttpGetEnabled = true,
            //    MetadataExporter = { PolicyVersion = PolicyVersion.Policy15 },
            //});

            //host.Open();
        }
    }
}
