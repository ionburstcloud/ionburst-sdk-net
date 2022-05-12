// Copyright Ionburst Limited 2019
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Reflection;
using System.IO;

using Microsoft.Extensions.Configuration;

using Ionburst.SDK.Contracts;
using Ionburst.SDK.Model;

namespace Ionburst.SDK
{
    public class IonburstClient : IonburstClientCommon, IIonburstClient
    {
        public IonburstClient()
        {
            _settings = new IonburstSDKSettings(true);
        }

        [Obsolete("Use client builder")]
        public IonburstClient(string serverUri)
        {
            _settings = new IonburstSDKSettings();
            CreateIonburstClient(serverUri);
        }

        [Obsolete("Use client builder")]
        public IonburstClient(IConfiguration externalConfiguration)
        {
            _settings = new IonburstSDKSettings(externalConfiguration);
            CreateIonburstClient(_settings.IonburstUri);
        }

        [Obsolete("Use client builder")]
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

        public void CreateIonburstClient(string serverUri)
        {
            InternalCreateIonburstClient(serverUri);
        }

        public IonburstClient WithIonburstUri(string uri)
        {
            this._settings.IonburstUri = uri;
            return this;
        }

        public IonburstClient WithExternalConfiguration(IConfiguration externalConfiguration)
        {
            this._settings.ExternalConfiguration = externalConfiguration;
            return this;
        }

        public IonburstClient WithIonburstId(string user)
        {
            this._settings.IonburstId = user;
            return this;
        }

        public IonburstClient WithIonburstKey(string key)
        {
            this._settings.IonburstKey = key;
            return this;
        }

        public IonburstClient WithCredential(IonburstCredential credential)
        {
            this._settings.IonburstId = credential.IonburstId;
            this._settings.IonburstKey = credential.IonburstKey;
            return this;
        }

        public IonburstClient WithProfile(string profile)
        {
            this._settings.IonburstProfile = profile;
            return this;
        }

        public IIonburstClient Build()
        {
            this._settings.BuildConfiguationFromBuilder();
            this._settings.BuildIonburstSDKSettings();
            InternalCreateIonburstClient(_settings.IonburstUri);
            return this;
        }

        // Contoller specific request body limits

        public async Task<long> GetDataUploadSizeLimit()
        {
            return await _apiHandler.GetUploadSizeLimit($"{_serverUri}{_uriDataPath}query/uploadsizelimit");
        }

        public async Task<long> GetSecretsUploadSizeLimit()
        {
            return await _apiHandler.GetUploadSizeLimit($"{_serverUri}{_uriSecretsPath}query/uploadsizelimit");
        }

        // Check object
        public CheckObjectResult Check(CheckObjectRequest request)
        {
            request.CheckValues(_serverUri, _uriDataPath);
            return InternalCheck(request).Result;
        }

        public CheckObjectResult SecretsCheck(CheckObjectRequest request)
        {
            request.Routing = string.Empty;
            request.CheckValues(_serverUri, _uriDataPath);
            return InternalCheck(request).Result;
        }

        public async Task<CheckObjectResult> CheckAsync(CheckObjectRequest request)
        {
            request.CheckValues(_serverUri, _uriDataPath);
            return await InternalCheck(request);
        }

        public async Task<CheckObjectResult> SecretsCheckAsync(CheckObjectRequest request)
        {
            request.Routing = string.Empty;
            request.CheckValues(_serverUri, _uriDataPath);
            return await InternalCheck(request);
        }

        public bool CheckWithCallback(CheckObjectRequest request)
        {
            bool functionResult = false;
            if (request.RequestResult != null)
            {
                try
                {
                    DelegateCheck(request);
                    functionResult = true;
                }
                catch (Exception)
                {
                    // Swallow
                }
            }

            return functionResult;
        }

        public bool SecretsCheckWithCallback(CheckObjectRequest request)
        {
            bool functionResult = false;
            if (request.RequestResult != null)
            {
                try
                {
                    SecretsDelegateCheck(request);
                    functionResult = true;
                }
                catch (Exception)
                {
                    // Swallow
                }
            }

            return functionResult;
        }

        private async Task<CheckObjectResult> InternalCheck(CheckObjectRequest request)
        {
            return await _apiHandler.ProcessRequest(request) as CheckObjectResult;
        }

        private async void DelegateCheck(CheckObjectRequest request)
        {
            CheckObjectResult result = await CheckAsync(request);
            result.DelegateTag = request.DelegateTag;
            request.RequestResult?.Invoke(result);
        }

        private async void SecretsDelegateCheck(CheckObjectRequest request)
        {
            CheckObjectResult result = await SecretsCheckAsync(request);
            result.DelegateTag = request.DelegateTag;
            request.RequestResult?.Invoke(result);
        }

        // Delete object

        public DeleteObjectResult Delete(DeleteObjectRequest request)
        {
            request.CheckValues(_serverUri, _uriDataPath);
            return InternalDelete(request).Result;
        }

