using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Net;
using System.Net.Sockets;

namespace UnityServer
{
    class Client
    {
        public static int dataBufferSize = 4096;    //Maximun sife of send data.

        Server server;                              //Server class reference.
        public TcpClient socket;                    //Stores the connected clients socket as a TcpClient class.
        public NetworkStream stream;                //Network stream on what data is written and from where it's read.
        byte[] receiveBuffer;                       //Data buffer used for temporarily storing read data.


        public int ID { get; private set; }         //Client id.
        public PlayerData characterData;            //Stores clients units information


        /// <summary>
        /// Construtor for Client class. Initializes data and starts an asynchronous listening from the clients socket.
        /// </summary>
        /// <param name="_server">
        /// Reference to the server
        /// </param>
        /// <param name="_socket">
        /// TcpClient to be stored.
        /// </param>
        /// <param name="_id">
        /// Clients id.
        /// </param>
        public Client(Server _server, TcpClient _socket, int _id)
        {
            Console.WriteLine($"I was given the id of {_id}");
            ID = _id;
            server = _server;
            socket = _socket;
            socket.ReceiveBufferSize = dataBufferSize;
            socket.SendBufferSize = dataBufferSize;
            receiveBuffer = new byte[dataBufferSize];

            characterData = new PlayerData();
            characterData.SetMoveData(new MoveData(ID));
            Console.WriteLine($"New Client object created for EndPoint of: {_socket.Client.RemoteEndPoint}");
            stream = socket.GetStream();

            stream.BeginRead(receiveBuffer, 0, dataBufferSize, ReceiveCallback, null);

            server.InstantiateExistingPlayers(this, SendType.Instantiate);

        }
        //Asynchronous callback for incoming data from the connected socket.
        void ReceiveCallback(IAsyncResult _result)
        {

            try
            {
                if(socket != null && socket.Connected)
                {
                    int _byteLength = stream.EndRead(_result); //Ends the reading, stores amount of sent bytes.
                    if (_byteLength <= 0)
                    {
                        //If no data send, client has disconnected from the server
                        Disconnect("No data received from data transfer.",true);
                        return;
                    }
                    Console.WriteLine("Connected client sending data");
                    byte[] dataBuffer = new byte[_byteLength];
                    Array.Copy(receiveBuffer, dataBuffer, _byteLength);
                    int id = BitConverter.ToInt32(dataBuffer, 0);
                    int size = BitConverter.ToInt32(dataBuffer, 4);
                    string data = Encoding.ASCII.GetString(dataBuffer, 8, size);
                    Console.WriteLine($"Client(id: {ID}) sent a message with of size of {size} bytes");

                    HandleData(data);
                    stream.BeginRead(receiveBuffer, 0, dataBufferSize, ReceiveCallback, null);
                }
            }
            catch (Exception _ex)
            {
                Console.WriteLine($"Error receiving TCP data: {_ex}");
                Disconnect($"Error receiving TCP data: {_ex}",true);
            }
        }
        /// <summary>
        /// Splits the data into an identifier and actual data.
        /// Deals with the data depending on the identifier.
        /// </summary>
        /// <param name="fullData">
        /// Data to be handled.
        /// </param>
        void HandleData(string fullData)
        {
            string identifier = "";
            string data = "";
            bool identifierRead = false;
            for (int i = 0; i < fullData.Length; i++)
            {
                if (fullData[i] == '!')
                {
                    identifierRead = true;
                }
                else
                {
                    if (!identifierRead)
                    {
                        identifier += fullData[i];
                    }
                    else
                    {
                        data += fullData[i];
                    }
                }

            }
            //Depending on the identifier, deals with the data accordingly. 
            switch (identifier)
            {
                case "MOVE":
                    characterData.SetMoveData(JsonSerializer.Deserialize<MoveData>(data));
                    server.SendToAllClients<MoveData>(this, characterData.ToMoveData(), SendType.Move);
                    break;
                case "ATTACK":
                    server.SendToAllClients(this, data, SendType.Attack);
                    break;
                case "DAMAGE":
                    DamageData damageData = JsonSerializer.Deserialize<DamageData>(data);
                    server.GetClient(damageData.HitId).characterData.TakeDamage(damageData.Damage);
                    server.SendToAllClients(this,data, SendType.Damage);
                    break;
                case "ENDTURN":
                    server.EndTurn();
                    break;
                case "CONNECT":
                    characterData.Username = data;
                    characterData.unitsLeft = 1;
                    server.SendToClient<PlayerData>(this, characterData, SendType.Connect);
                    server.SendToAllClients<PlayerData>(this, characterData, SendType.Instantiate);
                    break;
                default:
                    Console.WriteLine("Unidentified Indentifier");
                    Console.WriteLine(identifier);
                    Console.WriteLine(data);
                    break;
            }

        }
        /// <summary>
        /// Disconnects the client and informs the client why they were disconnected.
        /// </summary>
        /// <param name="reason">
        /// Reason for getting disconnected.
        /// </param>
        /// <param name="removeClient">
        /// Should the client be removed from the client list?
        /// </param>
        public void Disconnect(string reason,bool removeClient)
        {
            Console.WriteLine($"Client {ID} disconnected!");
            if (socket != null)
            {
                server.SendToAllClients(this, "Disconnected", SendType.Destroy);
                server.SendToClient(this, reason, SendType.Disconnect);
                stream.Close();
                socket.Close();
                socket = null;

                if (removeClient)
                {
                    server.RemoveClient(ID);
                }
            }
        }
    }
}
