﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Tacs20ImportClient.JsonObjects;

namespace Tacs20ImportClient
{
    public class ApiClient
    {
        private TokenResponse _currentToken;
        private DateTime _currentTokenExpiresAt;
        private readonly string _clientId = Credentials.ClientId;
        private readonly string _clientSecret = Credentials.ClientSecret;
        private readonly string _resource = Credentials.Resource;

        private async Task<TokenResponse> GetToken()
        {
            if (_currentToken == null || _currentTokenExpiresAt <= DateTime.UtcNow)
            {
                using (var httpClient = new HttpClient())
                {
                    httpClient.BaseAddress = new Uri("https://auth.cloudaccess.ch");

                    var formData = new FormUrlEncodedContent(new List<KeyValuePair<string, string>>
                    {
                        new KeyValuePair<string, string>("client_id", _clientId),
                        new KeyValuePair<string, string>("client_secret", _clientSecret),
                        new KeyValuePair<string, string>("grant_type", "client_credentials"),
                        new KeyValuePair<string, string>("scope", "code"),
                        new KeyValuePair<string, string>("resource", _resource)
                    });

                    HttpResponseMessage response = await httpClient.PostAsync("connect/token", formData);
                    string content = await response.Content.ReadAsStringAsync();

                    _currentToken = new TokenResponse(content);
                    _currentTokenExpiresAt = DateTime.UtcNow.AddSeconds(_currentToken.ExpiresIn);
                }
            }

            return _currentToken;
        }

        public async Task GetCompleteImport()
        {
            var tokenResponse = await GetToken();
            using (var client = GetClient(tokenResponse))
            {
                var responseMessage = await client.GetAsync("api/v1");
                if (responseMessage.IsSuccessStatusCode)
                {
                    string content = await responseMessage.Content.ReadAsStringAsync();
                    var result = JsonConvert.DeserializeObject<BaseNavigation>(content);
                    await SaveStatistikCode(result.StatistikCodeUrl);
                    await SaveNutzniesser(result.NutzniesserUrl);
                    await SavePersonalkategorie(result.PersonalkategorieUrl);

                }
            }
        }

        private async Task SavePersonalkategorie(string personalkategorieUrl)
        {
            var tokenResponse = await GetToken();
            using (var client = GetClient(tokenResponse))
            {
                var response = await client.GetAsync(personalkategorieUrl);
                if (response.IsSuccessStatusCode && response.StatusCode != HttpStatusCode.NoContent)
                {
                    string content = await response.Content.ReadAsStringAsync();
                    var result = JsonConvert.DeserializeObject<IEnumerable<Personalkategorie>>(content);
                    // here you can save the data
                }
            }
        }

        private async Task SaveNutzniesser(string nutzniesserUrl)
        {
            var tokenResponse = await GetToken();
            using (var client = GetClient(tokenResponse))
            {
                var response = await client.GetAsync(nutzniesserUrl);
                if (response.IsSuccessStatusCode && response.StatusCode != HttpStatusCode.NoContent)
                {
                    string content = await response.Content.ReadAsStringAsync();
                    var result = JsonConvert.DeserializeObject<IEnumerable<StatistikCodeImport>>(content);
                    // here you can save the data
                }
            }
        }

        private async Task SaveStatistikCode(string statistikCodeUrl)
        {
            var tokenResponse = await GetToken();
            using (var client = GetClient(tokenResponse))
            {
                var response = await client.GetAsync(statistikCodeUrl);
                if (response.IsSuccessStatusCode && response.StatusCode != HttpStatusCode.NoContent)
                {
                    string content = await response.Content.ReadAsStringAsync();
                    var result = JsonConvert.DeserializeObject<IEnumerable<Nutzniesser>>(content);
                    // here you can save the data
                }
            }
        }

        private HttpClient GetClient(TokenResponse token)
        {
            string accessToken = token.AccessToken;
            HttpClient client = new HttpClient();
            client.BaseAddress = new Uri("https://import.tacs.ch");

            var authHeader = new AuthenticationHeaderValue("Bearer", accessToken);
            client.DefaultRequestHeaders.Authorization = authHeader;

            var acceptHeader = new MediaTypeWithQualityHeaderValue("application/json");
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(acceptHeader);

            return client;
        }
    }
}
