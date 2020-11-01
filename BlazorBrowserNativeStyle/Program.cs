using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
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
using SharpWebview;
using SharpWebview.Content;

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
            #pragma warning disable CS4014 // 由于此调用不会等待，因此在调用完成前将继续执行当前方法
            host.RunAsync();
            #pragma warning restore CS4014 // 由于此调用不会等待，因此在调用完成前将继续执行当前方法

            //[Special for DesktopLoveBlazorWeb]
            OpenInLine(await futureAddr.Task);
        }

        public static void OpenInLine(string address)
        {
            using var webview = new Webview();

            webview
                .SetTitle($"Hello Asp.Net Blazor Server In Process of {address}")
                .Navigate(new UrlContent(address))
                .Run();
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
