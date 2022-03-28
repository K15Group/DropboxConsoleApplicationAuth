using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Dropbox.Api;
using Dropbox.Api.Files;

namespace DropboxApiIntegration
{
    internal class Program
    {
        private const string AppKey = "zfqeuqnz61da45m";
        private const string AppSecret = "3k7chm34wgaf914";
        private const string LoopbackHost = "http://localhost:8080/oauth2/";

        static void Main(string[] args)
        {
            var helper = new DropboxService(AppKey, AppSecret, LoopbackHost);

            var files = helper.ListFiles().Result;
            files.ToList().ForEach(f => Console.WriteLine(f));

            var search = helper.SeachFiles("Matches_temp.csv").Result.FirstOrDefault();
            Console.WriteLine(search);

            var download = helper.DownloadFile("Matches_temp.csv", "E:\\Temp\\Matches_temp.csv").Result;
            Console.WriteLine(download);

            Console.ReadKey();
        }
    }
}
