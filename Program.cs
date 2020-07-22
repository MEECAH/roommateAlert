using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text.Json.Serialization;


namespace RoommateAlert
{
    
    public class properties
    {
        public string auth { get; set; }
        public string routerPortal { get; set; }
        public string mac { get; set; }
        public string arriveURL { get; set; }
        public string departURL { get; set; }

    }
    class Program
    {
        
        static async Task Main(string[] args)
        {


            //initialize some stuff
            bool previouslyConnected = false;
            bool currentlyConnected = false;
            using var phone = new HttpClient();

            //read in sensitive data such as auths and api keys from gitignored json file
            var jsonString = File.ReadAllText(@"D:\code projects\roommateAlert\properties.json");
            var properties = JsonSerializer.Deserialize<properties>(jsonString);

            //for running infinitely in CLI
            var exitEvent = new ManualResetEvent(false);
            
            //for querying every x seconds
            var startTimeSpan = TimeSpan.Zero;
            var periodTimeSpan = TimeSpan.FromSeconds(10);

            var timer = new System.Threading.Timer(async (e) =>
            {

                var content = "initially empty";

                //query my router to give me its device list web portal page
                using var client = new HttpClient();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", properties.auth);
                try {
                    content = await client.GetStringAsync(properties.routerPortal);
                } catch(HttpRequestException e1) {
                    content = await client.GetStringAsync(properties.routerPortal);
                }

                //check if device list contains roommate's phone's MAC address
                currentlyConnected = content.Contains(properties.mac);

                //alert me of state changes using join api found here: https://joaoapps.com/join/api/
                //example api url: https://joinjoaomgcd.appspot.com/_ah/api/messaging/v1/sendPush?text=Arrived%20home&title=Roommate&deviceId={DEVICE-ID-HERE}&apikey={API-KEY-HERE}
                if((!previouslyConnected) && (currentlyConnected)){
                    Console.WriteLine("Roommate's Phone Connected");
                    var notifResponse = await phone.GetStringAsync(properties.arriveURL);
                }

                if((previouslyConnected) && (!currentlyConnected)){
                    Console.WriteLine("Roommate's Phone Disconnected");
                    var notifResponse = await phone.GetStringAsync(properties.departURL);
                }

                //update last known state
                previouslyConnected = currentlyConnected;
                
            }, null, startTimeSpan, periodTimeSpan);
            
            exitEvent.WaitOne();
        }
    }
}
