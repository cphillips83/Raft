using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Raft.Messages;

namespace Raft
{
    //Haystack tech talk http://www.downvids.net/haystack-tech-talk-4-28-2009--375887.html

    /* Design Ideas
     *  Modeling after haystack
     *  
     * Data Store
     *  Each store will have several physical volumes
     *  Each physical volume can be in multiple logical volumes
     *  Logical volumes can span across data centers
     *  
     * 
     */

    ////An AsyncResult that completes as soon as it is instantiated.
    //internal class CompletedAsyncResult : AsyncResult
    //{
    //    public CompletedAsyncResult(AsyncCallback callback, object state)
    //        : base(callback, state)
    //    {
    //        Complete(true);
    //    }

    //    public static void End(IAsyncResult result)
    //    {
    //        AsyncResult.End<CompletedAsyncResult>(result);
    //    }
    //}

    //internal class CompletedAsyncResult<T> : AsyncResult
    //{
    //    private T _data;

    //    public CompletedAsyncResult(T data, AsyncCallback callback, object state)
    //        : base(callback, state)
    //    {
    //        _data = data;
    //        Complete(true);
    //    }

    //    public static T End(IAsyncResult result)
    //    {
    //        CompletedAsyncResult<T> completedResult = AsyncResult.End<CompletedAsyncResult<T>>(result);
    //        return completedResult._data;
    //    }
    //}

    //internal class CompletedAsyncResult<TResult, TParameter> : AsyncResult
    //{
    //    private TResult _resultData;
    //    private TParameter _parameter;

    //    public CompletedAsyncResult(TResult resultData, TParameter parameter, AsyncCallback callback, object state)
    //        : base(callback, state)
    //    {
    //        _resultData = resultData;
    //        _parameter = parameter;
    //        Complete(true);
    //    }

    //    public static TResult End(IAsyncResult result, out TParameter parameter)
    //    {
    //        CompletedAsyncResult<TResult, TParameter> completedResult = AsyncResult.End<CompletedAsyncResult<TResult, TParameter>>(result);
    //        parameter = completedResult._parameter;
    //        return completedResult._resultData;
    //    }
    //}
    [ServiceContract()]
    public interface INodeService
    {
        [OperationContractAttribute(Action = "SampleMethod", ReplyAction = "ReplySampleMethod")]
        string SampleMethod(string msg);

        //[OperationContractAttribute(AsyncPattern = true)]
        //IAsyncResult BeginSampleMethod(string msg, AsyncCallback callback, object asyncState);

        ////Note: There is no OperationContractAttribute for the end method.
        //string EndSampleMethod(IAsyncResult result);

        //[OperationContractAttribute(AsyncPattern = true, Action="ServiceAsync")]
        //IAsyncResult BeginServiceAsyncMethod(string msg, AsyncCallback callback, object asyncState);

        //// Note: There is no OperationContractAttribute for the end method.
        //string EndServiceAsyncMethod(IAsyncResult result);

        //[OperationContract(AsyncPattern=true)]
        //IAsyncResult BeginRequestVote(VoteRequest request);

        //VoteReply EndRequestVote(IAsyncResult result);

        //[OperationContract(AsyncPattern = true)]
        //IAsyncResult BeginAppendEntries(AppendEntriesRequest request);

        //AppendEntriesReply EndAppendEntries(IAsyncResult result);
    }

    [ServiceContract()]
    public interface INodeServiceAsync : INodeService
    {
        [OperationContractAttribute(AsyncPattern = true, Action = "SampleMethod", ReplyAction="ReplySampleMethod")]
        IAsyncResult BeginSampleMethod(string msg, AsyncCallback callback, object asyncState);

        //Note: There is no OperationContractAttribute for the end method.
        string EndSampleMethod(IAsyncResult result);
    }

    public class Node
    {

    }

    public class Client
    {
        
    }

    public class Server
    {

    }

    public class Cluster
    {

    }



    public class MyService : INodeService
    {
        public string SampleMethod(string msg)
        {
            Console.WriteLine("Called synchronous sample method with \"{0}\"", msg);
            return "The sychronous service greets you: " + msg;
        }
    }



    //Factory class for client proxy
    public abstract class ClientFactory
    {
        public static INodeServiceAsync CreateClient(Type targetType)
        {
            BasicHttpBinding binding = new BasicHttpBinding();
            //Get the address of the service from configuration or some other mechanism - Not shown here
            EndpointAddress address = new EndpointAddress("http://localhost:7741/CategoryServiceHost.svc");

            var factory3 = new ChannelFactory<INodeServiceAsync>(binding, address);
            return factory3.CreateChannel();
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            //new TypedServiceReference
            var endpoint = new Uri("http://localhost:7741/CategoryServiceHost.svc");
            var host = new ServiceHost(typeof(MyService), endpoint);

            var binding = new BasicHttpBinding();

            host.AddServiceEndpoint(typeof(INodeService), binding, endpoint);

            host.Description.Behaviors.Add(new ServiceMetadataBehavior()
            {
                HttpGetEnabled = true,
                MetadataExporter = { PolicyVersion = PolicyVersion.Policy15 },
            });

            host.Open();
            //while (true)
            //{
            //    System.Threading.Thread.Sleep(0);
            //}
            Test2();
            //TestConsole();
        }
        public static void Test2()
        {

            //create client proxy from factory
            var pClient = ClientFactory.CreateClient(typeof(INodeService));
            {
                Console.WriteLine(pClient.SampleMethod("simple"));
            }
            {
                var r = pClient.BeginSampleMethod("sample", null, null);
                while (!r.IsCompleted)
                {
                    Console.WriteLine(r.IsCompleted);
                    System.Threading.Thread.Sleep(0);
                }
                Console.WriteLine(pClient.EndSampleMethod(r));
                Console.WriteLine(r.IsCompleted);
            }
            //{
            //    var r = pClient.BeginServiceAsyncMethod("test", null, null);
            //    Console.WriteLine(pClient.EndServiceAsyncMethod(r));
            //}
            Console.Read();
            //Console.WriteLine(pClient.DoSomething());
            //((IClientChannel)pClient).RemoteAddress
        }

        
        static void TestConsole()
        {
            var running = true;
            var model = SimulationModel.SetupFreshScenario();
            while (running)
            {
                if (Console.KeyAvailable)
                {
                    var key = Console.ReadKey();
                    switch (key.KeyChar)
                    {
                        case 'x': running = false; break;
                        case 'k':
                            {
                                var leader = model.GetLeader();
                                if (leader != null)
                                    leader.Stop(model);
                            }
                            break;
                        case 'u':
                            model.ResumeAllStopped();
                            break;
                        case 'r': model.ClientRequest(); break;
                        case 'a':
                            {
                                //add server to cluster
                                var leader = model.GetLeader();
                                if (leader != null)
                                    model.JoinServer(leader);
                            }
                            break;
                    }
                }
                model.Advance();
                System.Threading.Thread.Sleep(1);
            }
        }

    }

}
