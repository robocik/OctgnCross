﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web;

using Octgn.Site.Api.Models;
using System.Threading.Tasks;
using Octgn.Online.Hosting;
using System.Threading;
using Octgn.Online.Api.Models;

namespace Octgn.Site.Api
{
    public class ApiClient
    {
        public static Uri DefaultUrl = new Uri("https://www.Octgn.net");

        public Uri Url {
            get => _client.BaseAddress;
            set {
                if (value == _client.BaseAddress) return;

                _client.BaseAddress = value;
            }
        }

        internal HttpClient Client => _client;

        private static readonly HttpClient _client = new HttpClient(new HttpClientHandler() 
        { 
            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        });

        static ApiClient() {
            _client.DefaultRequestHeaders.Accept.Add(
                    new MediaTypeWithQualityHeaderValue("application/json"));
            
        }

        public ApiClient() {
            Url = DefaultUrl;
        }

        public async Task<IEnumerable<ApiUser>> UsersFromUserIds(IEnumerable<string> userIds, CancellationToken cancellationToken = default(CancellationToken)) {
            var client = Client;
            var resp = await client.PostAsJsonAsync("api/user/fromuserids", userIds, cancellationToken);

            if (!resp.IsSuccessStatusCode)
                throw ApiClientException.FromResponse(resp);

            return await resp.Content.ReadAsAsync<ApiUser[]>();
        }

        public async Task<ApiUser> UserFromUserId(string userId, CancellationToken cancellationToken = default(CancellationToken)) {
            return (await UsersFromUserIds(new[] { userId }, cancellationToken)).FirstOrDefault();
        }

        public LoginResult Login(string username, string password) {
            var client = Client;
            var resp =
                client.GetAsync(
                    "api/user/loginandusername?username2=" + HttpUtility.UrlEncode(username) + "&password2="
                    + HttpUtility.UrlEncode(password)).Result;
            return resp.Content.ReadAsAsync<LoginResult>().Result;
        }

        public ChangeEmailResult ChangeEmail(string username, string password, string newemail) {
            var client = Client;
            var resp =
                client.GetAsync(
                    "api/user/changeemail?username=" + HttpUtility.UrlEncode(username)
                    + "&password=" + HttpUtility.UrlEncode(password)
                    + "&newemail=" + HttpUtility.UrlEncode(newemail)).Result;
            return resp.Content.ReadAsAsync<ChangeEmailResult>().Result;
        }

        public async Task<CreateSessionResult> CreateSession(string username, string password, string deviceId) {
            var client = Client;

            var data = new CreateSession() {
                Username = username,
                Password = password,
                DeviceId = deviceId
            };

            var resp = await client.PostAsJsonAsync("api/sessions", data);

            if (!resp.IsSuccessStatusCode)
                throw ApiClientException.FromResponse(resp);

            var result = await resp.Content.ReadAsAsync<CreateSessionResult>();

            return result;
        }

        public async Task ClearSession(string userId, string deviceId, string sessionId) {
            var url = $"api/users/{userId}/devices/{deviceId}/session";

            var resp = await Client.PostAsJsonAsync(url, sessionId);

            if (!resp.IsSuccessStatusCode)
                throw ApiClientException.FromResponse(resp);
        }

        public async Task<bool> ValidateSession(string userId, string deviceId, string sessionId, CancellationToken cancellationToken = default(CancellationToken)) {
            var url = $"api/users/{userId}/devices/{deviceId}/session/validate";

            var resp = await Client.PutAsJsonAsync(url, sessionId, cancellationToken);

            if (resp.StatusCode == HttpStatusCode.NotFound) return false;

            if (!resp.IsSuccessStatusCode)
                throw ApiClientException.FromResponse(resp);

            var result = await resp.Content.ReadAsAsync<string>();

            if (result != "ok") {
                throw new ApiClientException($"Api call failed with an invalid response {result}");
            }

            return true;
        }

        public LoginResult GsmRegistration(string username, string password, string deviceType, string deviceName,
            string token) {
            var client = Client;
            const string urlFormat = "api/user/GsmRegistration?gsmregistrationusername={0}&gsmregistrationpassword={1}&deviceType={2}&deviceName={3}&pushToken={4}";
            var url = String.Format(urlFormat,
                HttpUtility.UrlEncode(username),
                HttpUtility.UrlEncode(password),
                HttpUtility.UrlEncode(deviceType),
                HttpUtility.UrlEncode(deviceName),
                HttpUtility.UrlEncode(token)
                );
            var resp = client.GetAsync(url).Result;
            return resp.Content.ReadAsAsync<LoginResult>().Result;
        }

        public IsSubbedResult IsSubbed(string username, string password) {
            var client = Client;
            var resp =
                client.GetAsync(
                    "api/user/issubbed?subusername=" + HttpUtility.UrlEncode(username)
                    + "&subpassword=" + HttpUtility.UrlEncode(password)).Result;
            return resp.Content.ReadAsAsync<IsSubbedResult>().Result;
        }

        public async Task<bool> IsGameServerRunning(string username, string password) {
            var client = Client;
            var resp = await
                client.GetAsync(
                    "api/servicestatus/gameserverrunning?username="
                    + HttpUtility.UrlEncode(username)
                    + "&password="
                    + HttpUtility.UrlEncode(password));

            if (!resp.IsSuccessStatusCode)
                throw ApiClientException.FromResponse(resp);

            return await resp.Content.ReadAsAsync<bool>();
        }

