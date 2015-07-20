using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Raft
{

    //////An AsyncResult that completes as soon as it is instantiated.
    ////internal class CompletedAsyncResult : AsyncResult
    ////{
    ////    public CompletedAsyncResult(AsyncCallback callback, object state)
    ////        : base(callback, state)
    ////    {
    ////        Complete(true);
    ////    }

    ////    public static void End(IAsyncResult result)
    ////    {
    ////        AsyncResult.End<CompletedAsyncResult>(result);
    ////    }
    ////}

    ////internal class CompletedAsyncResult<T> : AsyncResult
    ////{
    ////    private T _data;

    ////    public CompletedAsyncResult(T data, AsyncCallback callback, object state)
    ////        : base(callback, state)
    ////    {
    ////        _data = data;
    ////        Complete(true);
    ////    }

    ////    public static T End(IAsyncResult result)
    ////    {
    ////        CompletedAsyncResult<T> completedResult = AsyncResult.End<CompletedAsyncResult<T>>(result);
    ////        return completedResult._data;
    ////    }
    ////}

    ////internal class CompletedAsyncResult<TResult, TParameter> : AsyncResult
    ////{
    ////    private TResult _resultData;
    ////    private TParameter _parameter;

    ////    public CompletedAsyncResult(TResult resultData, TParameter parameter, AsyncCallback callback, object state)
    ////        : base(callback, state)
    ////    {
    ////        _resultData = resultData;
    ////        _parameter = parameter;
    ////        Complete(true);
    ////    }

    ////    public static TResult End(IAsyncResult result, out TParameter parameter)
    ////    {
    ////        CompletedAsyncResult<TResult, TParameter> completedResult = AsyncResult.End<CompletedAsyncResult<TResult, TParameter>>(result);
    ////        parameter = completedResult._parameter;
    ////        return completedResult._resultData;
    ////    }
    ////}
    //[ServiceContract(SessionMode = SessionMode.NotAllowed)]
    //public interface INodeProxy
    //{
    //    [OperationContractAttribute(IsOneWay = true, Action = "VoteRequest")]
    //    void VoteRequest(VoteRequest request);

    //    [OperationContractAttribute(IsOneWay = true, Action = "VoteReply")]
    //    void VoteReply(VoteReply reply);

    //    [OperationContractAttribute(IsOneWay = true, Action = "AppendEntriesRequest")]
    //    void AppendEntriesRequest(AppendEntriesRequest request);

    //    [OperationContractAttribute(IsOneWay = true, Action = "AppendEntriesReply")]
    //    void AppendEntriesReply(AppendEntriesReply reply);
    //}

    //[ServiceContract(SessionMode = SessionMode.NotAllowed)]
    //public interface INodeProxyAsync : INodeProxy
    //{
    //    [OperationContractAttribute(IsOneWay = true, AsyncPattern = true, Action = "VoteRequest")]
    //    IAsyncResult BeginVoteRequest(VoteRequest request, AsyncCallback callback, object asyncState);

    //    void EndVoteRequest(IAsyncResult r);

    //    [OperationContractAttribute(IsOneWay = true, AsyncPattern = true, Action = "VoteReply")]
    //    IAsyncResult BeginVoteReply(VoteReply reply, AsyncCallback callback, object asyncState);

    //    void EndVoteReply(IAsyncResult r);

    //    [OperationContractAttribute(IsOneWay = true, AsyncPattern = true, Action = "AppendEntriesRequest")]
    //    IAsyncResult BeginAppendEntriesRequest(AppendEntriesRequest request, AsyncCallback callback, object asyncState);

    //    void EndAppendEntriesRequest(IAsyncResult r);

    //    [OperationContractAttribute(IsOneWay = true, AsyncPattern = true, Action = "AppendEntriesReply")]
    //    IAsyncResult BeginAppendEntriesReply(AppendEntriesReply request, AsyncCallback callback, object asyncState);

    //    void EndAppendEntriesReply(IAsyncResult r);
    //}


    ////Factory class for client proxy
    //public abstract class ClientFactory
    //{
    //    public static INodeProxyAsync CreateClient(Type targetType)
    //    {
    //        BasicHttpBinding binding = new BasicHttpBinding();
    //        //Get the address of the service from configuration or some other mechanism - Not shown here
    //        EndpointAddress address = new EndpointAddress("http://localhost:7741/CategoryServiceHost.svc");

    //        var factory3 = new ChannelFactory<INodeProxyAsync>(binding, address);
    //        return factory3.CreateChannel();
    //    }
    //}
}






//using System;
//using System.Collections.Generic;
//using System.IO;
//using System.Linq;
//using System.Net;
//using System.ServiceModel;
//using System.ServiceModel.Description;
//using System.ServiceModel.Web;
//using System.Text;
//using System.Threading.Tasks;

//namespace ConsoleApplication5
//{
//    public class Post_fb9efac5_8b57_417e_9f71_35d48d421eb4
//    {
//        public class FileIndex
//        {
//            public string name;
//        }

