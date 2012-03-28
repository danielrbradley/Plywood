using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;

namespace Plywood.Ply
{
    class Program
    {
        // dep [ACTION] [OPTION]... [FOLDER]...
        static void Main(string[] args)
        {
            try
            {
                if (args == null || args.Length == 0)
                {
                    SyntaxError();
                    return;
                }

                switch (args[0].ToLower())
                {
                    case "pull":
                        Pull.Run(args);
                        break;
                    case "push":
                        Push.Run(args);
                        break;
                    case "add-to-path":
                        Setup.AddToPath(args);
                        break;
                    case "help":
                        PrintHelp(args);
                        break;
                    default:
                        SyntaxError();
                        break;
                }
            }
            catch (HandledCliException) { }
            catch (Exception ex)
            {
                Console.WriteLine("Uncaught exception.");
                Console.WriteLine(ex.ToString());
            }
            if (Debugger.IsAttached)
                Console.ReadKey();
        }

        static void SyntaxError()
        {
            Console.WriteLine("Try 'ply help' for more information.");
        }

        static void PrintHelp(string[] args)
        {
            if (args.Length > 1)
            {
                switch (args[1].ToLower())
                {
                    case "pull":
                        Pull.Help(args);
                        break;
                    case "push":
                        Push.Help(args);
                        break;
                    case "add-to-path":
                        Setup.AddToPathHelp(args);
                        break;
                    default:
                        Console.WriteLine("Try 'ply help' for more information.");
                        break;
                }
            }
            else
                GeneralHelp();
        }

        private static void GeneralHelp()
        {
            Console.WriteLine(
@"Usage: 'ply [COMMAND] [OPTIONS]...'
ply is designed to help with configuring and running deployment services.

[COMMAND]s
pull                  View the configuration currently saved in the registry.
push                  Load a new configuration into the registry.
add-to-path           Add a diretory to the path environment variable.
help                  Print this help message.

To get more information on a command use 'ply help [COMMAND]'.");
        }
    }

    public class CliSyntaxException : Exception
    {
        public CliSyntaxException() : base() { }
        public CliSyntaxException(string message) : base(message) { }
        public CliSyntaxException(string message, Exception ex) : base(message, ex) { }
        public CliSyntaxException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

    public class HandledCliException : Exception
    {
        public HandledCliException() : base() { }
        public HandledCliException(string message) : base(message) { }
        public HandledCliException(string message, Exception ex) : base(message, ex) { }
        public HandledCliException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

}
