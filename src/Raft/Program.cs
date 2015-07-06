using System;
using System.Collections.Generic;
using System.Linq;
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

    class Program
    {
        static void Main(string[] args)
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
                                    model.AddServer(leader);
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
