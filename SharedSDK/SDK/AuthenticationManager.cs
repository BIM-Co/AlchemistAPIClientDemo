using System.ComponentModel;
using System.Runtime.CompilerServices;
using IdentityModel.Client;
using System.IdentityModel.Tokens.Jwt;

namespace ConsoleApp1.SDK
{

    public class AuthenticationManager : INotifyPropertyChanged, IAuthenticationManager
    {
        /// <summary>
        /// Url de l'autorité qui délivre le token
        /// </summary>
        private static TokenResponseLight _tokenResponse = null;
     
        private readonly HttpClient _httpClient;

        private static readonly SemaphoreSlim semaphoreSlim = new(1, 1);

        private readonly IEnvironmentManager _environmentManager;

        public event PropertyChangedEventHandler PropertyChanged = delegate { };

        public bool Initialized { get; private set; }


        public AuthenticationManager(HttpClient httpClient, IEnvironmentManager environmentManager)
        {
            _httpClient = httpClient;
            _environmentManager = environmentManager;
        }

        public async Task Initialize()
        {
            Initialized = true;
        }

        public bool IsAuthenticated => Initialized && _tokenResponse != null;

        public string GetUserName()
        {
            if(_tokenResponse == null || string.IsNullOrEmpty(_tokenResponse.AccessToken))
            {
                return null;
            }

            var handler = new JwtSecurityTokenHandler();
            var jwtSecurityToken = handler.ReadJwtToken(_tokenResponse.AccessToken);
            return jwtSecurityToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Name)?.Value;
        }

        public string GetUserEmail()
        {
            if (_tokenResponse == null || string.IsNullOrEmpty(_tokenResponse.AccessToken))
            {
                return null;
            }
            var handler = new JwtSecurityTokenHandler();
            var jwtSecurityToken = handler.ReadJwtToken(_tokenResponse.AccessToken);
            return jwtSecurityToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Email)?.Value;
        }

        public string GetUserGuid()
        {
            if (_tokenResponse == null || string.IsNullOrEmpty(_tokenResponse.AccessToken))
            {
                return null;
            }
            var handler = new JwtSecurityTokenHandler();
            var jwtSecurityToken = handler.ReadJwtToken(_tokenResponse.AccessToken);
            return jwtSecurityToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub)?.Value;
        }

        public async Task<string> GetAccessToken()
        {
            if (!IsValidToken(_tokenResponse) && IsAuthenticated)
            {
                await RefreshToken(_tokenResponse.RefreshToken);
            }

            return _tokenResponse?.AccessToken;
        }

        public async Task<bool> GetAuthenticationTokenWithCredentialAsync(string userName, string password)
        {
            bool previousState = IsAuthenticated;

            try
            {
                await semaphoreSlim.WaitAsync();


                TokenResponse responseToken = await _httpClient.RequestPasswordTokenAsync(new PasswordTokenRequest()
                {
                    Address = _environmentManager.GetSSOUrl() + "/api/connect/token",
                    ClientId = Constants.ClientId,
                    GrantType = "password",
                    UserName = userName,
                    Password = password,
                    Scope = "openid profile email offline_access bim.api.onfly"

                });

                if (!(responseToken.Exception is HttpRequestException))
                {
                    if (!string.IsNullOrEmpty(responseToken.Error))
                    {
                        _tokenResponse = null;
                    }
                    else
                    {
                        _tokenResponse = new TokenResponseLight(responseToken);
                    }
                }

            }
            catch (Exception)
            {
                _tokenResponse = null;
            }
            finally
            {
                semaphoreSlim.Release();
                if (previousState != IsAuthenticated)
                {
                    OnPropertyChanged("IsAuthenticated");
                }
            }

            return IsAuthenticated;
        }

        private async Task RefreshToken(string refreshToken)
        {
            if (string.IsNullOrEmpty(refreshToken))
            {
                return;
            }

            bool previousState = IsAuthenticated;

            try
            {
                await semaphoreSlim.WaitAsync();


                TokenResponse responseToken = await _httpClient.RequestRefreshTokenAsync(new RefreshTokenRequest()
                {
                    Address = _environmentManager.GetSSOUrl() + "/api/connect/token",
                    ClientId = Constants.ClientId,
                    GrantType = "refresh_token",
                    RefreshToken = refreshToken,
                });

                if(!(responseToken.Exception is HttpRequestException))
                {
                    if (!string.IsNullOrEmpty(responseToken.Error))
                    {
                        _tokenResponse = null;
                    }
                    else
                    {
                        _tokenResponse = new TokenResponseLight(responseToken);
                    }
                }
               
            }
            catch (Exception)
            {
                _tokenResponse = null;
            }
            finally
            {
                semaphoreSlim.Release();
                if (previousState != IsAuthenticated)
                {
                    OnPropertyChanged("IsAuthenticated");
                }
            }
        }

        private bool IsValidToken(TokenResponseLight tokenResponse)
        => tokenResponse != null && tokenResponse.ExpiresAt > DateTime.UtcNow;

        public void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            // Raise the PropertyChanged event, passing the name of the property whose value has changed.
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public async void Logout()
        {
            _tokenResponse = null;
            OnPropertyChanged("IsAuthenticated");
        }

    }
}
