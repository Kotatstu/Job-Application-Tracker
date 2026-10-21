using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Gmail.v1;
using Google.Apis.Util.Store;

namespace backend.Services;

public static class GoogleFlowFactory
{
    public static GoogleAuthorizationCodeFlow Create(GoogleOptions options) =>
        new(new GoogleAuthorizationCodeFlow.Initializer
        {
            ClientSecrets = new ClientSecrets
            {
                ClientId = options.ClientId,
                ClientSecret = options.ClientSecret
            },
            Scopes = new[] { GmailService.Scope.GmailReadonly },
            Prompt = "consent",
            DataStore = new NullDataStore()
        });
}