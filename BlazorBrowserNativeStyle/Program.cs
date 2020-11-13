using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
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
using Microsoft.Web.WebView2.Wpf;

namespace BlazorBrowserNativeStyle
{
    public class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            System.Threading.SynchronizationContext.SetSynchronizationContext(new System.Windows.Threading.DispatcherSynchronizationContext());
            MainAsync(args).Wait();
        }

        public static async Task MainAsync(string[] args)
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
            #pragma warning disable CS4014 
            host.RunAsync();
            #pragma warning restore CS4014 
            
            //[Special for DesktopLoveBlazorWeb]
            OpenInLine(await futureAddr.Task);
        }

        public static void OpenInLine(string address)
        {          
            var selfIncludeWebView2DirName = "WebView2";
            var selfIncludeWebView2FullDirPath = Path.Combine(System.AppContext.BaseDirectory, selfIncludeWebView2DirName);

            new System.Windows.Application().Run(
                new System.Windows.Window
                {
                    Content = new WebView2
                    {
                        CreationProperties = new CoreWebView2CreationProperties
                        {
                            BrowserExecutableFolder = Directory.Exists(selfIncludeWebView2FullDirPath) ? selfIncludeWebView2FullDirPath: null
                        },
                        Source = new Uri(address)
                    }
                }
            );
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
