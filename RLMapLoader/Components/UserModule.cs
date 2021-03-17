using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Firebase.Auth;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Util;
using Google.Apis.Util.Store;
using Google.Cloud.Firestore;
using Microsoft.Extensions.Configuration;
using RLMapLoader.Components.Core;
using RLMapLoader.Components.Core.Constants;
using RLMapLoader.Components.Helpers.Extensions;

namespace RLMapLoader.Components
{
    //TODO:Load secrets from Google KMS
    public class UserModule : Component 
    {
        

        private static UserCredential _gOCred;
        private static FirebaseAuthLink _fbAuth;
        
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
        /// <summary>
        /// Make sure IsActive!
        /// </summary>
        public RLMLUser UserModel { get; set; }
        public DocumentReference UserReference { get; set; }
        private FirestoreDb _db;


        public bool IsActive => _fbAuth != null;

       
        public async Task<bool> InitializeAsync()
        {
            try
            {
                _db = await FirestoreDb.CreateAsync(GlobalConstants.G_PROJ_NAME);
                //Debug
                _logger.LogDebug($"established link with DB " + _db.ProjectId);
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
                    ClientId = Config["Auth:GOauthWebCID"],
                    ClientSecret = Config["Auth:GOauthWebCSec"]
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

            var authProvider = new FirebaseAuthProvider(new FirebaseConfig(Config["Auth:FbseKey"]));
            _fbAuth = await authProvider.SignInWithOAuthAsync(FirebaseAuthType.Google, _gOCred.Token.AccessToken);

            if (_db == null)
            {
                WaitDB();
            }
            //Load user profile 
            var userRes = _db.Collection("users").Document(_fbAuth.User.LocalId);
            UserReference = userRes;
            var user = await userRes.GetSnapshotAsync();
            UserModel = user.ToUserModel();
        }

        private void WaitDB()
        {
            _logger.LogInfo("Waiting for DB link...");
            var waitCount = 0;
            while (_db == null && waitCount < GlobalConstants.MASTER_DB_WAIT_COUNT)
            {
                Thread.Sleep(100);
                waitCount++;
                if(waitCount == GlobalConstants.MASTER_DB_WAIT_COUNT) throw new TimeoutException("Could not establish DB link!");
            }
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
                UserModel = null;
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