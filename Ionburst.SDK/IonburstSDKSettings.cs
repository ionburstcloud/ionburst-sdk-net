// Copyright Ionburst Limited 2019
using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
using Microsoft.Extensions.Configuration.EnvironmentVariables;

namespace Ionburst.SDK
{
    public class IonburstSDKSettings
    {
        public string JWT { get; set; }
        public bool JWTAssigned { get; set; }
        public string IonburstProfile { get; set; }
        public string IonburstId { get; set; }
        public string IonburstKey { get; set; }
        public string IonburstUri { get; set; }
        public bool CredentialsSet { get; set; }
        public DateTime JWTUpdateTime { get; set; }
        public bool TraceCredentialsFile { get; set; }
        public long DefaultChunkSize { get; set; }
        public string ProfilesLocation { get; set; }
        public IConfiguration ExternalConfiguration { get; set; }
        public string ManifestCaptureDir { get; set; }

        private IConfiguration _configuration { get; set; }

        public IonburstSDKSettings(bool usingBuilder = false)
        {
            if (!usingBuilder)
            {
                BuildConfiguation();
                BuildIonburstSDKSettings();
            }
        }

        public IonburstSDKSettings(IConfiguration externalConfiguration)
        {
            BuildConfiguation(externalConfiguration);
            BuildIonburstSDKSettings();
        }

        private void BuildConfiguation(IConfiguration externalConfiguration = null)
        {
            string environmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            if (externalConfiguration != null)
            {
                _configuration = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                    .AddJsonFile($"appsettings.{environmentName}.json", optional: true)
                    .AddEnvironmentVariables()
                    .AddConfiguration(externalConfiguration)
                    .Build();
            }
            else
            {
                _configuration = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                    .AddJsonFile($"appsettings.{environmentName}.json", optional: true)
                    .AddEnvironmentVariables()
                    .Build();
            }
        }

        public void BuildConfiguationFromBuilder()
        {
            string environmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            if (ExternalConfiguration != null)
            {
                _configuration = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                    .AddJsonFile($"appsettings.{environmentName}.json", optional: true)
                    .AddEnvironmentVariables()
                    .AddConfiguration(ExternalConfiguration)
                    .Build();
            }
            else
            {
                _configuration = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                    .AddJsonFile($"appsettings.{environmentName}.json", optional: true)
                    .AddEnvironmentVariables()
                    .Build();
            }
        }

        public void BuildIonburstSDKSettings()
        {
            try
            {
                if (_configuration["Ionburst:TraceCredentialsFile"].ToUpper() == "ON")
                {
                    TraceCredentialsFile = true;
                }
            }
            catch (Exception)
            {
                // Swallow
            }

            try
            {
                DefaultChunkSize = Convert.ToInt64(_configuration["Ionburst:DefaultChunkSize"]);
            }
            catch (Exception)
            {
                // Swallow
            }

            try
            {
                ProfilesLocation = _configuration["Ionburst:ProfilesLocation"];
            }
            catch (Exception)
            {
                // Swallow
            }

            if (string.IsNullOrEmpty(IonburstUri))
            {
                IonburstUri = _configuration["IONBURST_URI"];
                if (string.IsNullOrEmpty(IonburstUri))
                {
                    IonburstUri = _configuration["Ionburst:IonburstUri"];
                    if (string.IsNullOrEmpty(IonburstUri))
                    {
                        IonburstUri = _configuration["Ionburst:IonBurstUri"];
                    }
                }
            }

            ManifestCaptureDir = _configuration["Ionburst:ManifestCaptureDir"];

            CredentialsSet = false;
            EstablishCredentials(_configuration);
            if (!CredentialsSet)
            {
                throw new IonburstCredentialsUndefinedException("Ionburst SDK was not able to establish credentials");
            }
        }

        private void EstablishCredentials(IConfiguration configuration)
        {
            // Try environment variables first (if they haven't already been set by builder)
            if (string.IsNullOrEmpty(IonburstId))
            {
                IonburstId = configuration["IONBURST_ID"];
            }
            if (string.IsNullOrEmpty(IonburstKey))
            {
                IonburstKey = configuration["IONBURST_KEY"];
            }
            if (!string.IsNullOrEmpty(IonburstId) && !string.IsNullOrEmpty(IonburstKey))
            {
                CredentialsSet = true;
            }

            if (!CredentialsSet)
            {
                ReadCredentials(configuration);
            }
        }

