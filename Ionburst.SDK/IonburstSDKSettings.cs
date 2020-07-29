// Copyright Ionburst Limited 2019
using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.FileExtensions;
using Microsoft.Extensions.Configuration.Json;
using Microsoft.Extensions.Configuration.EnvironmentVariables;

namespace Ionburst.SDK
{
    public class IonburstSDKSettings
    {
        public string JWT { get; set; }
        public bool JWTAssigned { get; set; }
        public string IonBurstId { get; set; }
        public string IonBurstKey { get; set; }
        public string IonBurstUri { get; set; }
        public bool CredentialsSet { get; set; }
        public DateTime JWTUpdateTime { get; set; }
        public bool TraceCredentialsFile { get; set; }
        public string ProfilesLocation { get; set; }
        private IConfiguration _configuration { get; set; }

        public IonburstSDKSettings()
        {
            BuildConfiguation();
            BuildIonburstSDKSettings();
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
                ProfilesLocation = _configuration["Ionburst:ProfilesLocation"];
            }
            catch (Exception)
            {
                // Swallow
            }

            IonBurstUri = _configuration["IONBURST_URI"];
            if (IonBurstUri == null || IonBurstUri == string.Empty)
            {
                IonBurstUri = _configuration["Ionburst:IonBurstUri"];
            }

            CredentialsSet = false;
            EstablishCredentials(_configuration);
            if (!CredentialsSet)
            {
                throw new Exception("IonBurst SDK was not able to establish credentials");
            }
        }

        private void EstablishCredentials(IConfiguration configuration)
        {
            // Try environment variables first
            IonBurstId = configuration["IONBURST_ID"];
            IonBurstKey = configuration["IONBURST_KEY"];
            if (IonBurstId != null && IonBurstId != string.Empty && IonBurstKey != null && IonBurstKey != string.Empty)
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
            if (TraceCredentialsFile)
            {
                Console.WriteLine("Start of credentials file parse");
            }
            string profile = configuration["Ionburst:Profile"];
            if (TraceCredentialsFile)
            {
                Console.WriteLine($"Credentials file parse: profile={profile}");
            }
            if (profile != null)
            {
                profile = profile.Trim();
                try
                {
                    IonBurstId = string.Empty;
                    IonBurstKey = string.Empty;
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
                                        inCredentails = true;
                                        if (TraceCredentialsFile)
                                        {
                                            Console.WriteLine($"Credentials file parse: found section matching {profile}");
                                        }
                                    }
                                    else
                                    {
                                        if (inCredentails)
                                        {
                                            // We were in targeted section and now we're not so it has been fully read
                                            sectionRead = true;
                                        }
                                        inCredentails = false;
                                        if (TraceCredentialsFile)
                                        {
                                            Console.WriteLine($"Credentials file parse: ignoring section because it does not match {profile}");
                                        }
                                    }
                                }
                                if (inCredentails)
                                {
                                    if (line.Trim().StartsWith("ionburst_id"))
                                    {
                                        string[] pieces = line.Split('=');
                                        IonBurstId = pieces[1].Trim();
                                        if (TraceCredentialsFile)
                                        {
                                            Console.WriteLine($"Credentials file parse: set Id={IonBurstId}");
                                        }
                                    }
                                    if (line.Trim().StartsWith("ionburst_key"))
                                    {
                                        string[] pieces = line.Split('=');
                                        IonBurstKey = pieces[1].Trim();
                                        if (TraceCredentialsFile)
                                        {
                                            Console.WriteLine($"Credentials file parse: set key={IonBurstKey}");
                                        }
                                    }
                                    if (line.Trim().StartsWith("ionburst_uri"))
                                    {
                                        string[] pieces = line.Split('=');
                                        IonBurstUri = pieces[1].Trim();
                                        if (TraceCredentialsFile)
                                        {
                                            Console.WriteLine($"Credentials file parse: set uri={IonBurstUri}");
                                        }
                                    }
                                }
                                if (IonBurstId != string.Empty && IonBurstKey != string.Empty)
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
                catch (Exception e)
                {
                    if (TraceCredentialsFile)
                    {
                        Console.WriteLine($"Credentials file parse: exceptiom reading credentials file: {e.Message}");
                    }
                    throw new Exception("Failed to read IonBurst credentials", e);
                }
            }
            else
            {
                throw new ArgumentNullException(nameof(profile));
            }
        }
    }
}
