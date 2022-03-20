namespace Client {

    class Program {
        static void Main(string[] args) {
            var client = new Client();
            client.ConnectToServer();

            var key = System.Console.ReadKey(true).KeyChar;
            client.JoinVoiceChannel(key);

            if (args.Length > 0) {
                client.MuteVoiceChannel(args[0]);
            }

            while (true) {
                System.Console.ReadLine();
            }
        }
    }

}