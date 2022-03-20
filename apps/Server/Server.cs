using System;
using System.Collections.Generic;

using System.Net;
using System.Net.Sockets;

using System.Text;
using System.Threading.Tasks;

namespace Server {

    class Server {
        public Server() {
            // Create voice channels
            _VoiceChannels.Add(new VoiceChannel() {
                ID = 1,
                Name = "Voice Channel 1"
            });

            _VoiceChannels.Add(new VoiceChannel() {
                ID = 2,
                Name = "Voice Channel 2"
            });
        }

        public void Start() {
            // Start UDP server
            Console.WriteLine("UDP Listener Started");
            Task.Run(() => UDPListen());

            // Start TCP server
            Console.WriteLine("TCP Listener Started");
            _ServerTCP.Start();

            // Accept TCP connections
            while (true) {
                var client = _ServerTCP.AcceptTcpClient();
                Task.Run(() => TCPListen(client));
            }
        }


        private void TCPListen(TcpClient client) {
            while (true) {
                byte[] data = new byte[4096];
                int bytes_count = 0;

                try {
                    var stream = client.GetStream();
                    bytes_count = stream.Read(data, 0, data.Length);
                }
                catch (Exception ex) {
                    Console.Write("TCP Error: {0}", ex.Message);
                }

                if(bytes_count != 0) {
                    string message = Encoding.UTF8.GetString(data);
                    Task.Run(() => ProcessMessageTCP(message.Substring(0, bytes_count), client));
                }
            }
        }

        private void UDPListen() {
            while (true) {
                byte[] data = null;
                IPEndPoint ep = _IPEndPointUDP;

                try {
                    data = _ServerUDP.Receive(ref ep);
                }
                catch (Exception ex) {
                    Console.WriteLine("UDP Error: {0}", ex.Message);
                }

                if (data != null) {
                    string message = Encoding.UTF8.GetString(data);
                    Task.Run(() => ProcessMessageUDP(message, ep));
                }
            }
        }


        private void ProcessMessageTCP(string message, TcpClient tcp_client) {
            var data = message.Split('|');

            switch(data[0]) {
                case "CONNECT_TCP": {
                    var id = long.Parse(data[1]);
                    var connected_client = _ConnectedClients.Find(p => (p.ID == id));

                    var ip = data[3].Split(':')[0];
                    var port = data[3].Split(':')[1];

                    connected_client.Client = tcp_client;
                    connected_client.Stream = tcp_client.GetStream();

                    connected_client.InternalEndPoint = new IPEndPoint(IPAddress.Parse(ip), int.Parse(port));
                    SendTCP("TEST_TCP" + "|" + "TCP Communication Test", tcp_client);

                    break;
                }
                case "JOIN_CHANNEL": {
                    var connected_client = _ConnectedClients.Find(p => (p.Client == tcp_client));
                    var channel = _VoiceChannels.Find(p => p.ID == int.Parse(data[1]));

                    if(!channel.Clients.Contains(connected_client)) {
                        var response_data = "";
                        var broadcast_data = "CLIENT_JOINED_TO_VOICE_CHANNEL" + "|" + connected_client.ID + "|" + connected_client.Username + "|" + connected_client.InternalEndPoint + "|" + connected_client.ExternalEndPoint;

                        foreach (var client in channel.Clients) {
                            response_data += "|" + client.ID + "|" + client.Username + "|" + client.InternalEndPoint + "|" + client.ExternalEndPoint;
                            SendTCP(broadcast_data, client.Client);
                        }

                        channel.Clients.Add(connected_client);
                        SendTCP("CONNECTED_TO_VOICE_CHANNEL" + response_data, tcp_client);
                    }

                    break;
                }
            }
        }

        private void ProcessMessageUDP(string message, IPEndPoint ep) {
            var data = message.Split('|');

            switch (data[0]) {
                case "CONNECT_UDP": {
                    var client = new ConnectedClient();

                    client.ID = long.Parse(data[1]);
                    client.Username = data[2];
                    client.ExternalEndPoint = ep;

                    _ConnectedClients.Add(client);
                    SendUDP("TEST_UDP" + "|" + "UDP Communication Test", client.ExternalEndPoint);

                    break;
                }
            }
        }


        private void SendTCP(string message, TcpClient client) {
            if (client != null && client.Connected) {
                byte[] data = Encoding.UTF8.GetBytes(message);

                NetworkStream stream = client.GetStream();
                stream.Write(data, 0, data.Length);
            }
        }

        private void SendUDP(string message, IPEndPoint ep) {
            byte[] data = Encoding.UTF8.GetBytes(message);
            _ServerUDP.Send(data, data.Length, ep);
        }


        // TCP
        private static IPEndPoint _IPEndPointTCP = new IPEndPoint(IPAddress.Any, 65535);
        private TcpListener _ServerTCP = new TcpListener(_IPEndPointTCP);

        // UDP
        private static IPEndPoint _IPEndPointUDP = new IPEndPoint(IPAddress.Any, 65535);
        private UdpClient _ServerUDP = new UdpClient(_IPEndPointUDP);

        // Voice channels
        private List<ConnectedClient> _ConnectedClients = new List<ConnectedClient>();
        private List<VoiceChannel> _VoiceChannels = new List<VoiceChannel>();
    }

    class ConnectedClient {
        // Client data
        public long ID { get; set; }
        public string Username { get; set; }

        // TCP connection
        public TcpClient Client { get; set; }
        public NetworkStream Stream { get; set; }

        // UDP connection
        public IPEndPoint InternalEndPoint { get; set; }
        public IPEndPoint ExternalEndPoint { get; set; }
    }

    class VoiceChannel {
        public VoiceChannel() {
            Clients = new List<ConnectedClient>();
        }

        // Channel data
        public long ID { get; set; }
        public string Name { get; set; }

        // List of clients
        public List<ConnectedClient> Clients { get; set; }
    }

}