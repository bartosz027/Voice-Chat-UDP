namespace Startup {

    using System;
    using System.Diagnostics;

    class Program {
        static void Main(string[] args) {
            var processes = new Process[3] {
                Process.Start("Server.exe"),
                Process.Start("Client.exe", "true"),
                Process.Start("Client.exe", "false")
            };

            // Wait for server process
            processes[0].WaitForExit();

            foreach (var process in processes) {
                if (!process.HasExited) {
                    process.Kill();
                }
            }
        }
    }

}