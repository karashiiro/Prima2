using System;
using System.Net.Http;
using System.Threading.Tasks;
using Prima.Services;

namespace TestBed
{
    public static class Program
    {
        public static async Task Main()
        {
            var http = new HttpClient();
            var xivapi = new XIVAPIService(http);
            var exceptions = 0;
            for (var i = 0; i < 20; i++)
            {
                Console.WriteLine("Request {0}/{1}...", i + 1, 20);
                try
                {
                    await xivapi.SearchCharacter("Coeurl", "Karashiir Akhabila");
                }
                catch (XIVAPICharacterNotFoundException)
                {
                    exceptions++;
                }

                await Task.Delay(5000);
            }
            Console.WriteLine("{0} exceptions caught", exceptions);
        }
    }
}
