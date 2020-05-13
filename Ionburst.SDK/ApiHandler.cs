// Copyright Ionburst Limited 2019
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using System.Security.Cryptography;

using Microsoft.Extensions.DependencyInjection;

using Ionburst.SDK.Model;
using Ionburst.Api.Model;

using Newtonsoft.Json;

namespace Ionburst.SDK
{
    public class ApiHandler
    {
        private IHttpClientFactory _httpClientFactory = null;
        private IonburstSDKSettings _settings = null;
        private JwtRequest _jwtRequest = null;
        private object _jwtLock = new object();

        internal class DeferredResult
        {
            public Guid DeferredToken { get; set; }
        }

        internal ApiHandler(IonburstSDKSettings settings, JwtRequest jwtRequest)
        {
            IServiceCollection serviceCollection = new ServiceCollection();
            serviceCollection.AddHttpClient();
            ServiceProvider serviceProvider = serviceCollection.BuildServiceProvider();
            _httpClientFactory = serviceProvider.GetService<IHttpClientFactory>();
            _settings = settings;
            _jwtRequest = jwtRequest;
        }

        internal async Task<ObjectResult> ProcessRequest(ObjectRequest request)
        {
            if (request is DeleteObjectRequest deleteRequest)
            {
                return await ProcessDeleteRequest(deleteRequest);
            }
            else if (request is GetObjectRequest getRequest)
            {
                if (getRequest.PhasedMode)
                {
                    return await ProcessPhasedGetRequest(getRequest);
                }
                else
                {
                    return await ProcessGetRequest(getRequest);
                }
            }
            else if (request is PutObjectRequest putRequest)
            {
                if (putRequest.PhasedMode)
                {
                    return await ProcessPhasedPutRequest(putRequest);
                }
                else
                {
                    return await ProcessPutRequest(putRequest);
                }
            }
            else if (request is GetPolicyClassificationRequest classRequest)
            {
                return await ProcessClassificationRequest(classRequest);
            }
            else
            {
                return null;
            }
        }

