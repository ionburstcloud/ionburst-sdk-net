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
    public class IonburstClient : IonburstClientCommon, IIonburstClient
    {
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
            InternalCreateIonburstClient(serverUri);
        }

        public void CreateIonburstClient(string serverUri)
        {
            InternalCreateIonburstClient(serverUri);
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

        private async ValueTask<DeleteObjectResult> InternalDelete(DeleteObjectRequest request)
        {
            return (DeleteObjectResult)await _apiHandler.ProcessRequest(request);
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

        private async ValueTask<GetObjectResult> InternalGet(GetObjectRequest request)
        {
            return (GetObjectResult)await _apiHandler.ProcessRequest(request);
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

        private async ValueTask<PutObjectResult> InternalPut(PutObjectRequest request)
        {
            return (PutObjectResult)await _apiHandler.ProcessRequest(request);
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
            return await InternalCheckDeferredActionAsync(request);
        }

        public async Task<DeferredCheckResult> SecretsCheckDeferredActionAsync(ObjectRequest request)
        {
            request.Routing = string.Empty;
            request.CheckValues(_serverUri, _uriSecretsPath);
            return await InternalCheckDeferredActionAsync(request);
        }

        private async ValueTask<DeferredCheckResult> InternalCheckDeferredActionAsync(ObjectRequest request)
        {
            return await _apiHandler.DeferredRequestCheck(request);
        }
    }
}
