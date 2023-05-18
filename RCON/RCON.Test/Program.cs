using RCON.Core;
using System.Net.Sockets;
using System.Text;

namespace RCON.Test
{
    internal class Program
    {
        static string Formmater(string command, int ID, List<Packet> result)
        {
            StringBuilder stringBuilder = new StringBuilder();
            foreach (var item in result)
            {
                stringBuilder.Append(item.Body);
            }
            return stringBuilder.ToString();
        }

        static async Task Main(string[] args)
        {
            RconClient client = new RconClient();

            await client.ConnectAsync("127.0.0.1", 25575);

            var res = await client.AuthAsync("12345678");

            if (res)
                await Console.Out.WriteLineAsync("Working!!");

            var response = await client.ExecCommandAsync("help", 200, Formmater, 5000);

            await Console.Out.WriteLineAsync(response);

            //if (response != null)
            //{
            //    for (int i = 0; i < response.Count; i++)
            //    {
            //        await Console.Out.WriteLineAsync(response[i].ToString());
            //    }
            //}

            client.Close();
        }
    }
}