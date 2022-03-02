using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client1 {

    class Program {
        static void Main(string[] args) {
            var client = new Client();
            client.ConnectToServer();

            var key = Console.ReadKey(true).KeyChar;
            client.JoinVoiceChannel(key);

            while (true) {
                Console.ReadLine();
            }
        }
    }

}