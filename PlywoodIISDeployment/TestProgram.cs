using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Plywood.IISDeployApi
{
    class TestProgram
    {
        static void Main(string[] args)
        {
            switch (args.Length)
            {
                case 1:
                    IISDeployer.Deploy(args[0]);
                    break;
                case 2:
                    switch (args[0].ToLower())
                    {
                        case "sync":
                            IISDeployer.Deploy(args[1]);
                            break;
                        case "uninstall":
                            IISDeployer.UnDeploy(args[1]);
                            break;
                        default:
                            Console.WriteLine("Enter an action followed by a path.");
                            break;
                    }
                    break;
                default:
                    Console.WriteLine("Enter an action followed by a path.");
                    break;
            }
        }
    }
}
