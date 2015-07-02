using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Raft
{


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
                        case 'r': model.ClientRequest(); break;
                    }
                }
                model.Advance();
                System.Threading.Thread.Sleep(10);
            }

        }
    }
}