        private async Task<DeleteObjectResult> ProcessDeleteRequest(DeleteObjectRequest request)
        {
            DeleteObjectResult result = new DeleteObjectResult();

            try
            {
                string requestUriString = $"{request.Server}{request.Routing}";
                if (requestUriString == string.Empty || requestUriString == null)
                {
                    result.StatusCode = 400;
                }
                else
                {
                    string requestString = $"{requestUriString}{request.Particle}";
                    Uri requestUri = new Uri(requestString);
                    using (HttpClient deleteClient = _httpClientFactory.CreateClient())
                    {
                        if (request.TimeoutSpecified)
                        {
                            deleteClient.Timeout = request.RequestTimeout;
                        }
                        bool makeRequest = true;
                        int requestCount = 0;
                        while (makeRequest)
                        {
                            if (_settings.JWT != string.Empty)
                            {
                                deleteClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _settings.JWT);
                            }

                            makeRequest = false;
                            HttpResponseMessage deleteResponse = await deleteClient.DeleteAsync(requestUri);
                            requestCount++;
                            result.StatusCode = Convert.ToInt32(deleteResponse.StatusCode);
                            if (deleteResponse.StatusCode == System.Net.HttpStatusCode.OK)
                            {
                                // Success
                                try
                                {
                                    IWorkflowResult output = JsonConvert.DeserializeObject<WorkflowResult>(await deleteResponse.Content.ReadAsStringAsync());
                                    result.ActivityToken = output.ActivityToken;
                                }
                                catch (Exception e)
                                {
                                    result.StatusMessage = $"SDK exception handling DELETE response content: {e.Message}";
                                }
                            }
                            else if (deleteResponse.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                            {
                                // Possible that JWT has expired
                                if (_settings.JWTAssigned)
                                {
                                    // Had one before so refresh and try again. Once.
                                    if (requestCount < 2)
                                    {
                                        RefreshJWT();
                                        makeRequest = true;
                                    }
                                }
                            }
                            else
                            {
                                if (result.StatusCode == 429)
                                {
                                    // Rate limiter has steppeed in
                                    result.StatusMessage = await deleteResponse.Content.ReadAsStringAsync();
                                }
                                else
                                {
                                    // if a WorkflowResult came back, use that
                                    try
                                    {
                                        IWorkflowResult output = JsonConvert.DeserializeObject<WorkflowResult>(await deleteResponse.Content.ReadAsStringAsync());
                                        result.ActivityToken = output.ActivityToken;
                                        result.StatusMessage = output.Message;
                                    }
                                    catch
                                    {
                                        // Just put in the result content
                                        result.StatusMessage = await deleteResponse.Content.ReadAsStringAsync();
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (WebException e)
            {
                result.StatusCode = 500;
                if (e.Response != null)
                {
                    using (StreamReader reader = new StreamReader(e.Response.GetResponseStream()))
                    {
                        result.StatusMessage = await reader.ReadToEndAsync();
                    }
                }
            }
            catch (Exception e)
            {
                result.StatusCode = 99;
                result.StatusMessage = $"SDK exception: {e.Message}";
            }

            return result;
        }

        private async Task<GetObjectResult> ProcessGetRequest(GetObjectRequest request)
        {
            GetObjectResult result = new GetObjectResult();

            try
            {
                string requestUriString = $"{request.Server}{request.Routing}";
                if (requestUriString == string.Empty || requestUriString == null)
                {
                    result.StatusCode = 400;
                }
                else
                {
                    string requestString = $"{requestUriString}{request.Particle}";
                    if (request.DeferredToken != Guid.Empty)
                    {
                        requestString = $"{requestUriString}deferred/fetch/{request.DeferredToken}";
                    }
                    Uri requestUri = new Uri(requestString);
                    using (HttpClient getClient = _httpClientFactory.CreateClient())
                    {
                        if (request.TimeoutSpecified)
                        {
                            getClient.Timeout = request.RequestTimeout;
                        }
                        bool makeRequest = true;
                        int requestCount = 0;
                        while (makeRequest)
                        {
                            if (_settings.JWT != string.Empty)
                            {
                                getClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _settings.JWT);
                            }

                            makeRequest = false;
                            HttpResponseMessage getResponse = await getClient.GetAsync(requestUri);
                            requestCount++;
                            result.StatusCode = Convert.ToInt32(getResponse.StatusCode);
                            if (getResponse.StatusCode == System.Net.HttpStatusCode.OK)
                            {
                                // Success
                                result.DataStream = new MemoryStream();
                                await getResponse.Content.CopyToAsync(result.DataStream);
                            }
                            else if (getResponse.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                            {
                                // Possible that JWT has expired
                                if (_settings.JWTAssigned)
                                {
                                    // Had one before so refresh and try again. Once.
                                    if (requestCount < 2)
                                    {
                                        RefreshJWT();
                                        makeRequest = true;
                                    }
                                }
                            }
                            else
                            {
                                // Not Ok
                                if (result.StatusCode == 429)
                                {
                                    // Rate limiter has steppeed in
                                    result.StatusMessage = await getResponse.Content.ReadAsStringAsync();
                                }
                                else
                                {
                                    // if a WorkflowResult came back, use that
                                    try
                                    {
                                        IWorkflowResult output = JsonConvert.DeserializeObject<WorkflowResult>(await getResponse.Content.ReadAsStringAsync());
                                        result.ActivityToken = output.ActivityToken;
                                        result.StatusMessage = output.Message;
                                    }
                                    catch
                                    {
                                        // Just put in the result content
                                        result.StatusMessage = await getResponse.Content.ReadAsStringAsync();
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (WebException e)
            {
                result.StatusCode = 500;
                if (e.Response != null)
                {
                    using (StreamReader reader = new StreamReader(e.Response.GetResponseStream()))
                    {
                        result.StatusMessage = await reader.ReadToEndAsync();
                    }
                }
            }
            catch (Exception e)
            {
                result.StatusCode = 99;
                result.StatusMessage = $"SDK exception: {e.Message}";
            }

            return result;
        }

        private async Task<GetObjectResult> ProcessPhasedGetRequest(GetObjectRequest request)
        {
            GetObjectResult result = new GetObjectResult();
            DeferredCheckResult deferredResult = new DeferredCheckResult();

            try
            {
                // Start deferred GET
                request.DeferredToken = new Guid(await InitiateDeferredGet(request));
                if (request.DeferredToken != Guid.Empty)
                {
                    // Deferred GET is started, so check it
                    int checkCount = 0;
                    bool actionComplete = false;
                    while (!actionComplete)
                    {
                        // Eventually, either the workflow will complete or the deferred details cache entry will expire giving a 404
                        if (checkCount++ < 21)
                        {
                            Thread.Sleep(500);
                        }
                        else
                        {
                            Thread.Sleep(1000);
                        }
                        deferredResult = await DeferredRequestCheck(request);
                        actionComplete = deferredResult.ActionComplete;
                    }
                    if (actionComplete)
                    {
                        if (deferredResult.StatusCode == 200)
                        {
                            result = await ProcessGetRequest(request);
                        }
                        else
                        {
                            result.ActivityToken = deferredResult.ActivityToken;
                            result.StatusCode = deferredResult.StatusCode;
                            result.StatusMessage = deferredResult.StatusMessage;
                        }
                    }
                }
            }
            catch (WebException e)
            {
                result.StatusCode = 500;
                if (e.Response != null)
                {
                    using (StreamReader reader = new StreamReader(e.Response.GetResponseStream()))
                    {
                        result.StatusMessage = await reader.ReadToEndAsync();
                    }
                }
            }
            catch (Exception e)
            {
                result.StatusCode = 99;
                result.StatusMessage = $"SDK exception: {e.Message}";
            }

            return result;
        }

        internal async Task<string> InitiateDeferredGet(GetObjectRequest request)
        {
            string deferredToken = string.Empty;

            try
            {
                string requestUriString = $"{request.Server}{request.Routing}";
                if (requestUriString != null && requestUriString != string.Empty)
                {
                    string requestString = $"{requestUriString}deferred/start/{request.Particle}";
                    Uri requestUri = new Uri(requestString);
                    using (HttpClient getClient = _httpClientFactory.CreateClient())
                    {
                        if (request.TimeoutSpecified)
                        {
                            getClient.Timeout = request.RequestTimeout;
                        }
                        bool makeRequest = true;
                        int requestCount = 0;
                        while (makeRequest)
                        {
                            if (_settings.JWT != string.Empty)
                            {
                                getClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _settings.JWT);
                            }

                            makeRequest = false;
                            HttpResponseMessage getResponse = await getClient.GetAsync(requestUri);
                            requestCount++;
                            if (getResponse.StatusCode == System.Net.HttpStatusCode.OK)
                            {
                                // Success
                                string tokenResponse = await getResponse.Content.ReadAsStringAsync();
                                DeferredResult tokenObject = JsonConvert.DeserializeObject<DeferredResult>(tokenResponse);
                                deferredToken = tokenObject.DeferredToken.ToString();
                            }
                            else if (getResponse.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                            {
                                // Possible that JWT has expired
                                if (_settings.JWTAssigned)
                                {
                                    // Had one before so refresh and try again. Once.
                                    if (requestCount < 2)
                                    {
                                        RefreshJWT();
                                        makeRequest = true;
                                    }
                                }
                            }
                            else
                            {
                                // Not Ok
                            }
                        }
                    }
                }
            }
            catch (WebException)
            {
            }
            catch (Exception)
            {
            }

            return deferredToken;
        }

        internal async Task<DeferredCheckResult> DeferredRequestCheck(ObjectRequest request)
        {
            DeferredCheckResult checkResult = new DeferredCheckResult
            {
                ActionComplete = false
            };

            try
            {
                string requestUriString = $"{request.Server}{request.Routing}";
                string deferredToken = request.DeferredToken.ToString();
                if (requestUriString != null && requestUriString != string.Empty)
                {
                    string requestString = $"{requestUriString}deferred/check/{deferredToken}";
                    Uri requestUri = new Uri(requestString);
                    using (HttpClient getClient = _httpClientFactory.CreateClient())
                    {
                        if (request.TimeoutSpecified)
                        {
                            getClient.Timeout = request.RequestTimeout;
                        }
                        bool makeRequest = true;
                        int requestCount = 0;
                        while (makeRequest)
                        {
                            if (_settings.JWT != string.Empty)
                            {
                                getClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _settings.JWT);
                            }

                            makeRequest = false;
                            HttpResponseMessage getResponse = await getClient.GetAsync(requestUri);
                            requestCount++;
                            if (getResponse.StatusCode == System.Net.HttpStatusCode.OK)
                            {
                                checkResult.StatusCode = 200;
                                // Success
                                try
                                {
                                    IWorkflowResult output = JsonConvert.DeserializeObject<WorkflowResult>(await getResponse.Content.ReadAsStringAsync());
                                    checkResult.ActionComplete = true;
                                    checkResult.ActivityToken = output.ActivityToken;
                                    checkResult.StatusCode = output.Status;
                                    checkResult.StatusMessage = output.Message;
                                }
                                catch (Exception)
                                {
                                    checkResult.StatusCode = 500;
                                    checkResult.StatusMessage = await getResponse.Content.ReadAsStringAsync();
                                }
                            }
                            else if (getResponse.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                            {
                                // Possible that JWT has expired
                                if (_settings.JWTAssigned)
                                {
                                    // Had one before so refresh and try again. Once.
                                    if (requestCount < 2)
                                    {
                                        RefreshJWT();
                                        makeRequest = true;
                                    }
                                }
                            }
                            else
                            {
                                // Not Ok
                                checkResult.StatusCode = Convert.ToInt32(getResponse.StatusCode);
                                if (getResponse.StatusCode != HttpStatusCode.Accepted)
                                {
                                    // Didn't work, but still completed
                                    checkResult.ActionComplete = true;
                                    try
                                    {
                                        IWorkflowResult output = JsonConvert.DeserializeObject<WorkflowResult>(await getResponse.Content.ReadAsStringAsync());
                                        checkResult.ActivityToken = output.ActivityToken;
                                        checkResult.StatusCode = output.Status;
                                        checkResult.StatusMessage = output.Message;
                                    }
                                    catch (Exception)
                                    {
                                        checkResult.StatusMessage = await getResponse.Content.ReadAsStringAsync();
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (WebException e)
            {
                checkResult.StatusCode = 500;
                if (e.Response != null)
                {
                    using (StreamReader reader = new StreamReader(e.Response.GetResponseStream()))
                    {
                        checkResult.StatusMessage = await reader.ReadToEndAsync();
                    }
                }
            }
            catch (Exception e)
            {
                checkResult.StatusCode = 99;
                checkResult.StatusMessage = $"SDK exception: {e.Message}";
            }

            return checkResult;
        }

        private async Task<PutObjectResult> ProcessPutRequest(PutObjectRequest request)
        {
            PutObjectResult result = new PutObjectResult();

            try
            {
                string requestUriString = $"{request.Server}{request.Routing}";
                if (requestUriString == string.Empty || requestUriString == null)
                {
                    result.StatusCode = 400;
                }
                else
                {
                    if (request.DataStream.Length > 0)
                    {
                        request.DataStream.Position = request.StreamPosition;
                        // Can't put stream in using {} or it will be disposed before use
                        StreamContent sendData = new StreamContent(request.DataStream);
                        sendData.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                        string requestString = $"{requestUriString}{request.Particle}";
                        if (request.PolicyClassification != null && request.PolicyClassification != string.Empty)
                        {
                            requestString = $"{requestString}?classstr={request.PolicyClassification}";
                        }
                        Uri requestUri = new Uri(requestString);
                        using (HttpClient putClient = _httpClientFactory.CreateClient())
                        {
                            if (request.TimeoutSpecified)
                            {
                                putClient.Timeout = request.RequestTimeout;
                            }
                            bool makeRequest = true;
                            int requestCount = 0;
                            while (makeRequest)
                            {
                                if (_settings.JWT != string.Empty)
                                {
                                    putClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _settings.JWT);
                                }

                                makeRequest = false;
                                HttpResponseMessage putResponse = await putClient.PutAsync(requestUri, sendData);
                                requestCount++;
                                result.StatusCode = Convert.ToInt32(putResponse.StatusCode);
                                if (putResponse.StatusCode == System.Net.HttpStatusCode.OK)
                                {
                                    // Success
                                    try
                                    {
                                        IWorkflowResult output = JsonConvert.DeserializeObject<WorkflowResult>(await putResponse.Content.ReadAsStringAsync());
                                        result.ActivityToken = output.ActivityToken;
                                    }
                                    catch (Exception e)
                                    {
                                        result.StatusMessage = $"SDK exception handling PUT response content: {e.Message}";
                                    }
                                }
                                else if (putResponse.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                                {
                                    // Possible that JWT has expired
                                    if (_settings.JWTAssigned)
                                    {
                                        // Had one before so refresh and try again. Once.
                                        if (requestCount < 2)
                                        {
                                            RefreshJWT();
                                            makeRequest = true;
                                        }
                                    }
                                }
                                else
                                {
                                    // Not Ok
                                    if (result.StatusCode == 429)
                                    {
                                        // Rate limiter has steppeed in
                                        result.StatusMessage = await putResponse.Content.ReadAsStringAsync();
                                    }
                                    else
                                    {
                                        // if a WorkflowResult came back, use that
                                        try
                                        {
                                            IWorkflowResult output = JsonConvert.DeserializeObject<WorkflowResult>(await putResponse.Content.ReadAsStringAsync());
                                            result.ActivityToken = output.ActivityToken;
                                            result.StatusMessage = output.Message;
                                        }
                                        catch
                                        {
                                            // Just put in the result content
                                            result.StatusMessage = await putResponse.Content.ReadAsStringAsync();
                                        }
                                    }
                                }
                            }
                        }
                        sendData.Dispose();
                    }
                    else
                    {
                        result.StatusCode = 406;
                        result.StatusMessage = $"DataStream supplied to SDK has zero length";
                    }
                }
            }
            catch (WebException e)
            {
                result.StatusCode = 500;
                if (e.Response != null)
                {
                    using (StreamReader reader = new StreamReader(e.Response.GetResponseStream()))
                    {
                        result.StatusMessage = await reader.ReadToEndAsync();
                    }
                }
            }
            catch (Exception e)
            {
                result.StatusCode = 99;
                result.StatusMessage = $"SDK exception: {e.Message}";
            }

            return result;
        }

        private async Task<PutObjectResult> ProcessPhasedPutRequest(PutObjectRequest request)
        {
            PutObjectResult result = new PutObjectResult();
            DeferredCheckResult deferredResult = new DeferredCheckResult();

            try
            {
                // Start deferred PUT
                request.DeferredToken = new Guid(await InitiateDeferredPut(request));
                if (request.DeferredToken != Guid.Empty)
                {
                    // Deferred PUT is started, so check it
                    int checkCount = 0;
                    bool actionComplete = false;
                    while (!actionComplete)
                    {
                        // Eventually, either the workflow will complete or the deferred details cache entry will expire giving a 404
                        if (checkCount++ < 21)
                        {
                            Thread.Sleep(750);
                        }
                        else
                        {
                            Thread.Sleep(1500);
                        }
                    deferredResult = await DeferredRequestCheck(request);
                    actionComplete = deferredResult.ActionComplete;
                    }
                    if (actionComplete)
                    {
                        result.ActivityToken = deferredResult.ActivityToken;
                        result.StatusCode = deferredResult.StatusCode;
                        result.StatusMessage = deferredResult.StatusMessage;
                    }
                }
            }
            catch (WebException e)
            {
                result.StatusCode = 500;
                if (e.Response != null)
                {
                    using (StreamReader reader = new StreamReader(e.Response.GetResponseStream()))
                    {
                        result.StatusMessage = await reader.ReadToEndAsync();
                    }
                }
            }
            catch (Exception e)
            {
                result.StatusCode = 99;
                result.StatusMessage = $"SDK exception: {e.Message}";
            }

            return result;
        }

        internal async Task<string> InitiateDeferredPut(PutObjectRequest request)
        {
            string deferredToken = string.Empty;

            try
            {
                string requestUriString = $"{request.Server}{request.Routing}";
                if (requestUriString != null && requestUriString != string.Empty)
                {
                    if (request.DataStream.Length > 0)
                    {
                        request.DataStream.Position = request.StreamPosition;
                        // Can't put stream in using {} or it will be disposed before use
                        StreamContent sendData = new StreamContent(request.DataStream);
                        sendData.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

                        string requestString = $"{requestUriString}deferred/start/{request.Particle}";
                        if (request.PolicyClassification != null && request.PolicyClassification != string.Empty)
                        {
                            requestString = $"{requestString}?classstr={request.PolicyClassification}";
                        }
                        Uri requestUri = new Uri(requestString);
                        using (HttpClient getClient = _httpClientFactory.CreateClient())
                        {
                            if (request.TimeoutSpecified)
                            {
                                getClient.Timeout = request.RequestTimeout;
                            }
                            bool makeRequest = true;
                            int requestCount = 0;
                            while (makeRequest)
                            {
                                if (_settings.JWT != string.Empty)
                                {
                                    getClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _settings.JWT);
                                }

                                makeRequest = false;
                                HttpResponseMessage putResponse = await getClient.PostAsync(requestUri, sendData);
                                requestCount++;
                                if (putResponse.StatusCode == System.Net.HttpStatusCode.OK)
                                {
                                    // Success
                                    string tokenResponse = await putResponse.Content.ReadAsStringAsync();
                                    DeferredResult tokenObject = JsonConvert.DeserializeObject<DeferredResult>(tokenResponse);
                                    deferredToken = tokenObject.DeferredToken.ToString();
                                }
                                else if (putResponse.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                                {
                                    // Possible that JWT has expired
                                    if (_settings.JWTAssigned)
                                    {
                                        // Had one before so refresh and try again. Once.
                                        if (requestCount < 2)
                                        {
                                            RefreshJWT();
                                            makeRequest = true;
                                        }
                                    }
                                }
                                else
                                {
                                    // Not Ok
                                }
                            }
                        }
                        sendData.Dispose();
                    }
                }
            }
            catch (WebException)
            {
            }
            catch (Exception)
            {
            }

            return deferredToken;
        }

        private async Task<GetPolicyClassificationResult> ProcessClassificationRequest(GetPolicyClassificationRequest request)
        {
            GetPolicyClassificationResult result = new GetPolicyClassificationResult();

            try
            {
                string requestUriString = $"{request.Server}{request.Routing}";
                if (requestUriString == string.Empty || requestUriString == null)
                {
                    result.StatusCode = 400;
                }
                else
                {
                    Uri requestUri = new Uri(requestUriString);
                    using (HttpClient httpClient = _httpClientFactory.CreateClient())
                    {
                        Task<HttpResponseMessage> getTask = httpClient.GetAsync(requestUri);
                        HttpResponseMessage getResponse = await getTask;
                        result.StatusCode = Convert.ToInt32(getResponse.StatusCode);
                        if (getResponse.StatusCode == System.Net.HttpStatusCode.OK)
                        {
                            // Success
                            try
                            {
                                result.ClassificationList = JsonConvert.DeserializeObject<List<string>>(await getResponse.Content.ReadAsStringAsync());
                            }
                            catch (Exception e)
                            {
                                result.StatusMessage = $"SDK exception handling GET response content: {e.Message}";
                            }
                        }
                        else
                        {
                            // Not Ok
                            if (result.StatusCode == 429)
                            {
                                // Rate limiter has steppeed in
                                result.StatusMessage = await getResponse.Content.ReadAsStringAsync();
                            }
                            else
                            {
                                // Just put in the result content
                                result.StatusMessage = await getResponse.Content.ReadAsStringAsync();
                            }
                        }
                    }
                }
            }
            catch (WebException e)
            {
                result.StatusCode = 500;
                if (e.Response != null)
                {
                    using (StreamReader reader = new StreamReader(e.Response.GetResponseStream()))
                    {
                        result.StatusMessage = await reader.ReadToEndAsync();
                    }
                }
            }
            catch (Exception e)
            {
                result.StatusCode = 99;
                result.StatusMessage = $"SDK exception: {e.Message}";
            }

            return result;
        }

        internal async Task<JwtResponse> GetJWT(JwtRequest request)
        {
            JwtResponse response = new JwtResponse();

            try
            {
                string requestUriString = $"{request.Server}{request.Routing}";
                if (requestUriString == string.Empty || requestUriString == null)
                {
                    response.StatusCode = 400;
                }
                else
                {
                    Uri requestUri = new Uri(requestUriString);
                    AuthorisationBody authorisationBody = new AuthorisationBody()
                    {
                        Username = _settings.IonBurstId,
                        Password = _settings.IonBurstKey
                    };
                    StringContent authorisationBodyString = new StringContent(JsonConvert.SerializeObject(authorisationBody));
                    authorisationBodyString.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                    using (HttpClient httpClient = _httpClientFactory.CreateClient())
                    {
                        HttpResponseMessage jwtResponse = await httpClient.PostAsync(requestUri, authorisationBodyString);
                        response.StatusCode = Convert.ToInt32(jwtResponse.StatusCode);
                        if (jwtResponse.StatusCode == System.Net.HttpStatusCode.OK)
                        {
                            AuthorisationReponse authResponse = JsonConvert.DeserializeObject<AuthorisationReponse>(await jwtResponse.Content.ReadAsStringAsync());
                            response.JWT = authResponse.IdToken;
                        }
                        else
                        {
                            response.JWT = string.Empty;
                            if (response.StatusCode == 500)
                            {
                                response.StatusMessage = $"Cannot connect to IonBurst";
                            }
                            else
                            {
                                response.StatusMessage = $"IonBurst response to JWT request is {jwtResponse.StatusCode}";
                            }
                        }
                    }
                }
            }
            catch (WebException e)
            {
                response.StatusCode = 500;
                if (e.Response != null)
                {
                    using (StreamReader reader = new StreamReader(e.Response.GetResponseStream()))
                    {
                        response.StatusMessage = await reader.ReadToEndAsync();
                    }
                }
            }
            catch (Exception e)
            {
                // WIP
                response.StatusCode = 500;
                response.Exception = e;
            }

            return response;
        }

        internal void RefreshJWT()
        {
            lock (_jwtLock)
            {
                // If updated in last 10 secs (which might occur with multi-threading) then no need to update it again
                if (_settings.JWTUpdateTime.AddSeconds(10) <= DateTime.Now)
                {
                    JwtResponse jwtResponse = GetJWT(_jwtRequest).Result;
                    if (jwtResponse.StatusCode == 200)
                    {
                        _settings.JWT = jwtResponse.JWT;
                        _settings.JWTUpdateTime = DateTime.Now;
                    }
                    else
                    {
                        _settings.JWT = string.Empty;
                        _settings.JWTAssigned = false;
                    }
                }
            }
        }

        internal async Task<bool> CheckApi(string request)
        {
            bool apiResponds = false;

            try
            {
                Uri requestUri = new Uri(request);
                using (HttpClient httpClient = _httpClientFactory.CreateClient())
                {
                    HttpResponseMessage versionResponse = await httpClient.GetAsync(requestUri);
                    if (versionResponse.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        apiResponds = true;
                    }
                }
            }
            catch (WebException)
            {
            }
            catch (Exception)
            {
            }

            return apiResponds;
        }
        internal async Task<string> GetAPIVersion(string request)
        {
            string version = "Unknown";

            try
            {
                Uri requestUri = new Uri(request);
                using (HttpClient httpClient = _httpClientFactory.CreateClient())
                {
                    HttpResponseMessage versionResponse = await httpClient.GetAsync(requestUri);
                    if (versionResponse.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        version = await versionResponse.Content.ReadAsStringAsync();
                    }
                    else
                    {
                        version = $"HTTP response to version query is {versionResponse.StatusCode}";
                    }
                }
            }
            catch (WebException e)
            {
                if (e.Response != null)
                {
                    using (StreamReader reader = new StreamReader(e.Response.GetResponseStream()))
                    {
                        version = await reader.ReadToEndAsync();
                    }
                }
            }
            catch (Exception e)
            {
                version = $"Excrption: {e.Message}";
            }

            return version;
        }

        internal void SimulateBadToken()
        {
            _settings.JWT = "ABCDEFGH";
            _settings.JWTUpdateTime = _settings.JWTUpdateTime.AddHours(-1);
        }
    }
}
