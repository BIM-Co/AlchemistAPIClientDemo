﻿namespace ConsoleApp1.SDK
{
    public interface IEnvironmentManager
    {

        public string GetSSOUrl();

        public string GetPlatformApiUrl();

        public string GetAlchemistApiUrl();

        public void SetEnvironment(BCEnvironment environment);

        public void SetSSOEnvironment(BCEnvironment environment);

        public void SetAlchemistEnvironment(BCEnvironment environment);

        public BCEnvironment CurrentEnvironment { get; }

    }
}