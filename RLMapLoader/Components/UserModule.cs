using System;
using System.Threading;
using System.Threading.Tasks;
using Firebase.Auth;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Util;
using Google.Apis.Util.Store;
using Microsoft.Extensions.Configuration;
using RLMapLoader.Components.Core;

namespace RLMapLoader.Components
{
    //TODO:Load secrets from Google KMS
    public class UserModule : Component 
    {
        private IConfigurationRoot _configuration;

        private static UserCredential _gOCred;
        private FirebaseAuthLink _fbAuth;
        public FirebaseAuthLink AuthProfile
        {
            get
            {
                if (_fbAuth == null)
                {
                    return null;
                }
                if (_fbAuth.IsExpired())
                {
                    _fbAuth.GetFreshAuthAsync();
                }

                return _fbAuth;
            }
        }

        public bool IsActive => _fbAuth != null;

        public UserModule()
        {
            _configuration  = new ConfigurationBuilder()
                .AddUserSecrets(typeof(Program).Assembly)
                .Build();
        }

        public async Task<bool> InitializeAsync()
        {
            try
            {
                await GetGoogleOauthTokenAsync();
                await LoginToRLMLAsync();
            }
            catch(Exception e)
            {
                //TODO:maybe only log this in developer mode?
                _logger.LogError("Encountered problem initializing User context.", e);
                return false;
            }
          
            return true;
        }
        private async Task GetGoogleOauthTokenAsync()
        {
            if (_gOCred != null)
            {
                _logger.LogWarning("Existing Google Auth context!");

                return;
            }
            var credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                new ClientSecrets
                {
                    ClientId = _configuration["Auth:GOauthWebCID"],
                    ClientSecret = _configuration["Auth:GOauthWebCSec"]
                },
                new[] { "https://www.googleapis.com/auth/userinfo.email" },
                "user",
                CancellationToken.None,
                new FileDataStore("RLML-BaseAccess"));
            if (_gOCred == null)
            {
                _gOCred = credential;
            }
            return;
        }
        private async Task LoginToRLMLAsync()
        {
            if (IsActive)
            {
                _logger.LogWarning("Already logged in.");
                return;
            }
            if (_gOCred == null)
            {
                _logger.LogError("No Google Auth context.");
                return;
            }

            if (_gOCred.Token.IsExpired(SystemClock.Default))
            {
                await _gOCred.RefreshTokenAsync(CancellationToken.None);
            }

            var authProvider = new FirebaseAuthProvider(new FirebaseConfig(_configuration["Auth:FbseKey"]));
            _fbAuth = await authProvider.SignInWithOAuthAsync(FirebaseAuthType.Google, _gOCred.Token.AccessToken);
            return;
        }


        public async Task<bool> LogoutAsync()
        {
            if (!IsActive)
            {
                _logger.LogError("User is not active!");
                return false;
            }

            try
            {
                await _gOCred.RevokeTokenAsync(CancellationToken.None);
                await _fbAuth.UnlinkFromAsync(FirebaseAuthType.Google);

                _gOCred = null;
                _fbAuth = null;
                return true;
            }
            catch (Exception e)
            {
                _logger.LogError("Could not complete logout, please check your connection/restart and try again", e);
                return false;
            }
        }
    }
}