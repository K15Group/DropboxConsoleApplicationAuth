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
        private const string AppKey = "e3ghrchxlntamu0";
        private const string AppSecret = "cx8ycmlqov11ut6";
        private const string LoopbackHost = "http://localhost:52475/oauth2/";

        static void Main(string[] args)
        {
            string refreshToken = "AT6W_GWCO4QAAAAAAAAAAVsnXLsb8rtKFGb-nNwe0zsqOtePgBCTuj1JXFISUrMx";
            var helper = new DropboxService(AppKey, AppSecret, LoopbackHost, refreshToken);
            //var helper = new DropboxService(AppKey, AppSecret, LoopbackHost);

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
