using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.Text;
using System.Threading.Tasks;

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
    [ServiceContract()]
    public interface IMyService
    {
        [OperationContract()]
        void DoSomething();
    }

    public class MyService : IMyService
    {

        public void DoSomething()
        {
            Console.WriteLine("something!");
            // do something
        }
    }

    //Factory class for client proxy
    public abstract class ClientFactory
    {
        public static IMyService CreateClient(Type targetType)
        {
            BasicHttpBinding binding = new BasicHttpBinding();
            //Get the address of the service from configuration or some other mechanism - Not shown here
            EndpointAddress address = new EndpointAddress("http://localhost:7741/CategoryServiceHost.svc");

            var factory3 = new ChannelFactory<IMyService>(binding, address);
            return factory3.CreateChannel();
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            var endpoint = new Uri("http://localhost:7741/CategoryServiceHost.svc");
            var host = new ServiceHost(typeof(MyService), endpoint);

            var binding = new BasicHttpBinding();

            host.AddServiceEndpoint(typeof(IMyService), binding, endpoint);

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
            var pClient = ClientFactory.CreateClient(typeof(IMyService));
            pClient.DoSomething();
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