        private void ReadCredentials(IConfiguration configuration)
        {
            string defaultProfileId = string.Empty;
            string defaultProfileKey = string.Empty;
            string defaultProfileUri = string.Empty;
            bool profileDefined = false;

            if (TraceCredentialsFile)
            {
                Console.WriteLine("Start of credentials file parse");
            }
            string profile = IonburstProfile;
            if (string.IsNullOrEmpty(profile))
            {
                profile = configuration["Ionburst:Profile"];
            }
            if (profile != null)
            {
                profile = profile.Trim();
                profileDefined = true;
            }
            if (TraceCredentialsFile)
            {
                if (profileDefined)
                {
                    Console.WriteLine($"Credentials file parse: profile={profile}");
                }
                else
                {
                    Console.WriteLine("No Ionburst profile supplied for credentials file scan, looking only for default");
                }
            }

            try
            {
                IonburstId = string.Empty;
                IonburstKey = string.Empty;
                string credentialsFileName = string.Empty;
                if (configuration["HOME"] != null)
                {
                    // Smells like Linux
                    credentialsFileName = $"{configuration["HOME"]}/.ionburst/credentials";
                }
                if (configuration["HOMEDRIVE"] != null && configuration["HOMEPATH"] != null)
                {
                    // Smells like Windows
                    credentialsFileName = $"{configuration["HOMEDRIVE"]}{configuration["HOMEPATH"]}\\.ionburst\\Credentials";
                }
                if (ProfilesLocation != null && ProfilesLocation != string.Empty)
                {
                    // Use the specified one
                    credentialsFileName = ProfilesLocation;
                }
                if (TraceCredentialsFile)
                {
                    Console.WriteLine($"Credentials file parse: file={credentialsFileName}");
                }
                if (credentialsFileName != string.Empty)
                {
                    using (StreamReader credentialsFileReader = new StreamReader(credentialsFileName))
                    {
                        if (TraceCredentialsFile)
                        {
                            Console.WriteLine("Credentials file parse: stream reader established");
                        }
                        bool inCredentails = false;
                        bool inDefault = false;
                        string line;
                        bool sectionRead = false;
                        while (!sectionRead && (line = credentialsFileReader.ReadLine()) != null)
                        {
                            if (TraceCredentialsFile)
                            {
                                Console.WriteLine($"Credentials file parse: current line={line}");
                            }
                            if (line.Trim().StartsWith("["))
                            {
                                if (line.Trim().StartsWith($"[{profile}]"))
                                {
                                    if (inDefault)
                                    {
                                        inDefault = false;
                                    }
                                    inCredentails = true;
                                    if (TraceCredentialsFile)
                                    {
                                        Console.WriteLine($"Credentials file parse: found section matching {profile}");
                                    }
                                }
                                else if (line.Trim().StartsWith($"[default]"))
                                {
                                    if (inCredentails)
                                    {
                                        inCredentails = false;
                                    }
                                    inDefault = true;
                                    if (TraceCredentialsFile)
                                    {
                                        Console.WriteLine($"Credentials file parse: found [default] section");
                                    }
                                }
                                else
                                {
                                    if (inDefault)
                                    {
                                        inDefault = false;
                                    }
                                    if (inCredentails)
                                    {
                                        inCredentails = false;
                                        // We were in targeted section and now we're not so it has been fully read
                                        sectionRead = true;
                                    }
                                    if (TraceCredentialsFile)
                                    {
                                        Console.WriteLine($"Credentials file parse: ignoring section because it does not match {profile}");
                                    }
                                }
                            }
                            if (inCredentails || inDefault)
                            {
                                if (line.Trim().StartsWith("ionburst_id"))
                                {
                                    string[] pieces = line.Split('=');
                                    if (inCredentails)
                                    {
                                        IonburstId = pieces[1].Trim();
                                    }
                                    if (inDefault)
                                    {
                                        defaultProfileId = pieces[1].Trim();
                                    }
                                    if (TraceCredentialsFile)
                                    {
                                        Console.WriteLine($"Credentials file parse: set Id={IonburstId}");
                                    }
                                }
                                if (line.Trim().StartsWith("ionburst_key"))
                                {
                                    string[] pieces = line.Split('=');
                                    if (inCredentails)
                                    {
                                        IonburstKey = pieces[1].Trim();
                                    }
                                    if (inDefault)
                                    {
                                        defaultProfileKey = pieces[1].Trim();
                                    }
                                    if (TraceCredentialsFile)
                                    {
                                        Console.WriteLine($"Credentials file parse: set key={IonburstKey}");
                                    }
                                }
                                if (line.Trim().StartsWith("ionburst_uri"))
                                {
                                    string[] pieces = line.Split('=');
                                    if (inCredentails)
                                    {
                                        IonburstUri = pieces[1].Trim();
                                    }
                                    if (inDefault)
                                    {
                                        IonburstUri = pieces[1].Trim();
                                    }
                                    if (TraceCredentialsFile)
                                    {
                                        Console.WriteLine($"Credentials file parse: set uri={IonburstUri}");
                                    }
                                }
                            }
                            if (IonburstId != string.Empty && IonburstKey != string.Empty)
                            {
                                if (!CredentialsSet)
                                {
                                    if (TraceCredentialsFile)
                                    {
                                        Console.WriteLine("Credentials file parse: found both Id and Key; considered success");
                                    }
                                    CredentialsSet = true;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                if (TraceCredentialsFile)
                {
                    Console.WriteLine($"Credentials file parse: exceptiom reading credentials file: {e.Message}");
                }
                throw new IonburstCredentialsException("Failed to read Ionburst credentials", e);
            }
            
            if (!profileDefined)
            {
                // Did we get defaults?
                if (defaultProfileId != string.Empty && defaultProfileKey != string.Empty)
                {
                    IonburstId = defaultProfileId;
                    IonburstKey = defaultProfileKey;
                    IonburstUri = defaultProfileUri;
                    CredentialsSet = true;
                }
                else
                {
                    throw new ArgumentNullException(nameof(profile));
                }
            }
        }
    }
}
