using System;
using System.Collections.Generic;

using System.Net;
using System.Net.Sockets;

using System.Text;

using System.Threading;
using System.Threading.Tasks;

namespace Client2 {

    class ClientInfo {
        // Client data
        public long ID { get; set; }
        public string Username { get; set; }

        // UDP connection
        public IPEndPoint InternalEndPoint { get; set; }
        public IPEndPoint ExternalEndPoint { get; set; }
    }

    class Client {
        public Client() {
            _Client = new ClientInfo();
            _Receivers = new List<ClientInfo>();

            _Client.ID = DateTime.Now.Ticks;
            _Client.Username = Environment.MachineName;

            _Recorder = new AudioRecorder();
            _Recorder.StartRecording((sender, args) => {
                foreach (var receiver in _Receivers) {
                    SendVoiceUDP(args.Buffer, receiver.ExternalEndPoint);
                }
            });
        }

        public void ConnectToServer() {
            var ep = new IPEndPoint(IPAddress.Parse("85.193.246.112"), 65535);

            // Init TCP
            _ClientTCP = new TcpClient();
            _ClientTCP.Connect(ep);

            // Init UDP
            _ClientUDP = new UdpClient(0);

            // TCP & UDP listener
            Task.Run(() => ListenTCP());
            Task.Run(() => ListenUDP());

            // Init communication
            SendMessageUDP("CONNECT_UDP" + "|" + _Client.ID + "|" + _Client.Username, ep);
            Thread.Sleep(500);

            var ep1 = _ClientTCP.Client.LocalEndPoint as IPEndPoint;
            var ep2 = _ClientUDP.Client.LocalEndPoint as IPEndPoint;

            _Client.InternalEndPoint = new IPEndPoint(ep1.Address, ep2.Port);
            SendMessageTCP("CONNECT_TCP" + "|" + _Client.ID + "|" + _Client.Username + "|" + _Client.InternalEndPoint);
        }

        public void JoinVoiceChannel(char key) {
            switch (key) {
                case '1': {
                    SendMessageTCP("JOIN_CHANNEL" + "|" + "1");
                    break;
                }
                case '2': {
                    SendMessageTCP("JOIN_CHANNEL" + "|" + "2");
                    break;
                }
            }
        }


        private void ListenTCP() {
            while (true) {
                byte[] data = new byte[4096];

                try {
                    var stream = _ClientTCP.GetStream();
                    int bytes_count = stream.Read(data, 0, data.Length);

                    if (bytes_count != 0) {
                        string message = Encoding.UTF8.GetString(data);
                        Task.Run(() => ProcessMessageTCP(message.Substring(0, bytes_count)));
                    }
                }
                catch (Exception e) {
                    Console.WriteLine("Error on TCP Receive: " + e.Message);
                }
            }
        }

        private void ListenUDP() {
            while (true) {
                try {
                    IPEndPoint ep = _Client.InternalEndPoint;
                    byte[] data = _ClientUDP.Receive(ref ep);

                    if (ep != null) {
                        Task.Run(() => ProcessMessageUDP(data, ep));
                    }
                }
                catch (Exception e) {
                    Console.WriteLine("Error on UDP Receive: " + e.Message);
                }
            }
        }


        public void SendMessageTCP(string message) {
            if (_ClientTCP.Connected) {
                byte[] data = Encoding.ASCII.GetBytes(message);

                try {
                    NetworkStream stream = _ClientTCP.GetStream();
                    stream.Write(data, 0, data.Length);
                }
                catch (Exception e) {
                    Console.WriteLine("Error on TCP Send: " + e.Message);
                }
            }
        }

        public void SendMessageUDP(string message, IPEndPoint ep) {
            try {
                byte[] data = Encoding.ASCII.GetBytes(message);

                if (data != null) {
                    _ClientUDP.Send(data, data.Length, ep);
                }
            }
            catch (Exception e) {
                Console.WriteLine("Error on UDP Send: " + e.Message);
            }
        }

        public void SendVoiceUDP(byte[] data, IPEndPoint ep) {
            try {
                if (data != null) {
                    _ClientUDP.Send(data, data.Length, ep);
                }
            }
            catch (Exception e) {
                Console.WriteLine("Error on UDP Send: " + e.Message);
            }
        }


        public void ProcessMessageTCP(string message) {
            var data = message.Split('|');

            switch (data[0]) {
                case "TEST_TCP": {
                    Console.WriteLine(data[1]);
                    break;
                }
                case "CONNECTED_TO_VOICE_CHANNEL": {
                    ClientInfo client = null;

                    for (int i = 1; i < data.Length; i++) {
                        switch ((i - 1) % 4) {
                            case 0: {
                                client = new ClientInfo();
                                client.ID = long.Parse(data[i]);
                                break;
                            }
                            case 1: {
                                client.Username = data[i];
                                break;
                            }
                            case 2: {
                                var internal_ip = data[i].Split(':')[0];
                                var internal_port = data[i].Split(':')[1];

                                client.InternalEndPoint = new IPEndPoint(IPAddress.Parse(internal_ip), int.Parse(internal_port));
                                break;
                            }
                            case 3: {
                                var external_ip = data[i].Split(':')[0];
                                var external_port = data[i].Split(':')[1];

                                client.ExternalEndPoint = new IPEndPoint(IPAddress.Parse(external_ip), int.Parse(external_port));
                                _Receivers.Add(client);

                                break;
                            }
                        }
                    }

                    Console.WriteLine(message);
                    break;
                }
                case "CLIENT_JOINED_TO_VOICE_CHANNEL": {
                    var client = new ClientInfo();

                    var internal_ip = data[3].Split(':')[0];
                    var internal_port = data[3].Split(':')[1];

                    var external_ip = data[4].Split(':')[0];
                    var external_port = data[4].Split(':')[1];

                    client.ID = long.Parse(data[1]);
                    client.Username = data[2];
                    client.InternalEndPoint = new IPEndPoint(IPAddress.Parse(internal_ip), int.Parse(internal_port));
                    client.ExternalEndPoint = new IPEndPoint(IPAddress.Parse(external_ip), int.Parse(external_port));

                    Console.WriteLine(message);
                    _Receivers.Add(client);

                    break;
                }
            }
        }

        public void ProcessMessageUDP(byte[] data, IPEndPoint ep) {
            var message = Encoding.UTF8.GetString(data).Split('|');

            switch (message[0]) {
                case "TEST_UDP": {
                    Console.WriteLine(message[1]);
                    break;
                }
                default: {
                    _Recorder.AddDataToBuffer(data);
                    break;
                }
            }
        }


        // TCP & UDP
        private TcpClient _ClientTCP = new TcpClient();
        private UdpClient _ClientUDP = new UdpClient();

        private ClientInfo _Client;
        private List<ClientInfo> _Receivers;

        // Audio
        private AudioRecorder _Recorder;
    }

}