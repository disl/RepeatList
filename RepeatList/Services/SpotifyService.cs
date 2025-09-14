using SpotifyAPI.Web;

namespace RepeatList.Services
{


    public class SpotifyService
    {
        // developer.spotify.com/dashboard/applications 
        private readonly string _clientId = "79d0aca896f242759bf9c8b435075efd"; // Ersetze mit deiner Client ID
        private readonly string _redirectUri = "com.companyname.repeatlist://auth";  //"myapp://callback"; // Muss im Spotify Dashboard eingetragen sein
        private SpotifyClient _spotify;
        private PKCEAuthenticator _authenticator;

        public async Task AuthenticateAsync()
        {
            try
            {
                // PKCE-Verifier und Challenge generieren
                var pkce = PKCEUtil.GenerateCodes();

                // Auth-URL erstellen mit Scopes
                var loginRequest = new LoginRequest(new Uri(_redirectUri), _clientId, LoginRequest.ResponseType.Code)
                {
                    CodeChallenge = pkce.challenge,
                    CodeChallengeMethod = "S256",
                    Scope = new[] { Scopes.PlaylistModifyPublic, Scopes.PlaylistModifyPrivate, Scopes.UserReadPrivate }
                };

                // Browser öffnen für Spotify-Login
                var uri = loginRequest.ToUri();
                var authResult = await WebAuthenticator.AuthenticateAsync(new WebAuthenticatorOptions
                {
                    Url = uri,
                    CallbackUrl = new Uri(_redirectUri),
                    PrefersEphemeralWebBrowserSession = true // Private Session für mehr Sicherheit
                });

                // Authorization Code extrahieren
                if (authResult.Properties.TryGetValue("code", out var code))
                {
                    // Token abrufen
                    var oauthClient = new OAuthClient();
                    var tokenResponse = await oauthClient.RequestToken(
                        new PKCETokenRequest(_clientId, pkce.verifier, new Uri(_redirectUri), code)
                    );

                    _spotify = new SpotifyClient(tokenResponse.AccessToken);
                }
                else
                {
                    throw new Exception("Authentifizierung fehlgeschlagen: Kein Code empfangen");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Fehler bei der Authentifizierung: {ex.Message}", ex);
            }
        }

        public async Task RefreshTokenAsync()
        {
            if (_authenticator == null || _spotify == null)
                throw new Exception("Nicht authentifiziert");

            // PKCEAuthenticator hat keine RefreshToken-Methode.
            // Token-Refresh muss über OAuthClient und das RefreshToken erfolgen.
            // Hier ein Beispiel, wie das aussehen kann:

            // Annahme: Das RefreshToken ist im InitialToken gespeichert
            var refreshToken = _authenticator.InitialToken?.RefreshToken;
            if (string.IsNullOrEmpty(refreshToken))
                throw new Exception("Kein RefreshToken verfügbar");

            var oauthClient = new OAuthClient();
            var tokenResponse = await oauthClient.RequestToken(
                new PKCETokenRefreshRequest(_authenticator.ClientId, refreshToken)
            );

            _spotify = new SpotifyClient(tokenResponse.AccessToken);
        }

        public SpotifyClient Spotify => _spotify;

        public async Task<string> ImportToPlaylist(List<string> songLines, string playlistName = "Importierte Liste")
        {
            if (_spotify == null) throw new Exception("Nicht authentifiziert");

            try
            {
                // Tracks suchen
                var uris = new List<string>();
                foreach (var song in songLines)
                {
                    var searchRequest = new SearchRequest(SearchRequest.Types.Track, song)
                    {
                        Limit = 1
                    };
                    var search = await _spotify.Search.Item(searchRequest);
                    if (search.Tracks.Items.Any())
                    {
                        uris.Add(search.Tracks.Items.First().Uri);
                    }
                    await Task.Delay(100); // Rate-Limit vermeiden
                }

                // Benutzer-ID holen
                var me = await _spotify.UserProfile.Current();
                var createRequest = new PlaylistCreateRequest(playlistName)
                {
                    Public = false
                };
                var playlist = await _spotify.Playlists.Create(me.Id, createRequest);

                // Tracks hinzufügen (in Batches von max. 100)
                for (int i = 0; i < uris.Count; i += 100)
                {
                    var batch = uris.Skip(i).Take(100).ToList();
                    var addRequest = new PlaylistAddItemsRequest(batch);
                    await _spotify.Playlists.AddItems(playlist.Id, addRequest);
                }

                return playlist.Id;
            }
            catch (Exception ex)
            {
                throw new Exception($"Fehler beim Import: {ex.Message}", ex);
            }
        }
    }
}