        public DeleteObjectResult SecretsDelete(DeleteObjectRequest request)
        {
            request.Routing = string.Empty;
            request.CheckValues(_serverUri, _uriSecretsPath);
            return InternalDelete(request).Result;
        }

        public async Task<DeleteObjectResult> DeleteAsync(DeleteObjectRequest request)
        {
            request.CheckValues(_serverUri, _uriDataPath);
            return await InternalDelete(request);
        }

        public async Task<DeleteObjectResult> SecretsDeleteAsync(DeleteObjectRequest request)
        {
            request.Routing = string.Empty;
            request.CheckValues(_serverUri, _uriSecretsPath);
            return await InternalDelete(request);
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

        public bool SecretsDeleteWithCallback(DeleteObjectRequest request)
        {
            bool functionResult = false;
            if (request.RequestResult != null)
            {
                try
                {
                    SecretsDelegateDelete(request);
                    functionResult = true;
                }
                catch (Exception)
                {
                    // Swallow
                }
            }

            return functionResult;
        }

        private async Task<DeleteObjectResult> InternalDelete(DeleteObjectRequest request)
        {
            return await _apiHandler.ProcessRequest(request) as DeleteObjectResult;
        }

        private async void DelegateDelete(DeleteObjectRequest request)
        {
            DeleteObjectResult result = await DeleteAsync(request);
            result.DelegateTag = request.DelegateTag;
            request.RequestResult?.Invoke(result);
        }

        private async void SecretsDelegateDelete(DeleteObjectRequest request)
        {
            DeleteObjectResult result = await SecretsDeleteAsync(request);
            result.DelegateTag = request.DelegateTag;
            request.RequestResult?.Invoke(result);
        }

        // Get object

        public GetObjectResult Get(GetObjectRequest request)
        {
            request.CheckValues(_serverUri, _uriDataPath);
            return InternalGet(request).Result;
        }

        public GetObjectResult SecretsGet(GetObjectRequest request)
        {
            request.Routing = string.Empty;
            request.CheckValues(_serverUri, _uriSecretsPath);
            return InternalGet(request).Result;
        }

        public async Task<GetObjectResult> GetAsync(GetObjectRequest request)
        {
            request.CheckValues(_serverUri, _uriDataPath);
            request.CheckTrailingCharacters();
            return await InternalGet(request);
        }

        public async Task<GetObjectResult> SecretsGetAsync(GetObjectRequest request)
        {
            request.Routing = string.Empty;
            request.CheckValues(_serverUri, _uriSecretsPath);
            request.CheckTrailingCharacters();
            return await InternalGet(request);
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

        public bool SecretsGetWithCallback(GetObjectRequest request)
        {
            bool functionResult = false;
            if (request.RequestResult != null)
            {
                try
                {
                    SecretsDelegateGet(request);
                    functionResult = true;
                }
                catch (Exception)
                {
                    // Swallow
                }
            }

            return functionResult;
        }

        private async Task<GetObjectResult> InternalGet(GetObjectRequest request)
        {
            return await _apiHandler.ProcessRequest(request) as GetObjectResult;
        }

        private async void DelegateGet(GetObjectRequest request)
        {
            GetObjectResult result = await GetAsync(request);
            result.DelegateTag = request.DelegateTag;
            request.RequestResult?.Invoke(result);
        }

        private async void SecretsDelegateGet(GetObjectRequest request)
        {
            GetObjectResult result = await SecretsGetAsync(request);
            result.DelegateTag = request.DelegateTag;
            request.RequestResult?.Invoke(result);
        }

        // Put object

        public PutObjectResult Put(PutObjectRequest request)
        {
            request.CheckValues(_serverUri, _uriDataPath);
            return InternalPut(request).Result;
        }

        public PutObjectResult SecretsPut(PutObjectRequest request)
        {
            request.Routing = string.Empty;
            request.CheckValues(_serverUri, _uriSecretsPath);
            return InternalPut(request).Result;
        }

        public async Task<PutObjectResult> PutAsync(PutObjectRequest request)
        {
            request.CheckValues(_serverUri, _uriDataPath);
            return await InternalPut(request);
        }

        public async Task<PutObjectResult> SecretsPutAsync(PutObjectRequest request)
        {
            request.Routing = string.Empty;
            request.CheckValues(_serverUri, _uriSecretsPath);
            return await InternalPut(request);
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

        public bool SecretsPutWithCallback(PutObjectRequest request)
        {
            bool functionResult = false;
            if (request.RequestResult != null)
            {
                try
                {
                    SecretsDelegatePut(request);
                    functionResult = true;
                }
                catch (Exception)
                {
                    // Swallow
                }
            }

            return functionResult;
        }

        private async Task<PutObjectResult> InternalPut(PutObjectRequest request)
        {
            return await _apiHandler.ProcessRequest(request) as PutObjectResult;
        }

        private async void DelegatePut(PutObjectRequest request)
        {
            PutObjectResult result = await PutAsync(request);
            result.DelegateTag = request.DelegateTag;
            request.RequestResult?.Invoke(result);
        }

        private async void SecretsDelegatePut(PutObjectRequest request)
        {
            PutObjectResult result = await SecretsPutAsync(request);
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
            GetObjectResult getResult = await _apiHandler.ProcessRequest(getRequest) as GetObjectResult;
            if (getResult.StatusCode == 200)
            {
                getResult.DataStream.Seek(0, SeekOrigin.Begin);
                MemoryStream ms = new MemoryStream();
                await getResult.DataStream.CopyToAsync(ms);
                ms.Seek(0, SeekOrigin.Begin);
                request.DataStream = ms;

                // Need to delete it before the re-put. Risky
                DeleteObjectRequest deleteRequest = new DeleteObjectRequest
                {
                    Particle = request.Particle
                };
                deleteRequest.CheckValues(_serverUri, _uriDataPath);
                DeleteObjectResult deleteResult = await _apiHandler.ProcessRequest(deleteRequest) as DeleteObjectResult;
                if (deleteResult.StatusCode == 200)
                {
                    return await _apiHandler.ProcessRequest(request) as PutObjectResult;
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
            request.CheckValues(_serverUri, _uriDataPath);
            return await InternalStartDeferredActionAsync(request);
        }

        public async Task<DeferredActionResult> SecretsStartDeferredActionAsync(ObjectRequest request)
        {
            request.Routing = string.Empty;
            request.CheckValues(_serverUri, _uriSecretsPath);
            return await InternalStartDeferredActionAsync(request);
        }

        private async Task<DeferredActionResult> InternalStartDeferredActionAsync(ObjectRequest request)
        {
            DeferredActionResult result = new DeferredActionResult();

            request.PhasedMode = true;
            if (request is GetObjectRequest getRequest)
            {
                DeferredResponse deferredResponse = await _apiHandler.InitiateDeferredGet(getRequest);
                if (deferredResponse.Status == 200)
                {
                    if (deferredResponse.DeferredToken != null && deferredResponse.DeferredToken != string.Empty)
                    {
                        try
                        {
                            result.DeferredToken = new Guid(deferredResponse.DeferredToken);
                            result.StatusCode = 200;
                        }
                        catch (Exception e)
                        {
                            result.StatusCode = 500;
                            result.StatusMessage = e.Message;
                        }
                    }
                    else
                    {
                        result.StatusCode = deferredResponse.Status;
                        if (result.StatusCode == 401 && result.StatusMessage == string.Empty)
                        {
                            result.StatusMessage = "Not authorized to get data";
                        }
                        if (result.StatusCode == 403 && result.StatusMessage == string.Empty)
                        {
                            result.StatusMessage = "Get operation rejected because quota is exceeded";
                        }
                        if (result.StatusCode == 429 && result.StatusMessage == string.Empty)
                        {
                            result.StatusMessage = "Web server throttling has prevented getting data";
                        }
                    }
                }
            }
            else if (request is PutObjectRequest putRequest)
            {
                DeferredResponse deferredResponse = await _apiHandler.InitiateDeferredPut(putRequest);
                if (deferredResponse.Status == 200)
                {
                    if (deferredResponse.DeferredToken != null && deferredResponse.DeferredToken != string.Empty)
                    {
                        try
                        {
                            result.DeferredToken = new Guid(deferredResponse.DeferredToken);
                            result.StatusCode = 200;
                        }
                        catch (Exception e)
                        {
                            result.StatusCode = 500;
                            result.StatusMessage = e.Message;
                        }
                    }
                }
                else
                {
                    result.StatusCode = deferredResponse.Status;
                    if (result.StatusCode == 401 && result.StatusMessage == string.Empty)
                    {
                        result.StatusMessage = "Not authorized to upload data";
                    }
                    if (result.StatusCode == 403 && result.StatusMessage == string.Empty)
                    {
                        result.StatusMessage = "Upload rejected because quota is exceeded";
                    }
                    if (result.StatusCode == 413 && result.StatusMessage == string.Empty)
                    {
                        result.StatusMessage = "Data is too large to upload";
                    }
                    if (result.StatusCode == 429 && result.StatusMessage == string.Empty)
                    {
                        result.StatusMessage = "Web server throttling has prevented the upload";
                    }
                }
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
            return await InternalCheckDeferredActionAsync(request);
        }

        public async Task<DeferredCheckResult> SecretsCheckDeferredActionAsync(ObjectRequest request)
        {
            request.Routing = string.Empty;
            request.CheckValues(_serverUri, _uriSecretsPath);
            return await InternalCheckDeferredActionAsync(request);
        }

        private async Task<DeferredCheckResult> InternalCheckDeferredActionAsync(ObjectRequest request)
        {
            return await _apiHandler.DeferredRequestCheck(request);
        }
    }
}
