// Copyright Ionburst Limited 2019
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Reflection;
using System.IO;

using Microsoft.Extensions.Configuration;

using Ionburst.SDK.Model;

namespace Ionburst.SDK
{
    public class IonburstClient : IIonburstClient
    {
        private ApiHandler _apiHandler;
        private string _serverUri;
        private readonly string _uriDataPath = "api/Data/";
        private readonly string _uriClassiticationPath = "api/Classification/";
        private readonly string _uriJwtPath = "api/signin";
        private IonburstSDKSettings _settings = null;

        public IonburstClient()
        {
            _settings = new IonburstSDKSettings();
            CreateIonburstClient(_settings.IonburstUri);
        }

        public IonburstClient(string serverUri)
        {
            _settings = new IonburstSDKSettings();
            CreateIonburstClient(serverUri);
        }

        public IonburstClient(IConfiguration externalConfiguration)
        {
            _settings = new IonburstSDKSettings(externalConfiguration);
            CreateIonburstClient(_settings.IonburstUri);
        }

        public IonburstClient(IConfiguration externalConfiguration, string serverUri)
        {
            _settings = new IonburstSDKSettings(externalConfiguration);
            if (serverUri != null && serverUri != string.Empty)
            {
                CreateIonburstClient(serverUri);
            }
            else
            {
                CreateIonburstClient(_settings.IonburstUri);
            }
        }

        [Obsolete("Use CreateIonburstClient")]
        public void CreateIonBurstClient(string serverUri)
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

        public void CreateIonburstClient(string serverUri)
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

        // Delete object

        public DeleteObjectResult Delete(DeleteObjectRequest request)
        {
            request.CheckValues(_serverUri, _uriDataPath);
            return (DeleteObjectResult)_apiHandler.ProcessRequest(request).Result;
        }

        public async Task<DeleteObjectResult> DeleteAsync(DeleteObjectRequest request)
        {
            request.CheckValues(_serverUri, _uriDataPath);
            return (DeleteObjectResult)await _apiHandler.ProcessRequest(request);
        }

        public bool DeleteWithCallback(DeleteObjectRequest request)
        {
            bool functionResult = false;
            if (request.RequestResult != null)
            {
                try
                {
                    DelegateDelete(request);
                    functionResult = true;
                }
                catch (Exception)
                {
                    // Swallow
                }
            }

            return functionResult;
        }

        private async void DelegateDelete(DeleteObjectRequest request)
        {
            DeleteObjectResult result = await DeleteAsync(request);
            result.DelegateTag = request.DelegateTag;
            request.RequestResult?.Invoke(result);
        }

        // Get object

        public GetObjectResult Get(GetObjectRequest request)
        {
            request.CheckValues(_serverUri, _uriDataPath);
            return (GetObjectResult)_apiHandler.ProcessRequest(request).Result;
        }

        public async Task<GetObjectResult> GetAsync(GetObjectRequest request)
        {
            request.CheckValues(_serverUri, _uriDataPath);
            request.CheckTrailingCharacters();
            return (GetObjectResult)await _apiHandler.ProcessRequest(request);
        }

        public bool GetWithCallback(GetObjectRequest request)
        {
            bool functionResult = false;
            if (request.RequestResult != null)
            {
                try
                {
                    DelegateGet(request);
                    functionResult = true;
                }
                catch (Exception)
                {
                    // Swallow
                }
            }

            return functionResult;
        }

        private async void DelegateGet(GetObjectRequest request)
        {
            GetObjectResult result = await GetAsync(request);
            result.DelegateTag = request.DelegateTag;
            request.RequestResult?.Invoke(result);
        }

        // Put object

        public PutObjectResult Put(PutObjectRequest request)
        {
            request.CheckValues(_serverUri, _uriDataPath);
            return (PutObjectResult)_apiHandler.ProcessRequest(request).Result;
        }

        public async Task<PutObjectResult> PutAsync(PutObjectRequest request)
        {
            request.CheckValues(_serverUri, _uriDataPath);
            return (PutObjectResult)await _apiHandler.ProcessRequest(request);
        }

        public bool PutWithCallback(PutObjectRequest request)
        {
            bool functionResult = false;
            if (request.RequestResult != null)
            {
                try
                {
                    DelegatePut(request);
                    functionResult = true;
                }
                catch (Exception)
                {
                    // Swallow
                }
            }

            return functionResult;
        }