        public UserIconSetResult SetUserIcon(string username, string password, string fileExtension, Stream imageStream) {
            var client = Client;
            var resp =
                client.PutAsync(
                    "api/usericon/?username="
                    + HttpUtility.UrlEncode(username)
                    + "&password=" + HttpUtility.UrlEncode(password)
                    + "&fileextension=" + HttpUtility.UrlEncode(fileExtension)
                    , new StreamContent(imageStream)).Result;

            return resp.Content.ReadAsAsync<UserIconSetResult>().Result;
        }

        public int SubPercent() {
            var client = Client;
            var resp =
                client.GetAsync(
                    "api/stats/UsersOnlineNow?type="
                    + HttpUtility.UrlEncode(StatType.SubPercent.ToString()))
                    .Result;
            var istr = resp.Content.ReadAsStringAsync().Result;
            return int.Parse(istr.Trim());
        }

        public SharedDeckUploadResult ShareDeck(string username, string password, string name, Stream deckstream) {
            var client = Client;
            var resp =
                client.PutAsync(
                    "api/shareddeck/?username=" + HttpUtility.UrlEncode(username) + "&password="
                    + HttpUtility.UrlEncode(password) + "&name=" + HttpUtility.UrlEncode(name),
                    new StreamContent(deckstream)).Result;

            return resp.Content.ReadAsAsync<SharedDeckUploadResult>().Result;
        }

        public List<SharedDeckInfo> GetUsersSharedDecks(string username) {
            var client = Client;
            var resp = client.GetAsync("api/shareddeck/" + username).Result;
            if (!resp.IsSuccessStatusCode) {
                var content = resp.Content.ReadAsStringAsync().Result;
                throw new ApiClientException("GetUsersSharedDecks bad status code {resp.StatusCode}\n{content}");
            }
            var ret = resp.Content.ReadAsAsync<IEnumerable<SharedDeckInfo>>().Result.ToList();
            return ret;
        }

        public DeleteDeckResult DeleteSharedDeck(string username, string password, string name) {
            var client = Client;
            var resp =
                client.DeleteAsync(
                    "api/shareddeck/?username=" + HttpUtility.UrlEncode(username) + "&password="
                    + HttpUtility.UrlEncode(password) + "&name=" + HttpUtility.UrlEncode(name)).Result;

            return resp.Content.ReadAsAsync<DeleteDeckResult>().Result;
        }

        public void WebhookQueuePublish(WebhookEndpoint end, string message, string accessKey) {
            var client = Client;
            const string urlFormat = "Api/WebhookQueue/{0}?accessKey={1}";
            var url = string.Format(urlFormat, end, accessKey);
            var resp = client.PostAsync(url, new StringContent(message)).Result;
            if (!resp.IsSuccessStatusCode) {
                var content = resp.Content.ReadAsStringAsync().Result;
                throw new ApiClientException("UsersFromUsername bad status code {resp.StatusCode}\n{content}");
            }
        }

        public void CreateGameHistory(PutGameHistoryReq req) {
            var client = Client;
            var resp = client.PostAsJsonAsync("api/gamehistory/put", req).Result;
        }

        public void CompleteGameHistory(PutGameCompleteReq req) {
            var client = Client;
            var resp = client.PostAsJsonAsync("api/gamehistory/putgamecomplete", req).Result;
        }

        public async Task SetGameList(string apiKey, IEnumerable<HostedGame> games) {
            var client = Client;
            var model = new SetGameListRequest() {
                ApiKey = apiKey,
                Games = games.Sanitized().ToArray()
            };
            var resp = await client.PutAsJsonAsync("api/game", model);
            if(!resp.IsSuccessStatusCode)
                throw ApiClientException.FromResponse(resp);
        }

        public async Task<IEnumerable<HostedGame>> GetGameList() {
            var client = Client;
            var resp = await client.GetAsync("api/game");
            if (resp.IsSuccessStatusCode) {
                return await resp.Content.ReadAsAsync<IEnumerable<HostedGame>>(); ;
            }
            throw ApiClientException.FromResponse(resp);
        }

        public bool GameMessage(string apiKey, GameMessage message) {
            var client = Client;
            var resp = client.PutAsJsonAsync("api/Game/GameMessage?gameMessageApiKey=" + apiKey, message).Result;
            return resp.IsSuccessStatusCode;
        }

        public ReleaseInfo GetReleaseInfo() {
            var client = Client;
            var resp = client.GetAsync("api/octgn/releaseinfo").Result;
            return resp.Content.ReadAsAsync<ReleaseInfo>().Result;
        }

        public ReleaseInfo GetLatestRelease(Version currentVersion) {
            if (currentVersion == null)
                throw new ArgumentNullException(nameof(currentVersion));

            var client = Client;
            var resp = client.GetAsync("api/octgn/latestrelease?currentVersion=" + currentVersion).Result;
            return resp.Content.ReadAsAsync<ReleaseInfo>().Result;
        }

        public void ReportUser(string username, string password, ReportUserRequest request) {
            var client = Client;
            var resp =
                client.PostAsJsonAsync(
                    "api/user/report/?reportusername=" + HttpUtility.UrlEncode(username) + "&reportpassword="
                    + HttpUtility.UrlEncode(password), request).Result;

            resp.EnsureSuccessStatusCode();
        }
    }
}