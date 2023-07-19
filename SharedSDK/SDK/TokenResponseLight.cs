using IdentityModel.Client;
using System;
using System.Collections.Generic;
using System.Text;

namespace ConsoleApp1.SDK
{
    public class TokenResponseLight
    {
        public TokenResponseLight()
        {
        }

        public TokenResponseLight(TokenResponse tokenResponse)
        {
            RefreshToken = tokenResponse.RefreshToken;
            AccessToken = tokenResponse.AccessToken;
            ExpiresAt = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn - 60); 
        }

        public string RefreshToken { get; set; }
        public string AccessToken { get; set; }
        public DateTime ExpiresAt { get; set; }

    }
}
