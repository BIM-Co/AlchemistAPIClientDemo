namespace ConsoleApp1.SDK
{
    public class EnvironmentManager : IEnvironmentManager
    {
        private BCEnvironment _currentEnvironment = BCEnvironment.PROD;

        public EnvironmentManager()
        {
        }

        public BCEnvironment CurrentEnvironment => _currentEnvironment;

        public void SetEnvironment(BCEnvironment environment)
        {
            _currentEnvironment = environment;
        }

        public string GetSSOUrl()
        {
            string url;
            switch (_currentEnvironment)
            {
                case BCEnvironment.PROD:
                default:
                    url = "https://auth.bimandco.com";
                    break;
                case BCEnvironment.RC:
                    url = "https://auth-rc.bimandco.com";
                    break;
                case BCEnvironment.DEV:
                case BCEnvironment.PREPROD:
                    url = "https://preprodauth.bimandco.com";
                    break;
                case BCEnvironment.TEST:
                    url = "https://testauth.bimandco.com";
                    break;

            }
            return url;
        }

        public string GetAlchemistApiUrl()
        {
            string url;
            switch (_currentEnvironment)
            {
                case BCEnvironment.PROD:
                default:
                    url = "https://api.alchemist-app.io";
                    break;
                case BCEnvironment.RC:
                    url = "https://api-rc.alchemist-app.io";
                    break;
                case BCEnvironment.DEV:
                    url = "https://localhost:7007";
                    break;

            }
            return url;
        }

        public string GetPlatformApiUrl()
        {
            string url;
            switch (_currentEnvironment)
            {
                case BCEnvironment.PROD:
                default:
                    url = "https://www.bimandco.com/api/";
                    break;
                case BCEnvironment.RC:
                    url = "https://www-rc.bimandco.com/api/";
                    break;
                case BCEnvironment.DEV:
                    url = "https://dev.bimandco.com/api/";
                    break;
                case BCEnvironment.PREPROD:
                    url = "https://preprod.bimandco.com/api/";
                    break;
                case BCEnvironment.TEST:
                    url = "https://test.bimandco.com/api/";
                    break;

            }
            return url;
        }
    }


    public enum BCEnvironment
    {
        DEV = 0,
        PROD = 2,
        RC = 4,
        PREPROD = 8,
        TEST = 16
    }
}