        private async void DelegatePut(PutObjectRequest request)
        {
            PutObjectResult result = await PutAsync(request);
            result.DelegateTag = request.DelegateTag;
            request.RequestResult?.Invoke(result);
        }

        public PutObjectResult RePut(PutObjectRequest request)
        {
            request.CheckValues(_serverUri, _uriDataPath);
            return RePutAsync(request).Result;
        }

        public async Task<PutObjectResult> RePutAsync(PutObjectRequest request)
        {
            request.CheckValues(_serverUri, _uriDataPath);
            GetObjectRequest getRequest = new GetObjectRequest
            {
                Particle = request.Particle
            };
            getRequest.CheckValues(_serverUri, _uriDataPath);
            GetObjectResult getResult = (GetObjectResult)await _apiHandler.ProcessRequest(getRequest);
            if (getResult.StatusCode == 200)
            {
                getResult.DataStream.Seek(0, SeekOrigin.Begin);
                request.DataStream = new MemoryStream();
                await getResult.DataStream.CopyToAsync(request.DataStream);

                // Need to delete it before the re-put. Risky
                DeleteObjectRequest deleteRequest = new DeleteObjectRequest
                {
                    Particle = request.Particle
                };
                deleteRequest.CheckValues(_serverUri, _uriDataPath);
                DeleteObjectResult deleteResult = (DeleteObjectResult)await _apiHandler.ProcessRequest(deleteRequest);
                if (deleteResult.StatusCode == 200)
                {
                    return (PutObjectResult)await _apiHandler.ProcessRequest(request);
                }
                else
                {
                    PutObjectResult result = new PutObjectResult
                    {
                        StatusCode = deleteResult.StatusCode,
                        StatusMessage = deleteResult.StatusMessage,
                        ActivityToken = deleteResult.ActivityToken
                    };
                    return result;
                }
            }
            else
            {
                PutObjectResult result = new PutObjectResult
                {
                    StatusCode = getResult.StatusCode,
                    StatusMessage = getResult.StatusMessage,
                    ActivityToken = getResult.ActivityToken
                };
                return result;
            }
        }

        // Deferred functions

        public async Task<DeferredActionResult> StartDeferredActionAsync(ObjectRequest request)
        {
            DeferredActionResult result = new DeferredActionResult();

            request.CheckValues(_serverUri, _uriDataPath);
            request.PhasedMode = true;
            if (request is GetObjectRequest getRequest)
            {
                string deferredToken = await _apiHandler.InitiateDeferredGet(getRequest);
                if (deferredToken != null && deferredToken != string.Empty)
                {
                    result.StatusCode = 200;
                }
                result.DeferredToken = new Guid(deferredToken);
            }
            else if (request is PutObjectRequest putRequest)
            {
                string deferredToken = await _apiHandler.InitiateDeferredPut(putRequest);
                if (deferredToken != null && deferredToken != string.Empty)
                {
                    result.StatusCode = 200;
                }
                result.DeferredToken = new Guid(deferredToken);
            }
            else
            {
                result.StatusCode = 400;
                result.StatusMessage = "Deferred action only available for PUT or GET";
            }

            return result;
        }

        public async Task<DeferredCheckResult> CheckDeferredActionAsync(ObjectRequest request)
        {
            request.CheckValues(_serverUri, _uriDataPath);
            return await _apiHandler.DeferredRequestCheck(request);
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
            return await _apiHandler.GetUploadSizeLimit($"{_serverUri}{_uriDataPath}query/uploadsizelimit");
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

        [Obsolete("Use CheckIonburstAPI")]
        public async Task<bool> CheckIonBurstAPI()
        {
            bool apiResponds = await _apiHandler.CheckApi($"{_serverUri}{_uriDataPath}web/check");

            return apiResponds;
        }

        public async Task<bool> CheckIonburstAPI()
        {
            bool apiResponds = await _apiHandler.CheckApi($"{_serverUri}{_uriDataPath}web/check");

            return apiResponds;
        }

        public async Task<List<string>> GetVersionDetails()
        {
            List<string> versionDetails = new List<string>();

            string apiVersion = await _apiHandler.GetAPIVersion($"{_serverUri}{_uriDataPath}assembly/version");
            versionDetails.Add($"API version: {apiVersion}");
            versionDetails.Add($"SDK version: {Assembly.GetExecutingAssembly().GetName().Version.ToString()}");

            return versionDetails;
        }

        public void SimulateBadTokenForTesting()
        {
            _apiHandler.SimulateBadToken();
        }
    }
}
