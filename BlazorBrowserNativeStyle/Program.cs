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
        public static async Task Main(string[] args)
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
        }

        public static void OpenInLine(string address)
        {
            Console.WriteLine($"Try to view blazor application on {address}");
            /*using(var webview = new Webview())
            {
                webview
                    .SetTitle("Blazor in webview_csharp")             
                    .SetSize(1024, 768, WebviewHint.None)
                    .SetSize(800, 600, WebviewHint.Min)
                    .Navigate(new UrlContent(address))
                    .Run();
            }*/
                    {
            // Window title declared here for visibility
            string windowTitle = "Photino for .NET Demo App";

            // Define the PhotinoWindow options. Some handlers 
            // can only be registered before a PhotinoWindow instance
            // is initialized. Currently there are three handlers
            // that must be defined here.
            Action<PhotinoWindowOptions> windowConfiguration = options =>
            {
                // Custom scheme handlers can only be registered during
                // initialization of a PhotinoWindow instance.
                options.CustomSchemeHandlers.Add("app", (string url, out string contentType) =>
                {
                    contentType = "text/javascript";
                    return new MemoryStream(Encoding.UTF8.GetBytes(@"
                        (() =>{
                            window.setTimeout(() => {
                                alert(`🎉 Dynamically inserted JavaScript.`);
                            }, 1000);
                        })();
                    "));
                });

                // Window creating and created handlers can only be
                // registered during initialization of a PhotinoWindow instance.
                // These handlers are fired before and after the native constructor
                // method is called.
                options.WindowCreatingHandler += (object sender, EventArgs args) =>
                {
                    var window = (PhotinoWindow)sender; // Instance is not initialized at this point. Class properties are not set yet.
                    Console.WriteLine($"Creating new PhotinoWindow instance.");
                };

                options.WindowCreatedHandler += (object sender, EventArgs args) =>
                {
                    var window = (PhotinoWindow)sender; // Instance is initialized. Class properties are now set and can be used.
                    Console.WriteLine($"Created new PhotinoWindow instance with title {window.Title}.");
                };
            };

            // Creating a new PhotinoWindow instance with the fluent API
            var window = new PhotinoWindow(windowTitle, windowConfiguration)
                // Resize to a percentage of the main monitor work area
                .Resize(50, 50, "%")
                // Center window in the middle of the screen
                .Center()
                // Users can resize windows by default.
                // Let's make this one fixed instead.
                .UserCanResize(false)
                // Most event handlers can be registered after the
                // PhotinoWindow was instantiated by calling a registration 
                // method like the following RegisterWebMessageReceivedHandler.
                // This could be added in the PhotinoWindowOptions if preferred.
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
                //.Load("wwwroot/index.html"); // Can be used with relative path strings or "new URI()" instance to load a website.
                .Load(address);
            window.WaitForClose(); // Starts the application event loop
        }
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
