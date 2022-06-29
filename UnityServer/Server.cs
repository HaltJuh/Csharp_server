using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Collections;
using System.Text.Json;



namespace UnityServer
{
    class Server
    {
        static IPEndPoint localEndPoint;    //Instance of a IPEndPoint, stores the servers ip address and port.
        static TcpListener listener;        //Instance of a TcpListener class, used for listening incoming connections to the given IPEndPoint.
        public List<Client> clients;        //List of connected Client classes.
        int currentTurn;                    //keeps track of whose turn it currently is.
        bool running;                       //Is the server running?
        int lastId;                         //Keeps track of what ip was given last to make sure no two clients have the same id.

        /// <summary>
        /// Called to start the server. 
        /// Creates an endpoint, connects to it, initializes all needed values and tells the listener to start listening for connections.
        /// </summary>
        public void StartServer()
        {
            lastId = 0;
            currentTurn = 0;
            IPHostEntry ipHost = Dns.GetHostEntry(Dns.GetHostName());
            IPAddress ipAddr = ipHost.AddressList[1];
            localEndPoint = new IPEndPoint(ipAddr, 8000);
            running = true;

            clients = new List<Client>();
            listener = new TcpListener(localEndPoint);
            listener.Start();

            listener.BeginAcceptTcpClient(new AsyncCallback(TCPCallback), null);

            Console.WriteLine($"Server started at: {localEndPoint}");
        }


