using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PhotinoNET;

namespace BlazorBrowserNativeStyle
{
    public class Program
    {
        [STAThread]
        public static int Main(string[] args)
        {
            return MainImpl(args).Result;
        }
        
        public static async Task<int> MainImpl(string[] args)
        {
            var host = CreateHostBuilder(args).Build();
            var applicationLifetime = host.Services.GetService(typeof(IHostApplicationLifetime)) as IHostApplicationLifetime;

            //[Special for DesktopLoveBlazorWeb]
            TaskCompletionSource<string> futureAddr=new TaskCompletionSource<string>();
            applicationLifetime?.ApplicationStarted.Register((futureAddrObj) =>
            {
                var server = host.Services.GetService(typeof(IServer)) as IServer;
                var logger=host.Services.GetService(typeof(ILogger<Program>)) as ILogger<Program>;

                var addressFeature = server.Features.Get<IServerAddressesFeature>();
                foreach (var addresses in addressFeature.Addresses)
                {
                    logger.LogInformation("Listening on address: " + addresses);
                }

                var addr = addressFeature.Addresses.First();
                (futureAddrObj as TaskCompletionSource<string>).SetResult(addr);
            }, futureAddr);

            //[Special for DesktopLoveBlazorWeb]
            #pragma warning disable CS4014 // ���ڴ˵��ò���ȴ�������ڵ������ǰ������ִ�е�ǰ����
            host.RunAsync();
            #pragma warning restore CS4014 // ���ڴ˵��ò���ȴ�������ڵ������ǰ������ִ�е�ǰ����

            //[Special for DesktopLoveBlazorWeb]
            OpenInLine(await futureAddr.Task);
            
            //TODO
            return 0;
        }

        public static void OpenInLine(string address)
        {
            Console.WriteLine($"Try to view blazor application on {address}");
            string windowTitle = "Cross Desktop Love Blazor Web";

            var window = new PhotinoWindow()
                .SetTitle(windowTitle)
                .SetUseOsDefaultSize(false)
                .SetSize(1024, 768)
                .SetResizable(true)
                .Center()
                .RegisterWebMessageReceivedHandler((object sender, string message) =>
                {
                    var window = (PhotinoWindow) sender;

                    // The message argument is coming in from sendMessage.
                    // "window.external.sendMessage(message: string)"
                    string response = $"Received message: \"{message}\"";

                    // Send a message back the to JavaScript event handler.
                    // "window.external.receiveMessage(callback: Function)"
                    window.SendWebMessage(response);
                }) 
                .RegisterCustomSchemeHandler("app", (object sender, string scheme, string url, out string contentType) =>
                {
                    contentType = "text/javascript";
                    Console.WriteLine($"Received custom scheme request: {scheme} {url}");
                    return new MemoryStream(Encoding.UTF8.GetBytes(@"
                        (() =>{
                            window.setTimeout(() => {
                                alert(`🎉 Dynamically inserted JavaScript.`);
                            }, 1000);
                        })();
                    "));
                })
                .RegisterWindowCreatingHandler((object sender, EventArgs args) =>
                {
                    var window = (PhotinoWindow)sender; // Instance is not initialized at this point. Class properties are not set yet.
                    Console.WriteLine($"Creating new PhotinoWindow instance.");
                })
                .RegisterWindowCreatedHandler((object sender, EventArgs args) =>
                {
                    var window = (PhotinoWindow)sender; // Instance is initialized. Class properties are now set and can be used.
                    Console.WriteLine($"Created new PhotinoWindow instance with title {window.Title}.");
                });

            window.Load(address);
            window.WaitForClose(); // Starts the application event loop
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    //[Special for DesktopLoveBlazorWeb]
                    //use any available port on localhost
                    webBuilder.ConfigureKestrel(serverOptions =>
                    {
                        serverOptions.Listen(IPAddress.Loopback, 0);
                    });
                    webBuilder.UseStartup<Startup>();
                });
    }
}
