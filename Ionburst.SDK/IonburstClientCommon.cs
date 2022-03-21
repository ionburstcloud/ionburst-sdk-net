// Copyright Ionburst Limited 2021
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Reflection;

using Ionburst.SDK.Contracts;
using Ionburst.SDK.Model;

namespace Ionburst.SDK
{
    public abstract class IonburstClientCommon : IIonburstClientCommon
    {
        protected ApiHandler _apiHandler;
        protected IonburstSDKSettings _settings;
        protected string _serverUri;

        protected readonly string _uriDataPath = "api/Data/";
        protected readonly string _uriSecretsPath = "api/Secrets/";
        protected readonly string _uriClassiticationPath = "api/Classification/";
        protected readonly string _uriJwtPath = "api/signin";

        protected void InternalCreateIonburstClient(string serverUri)
        {
            if (serverUri != null && serverUri != string.Empty)
            {
                if (serverUri.EndsWith("/"))
                {
                    _serverUri = serverUri;
                }
                else
                {
                    _serverUri = $"{serverUri}/";
                }

                JwtRequest jwtRequest = new JwtRequest()
                {
                    Server = _serverUri,
                    Routing = _uriJwtPath,
                    Id = _settings.IonburstId,
                    Key = _settings.IonburstKey
                };

                _apiHandler = new ApiHandler(_settings, jwtRequest);

                // Get first JWT
                JwtResponse jwtResponse = _apiHandler.GetJWT(jwtRequest).Result;
                if (jwtResponse.StatusCode == 200)
                {
                    _settings.JWT = jwtResponse.JWT;
                    _settings.JWTAssigned = true;
                    _settings.JWTUpdateTime = DateTime.Now;
                }
                else
                {
                    if (jwtResponse.StatusCode == 500)
                    {
                        throw new IonburstServiceException("SDK cannot connect to API", jwtResponse.Exception);
                    }
                    else if (jwtResponse.StatusCode == 503)
                    {
                        throw new IonburstServiceUnavailableException("Ionburst API service is not available", jwtResponse.Exception);
                    }
                    else
                    {
                        if (jwtResponse.Exception != null)
                        {
                            throw new IonburstServiceException($"SDK failed to negotiate with API: HTTP code {jwtResponse.StatusCode} - {jwtResponse.StatusMessage}", jwtResponse.Exception);
                        }
                        else
                        {
                            throw new IonburstServiceException($"SDK failed to negotiate with API: HTTP code {jwtResponse.StatusCode} - {jwtResponse.StatusMessage}");
                        }
                    }
                }
            }
            else
            {
                throw new IonburstUriUndefinedException("Ionburst URL not in configuration or supplied from client");
            }
        }

        // Get classifications

        public GetPolicyClassificationResult GetClassifications(GetPolicyClassificationRequest request)
        {
            request.CheckValues(_serverUri, _uriClassiticationPath);
            return (GetPolicyClassificationResult)_apiHandler.ProcessRequest(request).Result;
        }

        public async Task<GetPolicyClassificationResult> GetClassificationsAsync(GetPolicyClassificationRequest request)
        {
            request.CheckValues(_serverUri, _uriClassiticationPath);
            return (GetPolicyClassificationResult)await _apiHandler.ProcessRequest(request);
        }

        public bool GetClassificationsWithCallback(GetPolicyClassificationRequest request)
        {
            bool functionResult = false;
            try
            {
                DelegateClassification(request);
                functionResult = true;
            }
            catch (Exception)
            {
                // Swallow
            }

            return functionResult;
        }

        private async void DelegateClassification(GetPolicyClassificationRequest request)
        {
            GetPolicyClassificationResult result = await GetClassificationsAsync(request);
            result.DelegateTag = request.DelegateTag;
            request.RequestResult?.Invoke(result);
        }

        public async Task<long> GetUploadSizeLimit()
        {
            return await InternalGetUploadSizeLimit(_uriDataPath);
        }

        private async ValueTask<long> InternalGetUploadSizeLimit(string routePart)
        {
            long limit = await _apiHandler.GetUploadSizeLimit($"{_serverUri}query/uploadsizelimit");
            if (limit == 0)
            {
                // Could be older api
                limit = await _apiHandler.GetUploadSizeLimit($"{_serverUri}{routePart}query/uploadsizelimit");
            }
            return limit;
        }

        public async Task<string> GetConfiguredUri()
        {
            if (_settings.IonburstUri != null && _settings.IonburstUri != string.Empty)
            {
                return await Task.FromResult(_settings.IonburstUri);
            }
            else
            {
                return await Task.FromResult("An Ionburst server URI has not been configured");
            }
        }

        public async Task<bool> CheckIonburstAPI()
        {
            bool apiResponds = await _apiHandler.CheckApi($"{_serverUri}web/check");
            if (!apiResponds)
            {
                // Could be older api
                apiResponds = await _apiHandler.CheckApi($"{_serverUri}{_uriDataPath}web/check");
            }

            return apiResponds;
        }

        public async Task<List<string>> GetVersionDetails()
        {
            List<string> versionDetails = new List<string>();

            string apiVersion = await _apiHandler.GetAPIVersion($"{_serverUri}assembly/version");
            if (apiVersion == "HTTP response to version query is Not Found")
            {
                // Could be older api
                apiVersion = await _apiHandler.GetAPIVersion($"{_serverUri}{_uriDataPath}assembly/version");
            }
            versionDetails.Add($"API version: {apiVersion}");
            versionDetails.Add($"SDK version: {Assembly.GetExecutingAssembly().GetName().Version}");

            return versionDetails;
        }

        public void SimulateBadTokenForTesting()
        {
            _apiHandler.SimulateBadToken();
        }
    }
}
