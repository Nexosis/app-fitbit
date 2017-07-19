using System;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Fitbit.Api.Portable;
using Fitbit.Api.Portable.OAuth2;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace NexosisFitbit.Model
{
    public class FitbitConnector
    {
        private readonly IOptions<Auth0Settings> auth0Config;
        private readonly IOptions<FitbitSettings> fitbitConfig;

        private class AccessTokenResponse
        {
            public string access_token { get; set; }
        }

        public FitbitConnector(IOptions<Auth0Settings> auth0Config, IOptions<FitbitSettings> fitbitConfig)
        {
            this.auth0Config = auth0Config;
            this.fitbitConfig = fitbitConfig;
        }

        public async Task<bool> CanConnect(ClaimsPrincipal user)
        {
            try
            {
                var client = await Connect(user);
                await client.GetDayActivityAsync(DateTime.Today, "-");
                return true;
            }
            catch
            {
                return false;
            }
        }
        
        
        public async Task<IFitbitClient> Connect(ClaimsPrincipal user)
        {
            var nameClaim = user.Claims.FirstOrDefault(i =>
                i.Type.Equals(@"http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier"));

            var ac2 = await GetAccessToken();
            var managementApi =
                new Auth0.ManagementApi.ManagementApiClient(ac2.access_token, auth0Config.Value.Domain);

            var fitbitUser = await managementApi.Users.GetAsync(nameClaim.Value);
            var fitbitId = fitbitUser.Identities[0];


            var client = new FitbitClient(
                    new FitbitAppCredentials
                    {
                        ClientId = fitbitConfig.Value.ClientId,
                        ClientSecret = fitbitConfig.Value.ClientSecret
                    },
                    new OAuth2AccessToken()
                    {
                        Token = fitbitId.AccessToken,
                        TokenType = "Bearer",
                        UserId = fitbitId.UserId
                    });

            return client;
        }

        private async Task<AccessTokenResponse> GetAccessToken()
        {
            var client = new HttpClient();
            var body = string.Format(
                "{{\"grant_type\":\"client_credentials\",\"client_id\": \"{0}\",\"client_secret\": \"{1}\",\"audience\": \"https://{2}/api/v2/\"}}",
                auth0Config.Value.ClientId, auth0Config.Value.ClientSecret, auth0Config.Value.Domain);
            var response = await client.PostAsync($"https://{auth0Config.Value.Domain}/oauth/token",
                new StringContent(body, Encoding.UTF8, "application/json"));

            var json = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<AccessTokenResponse>(json);
        }
    }
}