        /// <summary>
        /// Asynchrous callback from a new connection.
        /// Creates a new Client object and stores it in the clients list.
        /// Starts a new asynchronous listen call, for new clients.
        /// </summary>
        /// <param name="result">
        /// Result from the asynchronous call, holds information from the connected client.
        /// </param>
        void TCPCallback(IAsyncResult result)
        {
            if (running)
            {
                try
                {
                    clients.Add(new Client(this, listener.EndAcceptTcpClient(result), ++lastId));
                    if (clients.Count == 1)
                    {
                        //If the connected client is the only one on the server. Tell them it is their turn.
                        SendToClient(clients[0], "yourturn", SendType.YourTurn);
                        currentTurn = 0;
                    }
                    listener.BeginAcceptTcpClient(new AsyncCallback(TCPCallback), null);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }
        }
        /// <summary>
        /// Moves the current turn count forward by one or more until it finds the next client capable of conducting their turn.
        /// Tells the found client that it's their turn.
        /// If a full round of searching is made, returns out to avoid a infinte loop
        /// </summary>
        public void EndTurn()
        {
            int rounds = 0;
            currentTurn++;
            while(true)
            {
                if (currentTurn >= clients.Count)
                {
                    currentTurn = 0;
                }
                if (clients.Count > 0 && clients[currentTurn].characterData.unitsLeft > 0)
                {
                    SendToClient(clients[currentTurn], "yourturn", SendType.YourTurn);
                    Console.WriteLine($"It is player {clients[currentTurn].ID}s turn now");
                    return;
                }
                currentTurn++;
                rounds++;
                if(rounds > clients.Count)
                {
                    return;
                }
            }
            


        }

        /// <summary>
        /// Sends a jsonfied data object to all clients using the given clients id as the sender id. Data won't be sent to the given client.
        /// </summary>
        /// <typeparam name="T">
        /// Type of data object.
        /// </typeparam>
        /// <param name="exception">
        /// Exception, data is not send to this client. 
        /// </param>
        /// <param name="data">
        /// Data object to be jsonfied 
        /// </param>
        /// <param name="type">
        /// Specifies use case of data.
        /// </param>

        public void SendToAllClients<T>(Client exception, T data, SendType type)
        {
            string message = type.value + "!";
            message += JsonSerializer.Serialize(data);

            List<byte> dataBuffer = new List<byte>();
            dataBuffer.AddRange(BitConverter.GetBytes(exception.ID));
            dataBuffer.AddRange(BitConverter.GetBytes(message.Length));
            dataBuffer.AddRange(Encoding.ASCII.GetBytes(message));


            for (int i = 0; i < clients.Count; i++)
            {
                if (clients[i] != null && clients[i].ID != exception.ID)
                {
                    clients[i].stream.BeginWrite(dataBuffer.ToArray(), 0, dataBuffer.Count, null, null);
                }
            }
        }
        /// <summary>
        /// Sends a string message to all clients using the given clients id as the sender. Data won't be send to the given client.
        /// </summary>
        /// <param name="exception">
        /// Exception, data is not send to this client. 
        /// </param>
        /// <param name="data">
        /// String message to be sent.
        /// </param>
        /// <param name="type">
        /// Specifies use case of data.
        /// </param>
        public void SendToAllClients(Client exception, string data, SendType type)
        {
            string message = type.value + "!" + data;

            List<byte> dataBuffer = new List<byte>();
            dataBuffer.AddRange(BitConverter.GetBytes(exception.ID));
            dataBuffer.AddRange(BitConverter.GetBytes(message.Length));
            dataBuffer.AddRange(Encoding.ASCII.GetBytes(message));


            for (int i = 0; i < clients.Count; i++)
            {
                if (clients[i].socket != null && clients[i].ID != exception.ID)
                {
                    clients[i].stream.BeginWrite(dataBuffer.ToArray(), 0, dataBuffer.Count, null, null);
                }
            }
        }
        /// <summary>
        /// Sends a string message to all clients, with the server as the sender.
        /// </summary>
        /// <param name="data">
        /// String message to be sent.
        /// </param>
        /// <param name="type">
        /// Specifies use case of data.
        /// </param>
        public void SendToAllClients(string data, SendType type)
        {
            string message = type.value + "!"+ data;

            List<byte> dataBuffer = new List<byte>();
            dataBuffer.AddRange(BitConverter.GetBytes(0));
            dataBuffer.AddRange(BitConverter.GetBytes(message.Length));
            dataBuffer.AddRange(Encoding.ASCII.GetBytes(message));

            for (int i = 0; i < clients.Count; i++)
            {
                clients[i].stream.BeginWrite(dataBuffer.ToArray(), 0, dataBuffer.Count, null, null);
            }
            Console.WriteLine("Data Sent");
        }
        /// <summary>
        /// Sends a string message to the given client.
        /// </summary>
        /// <param name="client">
        /// The recipient of the data to be sent.
        /// </param>
        /// <param name="_message">
        /// String message to be sent.
        /// </param>
        /// <param name="type">
        /// Specifies use case of data.
        /// </param>
        public void SendToClient(Client client, string _message, SendType type)
        {
            string message = type.value + "!" + _message;

            List<byte> dataBuffer = new List<byte>();

            dataBuffer.AddRange(BitConverter.GetBytes(client.ID));
            dataBuffer.AddRange(BitConverter.GetBytes(message.Length));
            dataBuffer.AddRange(Encoding.ASCII.GetBytes(message));
            client.stream.BeginWrite(dataBuffer.ToArray(), 0, dataBuffer.Count, null, null);

        }
        /// <summary>
        /// Instantiates existing player units on the given clients clientside.
        /// </summary>
        /// <param name="client"></param>
        /// <param name="type">
        /// Specifies use case of data.
        /// </param>
        public void InstantiateExistingPlayers(Client client, SendType type)
        {
            for (int i = 0; i < clients.Count; i++)
            {

                Console.WriteLine("Instantiating others.");
                string message = type.value + "!";
                message += JsonSerializer.Serialize(clients[i].characterData);

                List<byte> dataBuffer = new List<byte>();
                dataBuffer.AddRange(BitConverter.GetBytes(clients[i].ID));
                dataBuffer.AddRange(BitConverter.GetBytes(message.Length));
                dataBuffer.AddRange(Encoding.ASCII.GetBytes(message));

                client.stream.BeginWrite(dataBuffer.ToArray(), 0, dataBuffer.Count, null, null);

            }
        }
        /// <summary>
        /// Sends a jsonfied data object to the given client.
        /// </summary>
        /// <typeparam name="T">
        /// Type of data object.
        /// </typeparam>
        /// <param name="client">
        /// The recipient of the data to be sent.
        /// </param>
        /// <param name="data">
        /// Data to be jsonfied.
        /// </param>
        /// <param name="type">
        /// Specifies use case of data.
        /// </param>
        public void SendToClient<T>(Client client, T data, SendType type)
        {
            Console.WriteLine("Sending json to client");
            string message = type.value + "!";
            message += JsonSerializer.Serialize(data);

            List<byte> buffer = new List<byte>();
            buffer.AddRange(BitConverter.GetBytes(client.ID));
            buffer.AddRange(BitConverter.GetBytes(message.Length));
            buffer.AddRange(Encoding.ASCII.GetBytes(message));

            client.stream.BeginWrite(buffer.ToArray(), 0, buffer.Count, null, null);

        }
        /// <summary>
        /// Removes the client with the given id, from the clients list.
        /// </summary>
        /// <param name="id"></param>
        public void RemoveClient(int id)
        {

            for (int i = 0; i < clients.Count; i++)
            {
                if (clients[i].ID == id)
                {
                    clients.RemoveAt(i);
                    Console.WriteLine($"Players left: {clients.Count}");

                    //If it was the given clients turn, end the current turn to let still connected players have their turns.
                    if (i == currentTurn)
                    {
                        EndTurn();
                    }
                }
            }
        }
        /// <summary>
        /// Closes the server.
        /// </summary>
        public void CloseServer()
        {
            running = false;
            DisconnectAllClients("Server closing down.");
            listener.Stop();
        }
        /// <summary>
        /// Disconnects all clients.
        /// </summary>
        /// <param name="reason">
        /// The reason for the disconnect. Cients get a notification.
        /// </param>
        public void DisconnectAllClients(string reason)
        {
            foreach (Client client in clients)
            {
                client.Disconnect(reason,false);
            }
            clients.Clear();
        }
        /// <summary>
        /// Returns true if client with given id is found.
        /// </summary>
        /// <param name="id">
        /// Id to find.
        /// </param>
        /// <param name="client">
        /// Used to return a reference to the found client.
        /// </param>
        /// <returns></returns>
        public bool GetClient(int id, out Client client)
        {
            for (int i = 0; i < clients.Count; i++)
            {
                if (clients[i].ID == id)
                {
                    client = clients[i];
                    return true;
                }
            }
            client = null;
            return false;
        }
        /// <summary></summary>
        /// <param name="id">
        /// Id to find.
        /// </param>
        /// <returns>
        /// Returns found client with given id.
        /// </returns>
        public Client GetClient(int id)
        {
            for (int i = 0; i < clients.Count; i++)
            {
                if (clients[i].ID == id)
                {
                    return clients[i];
                }
            }
            return null;
        }
    }
}