//        [ServiceContract]
//        public interface ITest
//        {
//            [OperationContract]
//            [WebGet]
//            Stream DownloadFile(string fileName);
//            [OperationContract]
//            [WebInvoke(UriTemplate = "/UploadFile/{fileName}", ResponseFormat = WebMessageFormat.Json)]
//            FileIndex UploadFile(string fileName, Stream fileContents);
//        }

//        static long CountBytes(Stream stream)
//        {
//            byte[] buffer = new byte[100000];
//            int bytesRead;
//            long totalBytesRead = 0;
//            do
//            {
//                bytesRead = stream.Read(buffer, 0, buffer.Length);
//                totalBytesRead += bytesRead;
//            } while (bytesRead > 0);
//            return totalBytesRead;
//        }

//        class MyReadonlyStream : Stream
//        {
//            long length;
//            long leftToRead;
//            public MyReadonlyStream(long length)
//            {
//                this.length = length;
//                this.leftToRead = length;
//            }

//            public override bool CanRead
//            {
//                get { return true; }
//            }

//            public override bool CanSeek
//            {
//                get { return false; }
//            }

//            public override bool CanWrite
//            {
//                get { return false; }
//            }

//            public override void Flush()
//            {
//            }

//            public override long Length
//            {
//                get { return this.length; }
//            }

//            public override long Position
//            {
//                get { throw new NotSupportedException(); }
//                set { throw new NotSupportedException(); }
//            }

//            public override int Read(byte[] buffer, int offset, int count)
//            {
//                int toReturn = (int)Math.Min(this.leftToRead, (long)count);
//                this.leftToRead -= toReturn;
//                return toReturn;
//            }

//            public override long Seek(long offset, SeekOrigin origin)
//            {
//                throw new NotSupportedException();
//            }

//            public override void SetLength(long value)
//            {
//                throw new NotSupportedException();
//            }

//            public override void Write(byte[] buffer, int offset, int count)
//            {
//                throw new NotSupportedException();
//            }
//        }

//        public class Service : ITest
//        {
//            public Stream DownloadFile(string fileName)
//            {
//                WebOperationContext.Current.OutgoingResponse.Headers["Content-Disposition"] = "attachment; filename=" + fileName;
//                return new MyReadonlyStream(200000000); //200MB
//            }

//            public FileIndex UploadFile(string fileName, Stream fileContents)
//            {
//                long totalBytesRead = CountBytes(fileContents);
//                Console.WriteLine("Total bytes read for file {0}: {1}", fileName, totalBytesRead);
//                return new FileIndex() { name = fileName};
//            }
//        }

//        public static void Main()
//        {
//            string baseAddress = "http://" + Environment.MachineName + ":9000/Service";
//            ServiceHost host = new ServiceHost(typeof(Service), new Uri(baseAddress));
//            WebHttpBinding binding = new WebHttpBinding
//            {
//                TransferMode = TransferMode.Streamed,
//                MaxReceivedMessageSize = int.MaxValue,
//            };
//            binding.ReaderQuotas.MaxArrayLength = int.MaxValue;
//            host.AddServiceEndpoint(typeof(ITest), binding, "").Behaviors.Add(new WebHttpBehavior());
//            host.Open();
//            Console.WriteLine("Host opened on {0}", baseAddress);
//            Console.Read();

//            HttpWebRequest req = (HttpWebRequest)HttpWebRequest.Create(baseAddress + "/DownloadFile?fileName=test.txt");
//            req.Method = "GET";
//            HttpWebResponse resp;
//            try
//            {
//                resp = (HttpWebResponse)req.GetResponse();
//            }
//            catch (WebException e)
//            {
//                resp = (HttpWebResponse)e.Response;
//            }

//            Console.WriteLine("HTTP/{0} {1} {2}", resp.ProtocolVersion, (int)resp.StatusCode, resp.StatusDescription);
//            foreach (string header in resp.Headers.AllKeys)
//            {
//                Console.WriteLine("{0}: {1}", header, resp.Headers[header]);
//            }

//            Stream respStream = resp.GetResponseStream();
//            long size = CountBytes(respStream);
//            Console.WriteLine("Response size: {0}", size);

//            req = (HttpWebRequest)HttpWebRequest.Create(baseAddress + "/UploadFile/test.txt");
//            req.Method = "POST";
//            req.SendChunked = true;
//            req.AllowWriteStreamBuffering = false;
//            req.ContentType = "application/octet-stream";
//            Stream reqStream = req.GetRequestStream();
//            byte[] buffer = new byte[10000000];
//            long bytesWritten = 0;
//            for (int i = 0; i < 50; i++)
//            {
//                reqStream.Write(buffer, 0, buffer.Length);
//                bytesWritten += buffer.Length;
//                if ((i % 10) == 0)
//                {
//                    Console.WriteLine("Wrote {0} bytes", bytesWritten);
//                }
//            }
//            reqStream.Close();
//            resp = (HttpWebResponse)req.GetResponse();
//            Console.WriteLine(resp.StatusCode);
//            Console.Read();
//        }
//    }
//}
