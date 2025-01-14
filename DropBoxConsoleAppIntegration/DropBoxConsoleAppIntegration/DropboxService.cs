﻿using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Dropbox.Api;
using Dropbox.Api.Files;

namespace DropboxApiIntegration
{
    public class DropboxService
    {
        private readonly string appKey;
        private readonly string appSecret;
        private readonly DropboxClient client;

        private readonly string loopbackHost;
        private readonly Uri redirectUri;
        private readonly Uri jsRedirectUri;

        private string accessToken;
        private string refreshToken;

        #region Constructure
        /// <summary>
        /// Using ApiKey and OAuth 2 redirect URI
        /// Note: The Redirect URIs should ending with /authorize
        /// Example: config on Dropbox: http://localhost:52475/oauth2/authorize  The "loopbackHost" must be http://localhost:52475/oauth2
        /// </summary>
        /// <param name="appKey">App key in dropbox</param>
        /// <param name="appSecret">App secret in dropbox</param>
        /// <param name="loopbackHost">Redirect URI without the ending authorize</param>
        public DropboxService(string appKey, string appSecret, string loopbackHost, string refreshToken = null)
        {
            this.appKey = appKey;
            this.appSecret = appSecret;
            this.loopbackHost = loopbackHost;
            this.refreshToken = refreshToken;
            redirectUri = new Uri(loopbackHost + "authorize");
            jsRedirectUri = new Uri(loopbackHost + "token");

            RefreshAccessToken().Wait();

            client = new DropboxClient(accessToken);
        }
        /// <summary>
        /// Using predefine access token
        /// </summary>
        /// <param name="accessToken"></param>

        public DropboxService(string accessToken)
        {
            this.accessToken = accessToken;
            client = new DropboxClient(accessToken);
        }
        #endregion

        #region Functions
        public async Task<IEnumerable<string>> ListFiles()
        {
            var items = await client.Files.ListFolderAsync(string.Empty, true);
            return items.Entries
                .Where(i => i.IsFile)
                .Select(i => i.PathDisplay);
        }

        public async Task<IEnumerable<string>> SeachFiles(string fileName)
        {
            var items = await client.Files.ListFolderAsync(string.Empty, true);
            return items.Entries
                .Where(i => i.IsFile && i.Name.Equals(fileName, StringComparison.InvariantCultureIgnoreCase))
                .Select(i => i.PathDisplay);
        }

        public async Task<string> DownloadFile(string fileName, string localPath)
        {
            var files = await SeachFiles(fileName);
            if (files == null || !files.Any())
                throw new FileNotFoundException();
            var filePath = files.First();

            using (var response = await client.Files.DownloadAsync(filePath))
            {
                var contentStream = await response.GetContentAsStreamAsync();
                using (FileStream fileStream = File.Create(localPath))
                {
                    contentStream.CopyTo(fileStream);
                }
            }
            return localPath;
        }
        #endregion

        #region Get Access Token
        private async Task HandleOAuth2Redirect(HttpListener http)
        {
            var context = await http.GetContextAsync();

            // We only care about request to RedirectUri endpoint.
            while (context.Request.Url.AbsolutePath != redirectUri.AbsolutePath)
            {
                context = await http.GetContextAsync();
            }

            string responseString = $"<html>\n" +
                $"<body onload=\"redirect()\"/>\n" +
                $"<script>\n" +
                $"function redirect() {{\n" +
                $"document.location.href = \" /oauth2/token?url_with_fragment=\" + encodeURIComponent(document.location.href);\n" +
                $"}}\n" +
                $"</script>\n" +
                $"</html>\n";
            byte[] buffer = Encoding.UTF8.GetBytes(responseString);

            var response = context.Response;
            response.ContentLength64 = buffer.Length;
            var output = response.OutputStream;
            output.Write(buffer, 0, buffer.Length);
            output.Close();
        }

        private async Task<OAuth2Response> HandleJSRedirect(HttpListener http, string state)
        {
            var context = await http.GetContextAsync();

            // We only care about request to TokenRedirectUri endpoint.
            while (context.Request.Url.AbsolutePath != jsRedirectUri.AbsolutePath)
            {
                context = await http.GetContextAsync();
            }

            var redirectUri = new Uri(context.Request.QueryString["url_with_fragment"]);

            var tokenResult = await DropboxOAuth2Helper.ProcessCodeFlowAsync(redirectUri, appKey, appSecret, redirectUri: this.redirectUri.ToString(), state: state);

            string responseString = $"Get accessToken successfully";
            byte[] buffer = Encoding.UTF8.GetBytes(responseString);

            var response = context.Response;
            response.ContentLength64 = buffer.Length;
            var output = response.OutputStream;
            output.Write(buffer, 0, buffer.Length);
            output.Close();

            return tokenResult;

        }

        private async Task GetAccessToken()
        {
            var state = Guid.NewGuid().ToString("N");

            var authorizeUri = DropboxOAuth2Helper.GetAuthorizeUri(OAuthResponseType.Code, appKey, redirectUri, state: state, tokenAccessType: TokenAccessType.Offline);
            var http = new HttpListener();
            http.Prefixes.Add(loopbackHost);
            http.Start();
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(authorizeUri.ToString()) { UseShellExecute = true });

            // Handle OAuth redirect and send URL fragment to local server using JS.
            await HandleOAuth2Redirect(http);
            var result = await HandleJSRedirect(http, state);
            http.Close();

            accessToken = result.AccessToken;
            refreshToken = result.RefreshToken;
        }

        private async Task RefreshAccessToken()
        {
            if (string.IsNullOrEmpty(refreshToken))
            {
                await GetAccessToken();
                return;
            }

            var url = "https://api.dropbox.com/oauth2/token";
            var http = new HttpClient();
            http.BaseAddress = new Uri(url);

            var parameters = new Dictionary<string, string>
                {
                    { "refresh_token", refreshToken },
                    { "grant_type", "refresh_token" }
                };
            var bodyContent = new FormUrlEncodedContent(parameters);

            var authenticationString = $"{appKey}:{appSecret}";
            var base64EncodedAuthenticationString = Convert.ToBase64String(System.Text.ASCIIEncoding.ASCII.GetBytes(authenticationString));
            http.DefaultRequestHeaders.Add("Authorization", $"Basic {base64EncodedAuthenticationString}");

            var response = await http.PostAsync(url, bodyContent);
            response.EnsureSuccessStatusCode();

            string responseBody = await response.Content.ReadAsStringAsync();
            dynamic content = JsonSerializer.Deserialize<ExpandoObject>(responseBody);
            accessToken = content.access_token.ToString();
        }
        #endregion
    }
}
