using Google.Apis.Auth.OAuth2;
using Google.Apis.Calendar.v3;
using Google.Apis.Util.Store;

namespace Prima.Application.Scheduling.Calendar;

public static class GoogleApiAuth
{
    public static async Task<(UserCredential?, Exception?)> AuthorizeSafely(string secretPath, string tokenDirectory)
    {
        try
        {
            return (await Authorize(secretPath, tokenDirectory), null);
        }
        catch (Exception e)
        {
            return (null, e);
        }
    }

    public static async Task<UserCredential?> Authorize(string secretPath, string tokenDirectory)
    {
        var googleApiTokenStore = new FileDataStore(tokenDirectory, true);
        await using var secretStream = File.OpenRead(secretPath);
        // In a headless environment, this needs to generate a token locally which then needs to be copied
        // onto the server.
        var clientSecrets = await GoogleClientSecrets.FromStreamAsync(secretStream);
        var googleApiCredential = await GoogleWebAuthorizationBroker.AuthorizeAsync(clientSecrets.Secrets,
            new[] { CalendarService.Scope.CalendarEvents }, "Prima", CancellationToken.None, googleApiTokenStore);
        return googleApiCredential;
    }
}