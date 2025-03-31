using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace TestScript
{
    public static class Program
    {
        public static async Task Main(string[] args)
        {
            try
            {
                // Start
                Console.WriteLine("TestScript Starting");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }

}