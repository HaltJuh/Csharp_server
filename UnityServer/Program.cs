using System;

namespace UnityServer
{
    class Program
    {
        static Server server;
        static bool isRunning = false;
        static void Main(string[] args)
        {
            Console.Title = "Game Server";
            server = new Server();
            Console.WriteLine("Starting server...");
            server.StartServer();
            isRunning = true;
            while(isRunning)
            {
                string command;
                Console.WriteLine("Enter command: ");
                command = Console.ReadLine();
                if(command == "Stop")
                {
                    isRunning = false;
                }
                if(command == "Reset")
                {
                    Console.WriteLine("Resetting server...");
                    server.CloseServer();
                    
                    server = new Server();
                    Console.WriteLine("Restarting server...");
                    server.StartServer();
                }
                if(command == "Players")
                {
                    Console.WriteLine("Printing all connected clients.");
                    foreach(Client client in server.clients)
                    {
                        Console.WriteLine($"{client.characterData.Username} (id:{client.ID})");
                    }
                }
                if(command == "Kick")
                {
                    Console.WriteLine("Enter player id to be kicked.");
                    int id = int.Parse(Console.ReadLine());
                    Client client;
                    if(server.GetClient(id,out client))
                    {
                        Console.WriteLine("Enter a reason");
                        string reason = Console.ReadLine();
                        if (reason.Length > 5)
                        {
                            client.Disconnect(reason,true);
                        }
                    }
                }
            }
            server.CloseServer();
            Console.WriteLine("Server shutting down");
            Console.WriteLine("Press Enter to close the console");
            Console.ReadKey();
        }
    }
}
