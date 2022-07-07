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
using System.Linq;

using Microsoft.Extensions.DependencyInjection;

using Ionburst.SDK.Contracts;
using Ionburst.SDK.Model;
using Ionburst.Api.Model;

using Newtonsoft.Json;

namespace Ionburst.SDK
{
    public class ApiHandler
    {
        private readonly IHttpClientFactory _httpClientFactory = null;
        private readonly IonburstSDKSettings _settings = null;
        private readonly JwtRequest _jwtRequest = null;
        private readonly string _server;
        private readonly string _dataPath;
        private readonly string _secretsPath;

        private object _jwtLock = new object();

        internal class DeferredResult
        {
            public Guid DeferredToken { get; set; }
        }

        internal class ClassificationPair
        {
            public int id { get; set; }
            public string label { get; set; }
        }

        internal ApiHandler(IonburstSDKSettings settings, JwtRequest jwtRequest, string server, string dataPath, string secretsPath)
        {
            IServiceCollection serviceCollection = new ServiceCollection();
            serviceCollection.AddHttpClient();
            ServiceProvider serviceProvider = serviceCollection.BuildServiceProvider();
            _httpClientFactory = serviceProvider.GetService<IHttpClientFactory>();
            _settings = settings;
            _jwtRequest = jwtRequest;
            _server = server;
            _dataPath = dataPath;
            _secretsPath = secretsPath;
        }

        internal async Task<ObjectResult> ProcessRequest(ObjectRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (request is CheckObjectRequest checkRequest)
            {
                return await ProcessCheckRequest(checkRequest);
            }
            if (request is DeleteObjectRequest deleteRequest)
            {
                if (request is DeleteManifestRequest deleteManifestRequest)
                {
                    return await ProcessDeleteManifestRequest(deleteManifestRequest);
                }
                else
                {
                    return await ProcessDeleteRequest(deleteRequest);
                }
            }
            else if (request is GetObjectRequest getRequest)
            {
                if (request is GetManifestRequest getManifestRequest)
                {
                    return await ProcessGetManifestRequest(getManifestRequest);
                }
                else
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
            }
            else if (request is PutObjectRequest putRequest)
            {
                if (request is PutManifestRequest putManifestRequest)
                {
                    return await ProcessPutManifestRequest(putManifestRequest);
                }
                else
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

        private async Task<CheckObjectResult> ProcessCheckRequest(CheckObjectRequest request)
        {
            CheckObjectResult result = new CheckObjectResult();

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
                    using (HttpClient headClient = _httpClientFactory.CreateClient("HEADClient"))
                    {
                        if (request.TimeoutSpecified)
                        {
                            headClient.Timeout = request.RequestTimeout;
                        }
                        bool makeRequest = true;
                        int requestCount = 0;
                        while (makeRequest)
                        {
                            if (_settings.JWT != string.Empty)
                            {
                                headClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _settings.JWT);
                            }

                            HttpRequestMessage referenceRequest = new HttpRequestMessage
                            {
                                Method = HttpMethod.Head,
                                RequestUri = new Uri(requestString)
                            };
                            makeRequest = false;
                            HttpResponseMessage headResponse = await headClient.SendAsync(referenceRequest);
                            requestCount++;
                            result.StatusCode = Convert.ToInt32(headResponse.StatusCode);
                            if (headResponse.StatusCode == System.Net.HttpStatusCode.OK)
                            {
                                long objectSize = Convert.ToInt64(headResponse.Headers.GetValues("x-original-length").First());
                                result.ObjectSize = objectSize;
                            }
                            else if (headResponse.StatusCode == System.Net.HttpStatusCode.Unauthorized)
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

            return await Task.FromResult(result);
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
                    using (HttpClient deleteClient = _httpClientFactory.CreateClient("DELETEClient"))
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
                                    try
                                    {
                                        result.StatusMessage = await deleteResponse.Content.ReadAsStringAsync();
                                    }
                                    catch (Exception)
                                    {
                                        result.StatusMessage = "Request has been rate limited";
                                    }
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
                                        try
                                        {
                                            result.StatusMessage = await deleteResponse.Content.ReadAsStringAsync();
                                        }
                                        catch
                                        {
                                            result.StatusMessage = "API response in unexpected format";
                                        }
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

            return await Task.FromResult(result);
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
                    using (HttpClient getClient = _httpClientFactory.CreateClient("GETClient"))
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
                                try
                                {
                                    // Make sure response stream is at start
                                    result.DataStream.Seek(0, SeekOrigin.Begin);

                                    // The api will have given us the activity token for passing back to the SDK client
                                    IEnumerable<string> tokenList = getResponse.Headers.GetValues("x-activity-token");
                                    foreach (string tokenValue in tokenList)
                                    {
                                        // Only expect one
                                        try
                                        {
                                            result.ActivityToken = new Guid(tokenValue);
                                        }
                                        catch (Exception)
                                        {
                                            // Swallow
                                        }
                                    }
                                }
                                catch (Exception)
                                {
                                    // Swallow
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
                                if (result.StatusCode == 429)
                                {
                                    // Rate limiter has steppeed in
                                    try
                                    {
                                        result.StatusMessage = await getResponse.Content.ReadAsStringAsync();
                                    }
                                    catch
                                    {
                                        result.StatusMessage = "Request has been rate limited";
                                    }
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
                                        try
                                        {
                                            result.StatusMessage = await getResponse.Content.ReadAsStringAsync();
                                        }
                                        catch
                                        {
                                            result.StatusMessage = "API response in unexpected formart";
                                        }
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

            return await Task.FromResult(result);
        }

        private async Task<GetObjectResult> ProcessPhasedGetRequest(GetObjectRequest request)
        {
            GetObjectResult result = new GetObjectResult()
            {
                StatusMessage = string.Empty
            };
            DeferredCheckResult deferredResult = new DeferredCheckResult();

            try
            {
                // Start deferred GET
                DeferredResponse deferredResponse = await InitiateDeferredGet(request);
                if (deferredResponse.Status == 200)
                {
                    try
                    {
                        request.DeferredToken = new Guid(deferredResponse.DeferredToken);
                    }
                    catch (Exception e)
                    {
                        result.StatusCode = 500;
                        result.StatusMessage = e.Message;
                    }
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

            return await Task.FromResult(result);
        }

        internal async Task<DeferredResponse> InitiateDeferredGet(GetObjectRequest request)
        {
            DeferredResponse response = new DeferredResponse();

            try
            {
                string requestUriString = $"{request.Server}{request.Routing}";
                if (requestUriString != null && requestUriString != string.Empty)
                {
                    string requestString = $"{requestUriString}deferred/start/{request.Particle}";
                    Uri requestUri = new Uri(requestString);
                    using (HttpClient getClient = _httpClientFactory.CreateClient("DeferredGETClient"))
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
                                response.Status = 200;
                                string tokenResponse = await getResponse.Content.ReadAsStringAsync();
                                try
                                {
                                    DeferredResult tokenObject = JsonConvert.DeserializeObject<DeferredResult>(tokenResponse);
                                    response.DeferredToken = tokenObject.DeferredToken.ToString();
                                }
                                catch (Exception)
                                {
                                    // Probably old api giving plain text response
                                    response.DeferredToken = tokenResponse;
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
                                    else
                                    {
                                        response.Status = Convert.ToInt32(getResponse.StatusCode);
                                    }
                                }
                            }
                            else
                            {
                                // Not Ok
                                response.Status = Convert.ToInt32(getResponse.StatusCode);
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

            return await Task.FromResult(response);
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
                    using (HttpClient getClient = _httpClientFactory.CreateClient("DeferredCheckClient"))
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
                                    try
                                    {
                                        checkResult.StatusMessage = await getResponse.Content.ReadAsStringAsync();
                                    }
                                    catch
                                    {
                                        checkResult.StatusMessage = "API response in unexpected format";
                                    }
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
                                        try
                                        {
                                            checkResult.StatusMessage = await getResponse.Content.ReadAsStringAsync();
                                        }
                                        catch
                                        {
                                            checkResult.StatusMessage = "API respomse in unexpected format";
                                        }
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

            return await Task.FromResult(checkResult);
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
                        string requestString = $"{requestUriString}{request.Particle}";
                        if (request.PolicyClassification != null && request.PolicyClassification != string.Empty)
                        {
                            requestString = $"{requestString}?classstr={request.PolicyClassification}";
                        }
                        else
                        {
                            requestString = $"{requestString}?classid={request.PolicyClassificationId}";
                        }
                        Uri requestUri = new Uri(requestString);
                        using (HttpClient putClient = _httpClientFactory.CreateClient($"POSTClient"))
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
                                // Postman default headers
                                //putClient.DefaultRequestHeaders.Add("Accept", "*/*");
                                //putClient.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate, br");
                                //putClient.DefaultRequestHeaders.Add("Connection", "keep-alive");
                                //putClient.DefaultRequestHeaders.Add("Cache-Control", "no-cache");
                                if (request.DataStream.Position != request.StreamPosition)
                                {
                                    request.DataStream.Seek(request.StreamPosition, SeekOrigin.Begin);
                                }

                                ByteArrayContent sendData = await MakeByteArrayContent(request.DataStream);
                                if (sendData != null)
                                {
                                    sendData.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

                                    makeRequest = false;
                                    try
                                    {
                                        HttpResponseMessage putResponse = await putClient.PostAsync(requestUri, sendData);
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
                                                // Rate limiter has stepped in
                                                try
                                                {
                                                    result.StatusMessage = await putResponse.Content.ReadAsStringAsync();
                                                }
                                                catch (Exception)
                                                {
                                                    result.StatusMessage = "Request has been rate limited";
                                                }
                                            }
                                            else if (result.StatusCode == 413)
                                            {
                                                // The web server has rejected fot being too large
                                                try
                                                {
                                                    string apiOutput = await putResponse.Content.ReadAsStringAsync();
                                                    try
                                                    {
                                                        // Might be a workflow result
                                                        IWorkflowResult output = JsonConvert.DeserializeObject<WorkflowResult>(apiOutput);
                                                        result.ActivityToken = output.ActivityToken;
                                                        result.StatusMessage = output.Message;
                                                    }
                                                    catch (Exception)
                                                    {
                                                        result.StatusMessage = "Request data is too large";
                                                    }
                                                }
                                                catch (Exception)
                                                {
                                                    result.StatusMessage = "Request data is too large";
                                                }
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
                                                    try
                                                    {
                                                        result.StatusMessage = await putResponse.Content.ReadAsStringAsync();
                                                    }
                                                    catch (Exception)
                                                    {
                                                        result.StatusMessage = "API response not expected format";
                                                    }
                                                }
                                            }
                                        }
                                    }
                                    catch (HttpRequestException e)
                                    {
                                        result.StatusCode = 500;
                                        result.StatusMessage = $"HTTP request exception: {e.Message}";
                                    }
                                    catch (WebException e)
                                    {
                                        result.StatusCode = 500;
                                        result.StatusMessage = $"Web exception calling POST: {e.Message}";
                                    }
                                    catch (Exception e)
                                    {
                                        result.StatusCode = 99;
                                        result.StatusMessage = $"SDK exception making calling POST or handling response: {e.Message}";
                                    }

                                    sendData.Dispose();
                                    request.DataStream.Dispose();
                                }
                                else
                                {
                                    result.StatusCode = 99;
                                    result.StatusMessage = "Failed to create HttpContent for request";
                                }
                            }
                        }
                    }
                    else
                    {
                        result.StatusCode = 406;
                        result.StatusMessage = "DataStream supplied to SDK has zero length";
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

            return await Task.FromResult(result);
        }

        private async Task<PutObjectResult> ProcessPhasedPutRequest(PutObjectRequest request)
        {
            PutObjectResult result = new PutObjectResult
            {
                StatusMessage = string.Empty
            };
            DeferredCheckResult deferredResult = new DeferredCheckResult();

            try
            {
                // Start deferred PUT
                DeferredResponse deferredResponse = await InitiateDeferredPut(request);
                if (deferredResponse.Status == 200)
                {
                    try
                    {
                        request.DeferredToken = new Guid(deferredResponse.DeferredToken);
                    }
                    catch (Exception e)
                    {
                        result.StatusCode = 500;
                        result.StatusMessage = e.Message;
                    }
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
                    if (string.IsNullOrEmpty(result.StatusMessage))
                    {
                        result.StatusMessage = deferredResponse.Message;
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
                        try
                        {
                            result.StatusMessage = await reader.ReadToEndAsync();
                        }
                        catch
                        {
                            result.StatusMessage = e.Message;
                        }
                    }
                }
                else
                {
                    result.StatusMessage = e.Message;
                }
            }
            catch (Exception e)
            {
                result.StatusCode = 99;
                result.StatusMessage = $"SDK exception: {e.Message}";
            }

            return await Task.FromResult(result);
        }

        internal async Task<DeferredResponse> InitiateDeferredPut(PutObjectRequest request)
        {
            DeferredResponse response = new DeferredResponse();

            if (request is PutManifestRequest)
            {
                // What to do? A manifest involves multiple api calls and workflows but a deferred request is a single workflow
            }
            else
            {
                try
                {
                    string requestUriString = $"{request.Server}{request.Routing}";
                    if (requestUriString != null && requestUriString != string.Empty)
                    {
                        if (request.DataStream.Length > 0)
                        {
                            string requestString = $"{requestUriString}deferred/start/{request.Particle}";
                            if (request.PolicyClassification != null && request.PolicyClassification != string.Empty)
                            {
                                requestString = $"{requestString}?classstr={request.PolicyClassification}";
                            }
                            else
                            {
                                requestString = $"{requestString}?classid={request.PolicyClassificationId}";
                            }
                            Uri requestUri = new Uri(requestString);
                            using (HttpClient deferredPutClient = _httpClientFactory.CreateClient($"DeferredPOSTClient"))
                            {
                                if (request.TimeoutSpecified)
                                {
                                    deferredPutClient.Timeout = request.RequestTimeout;
                                }
                                bool makeRequest = true;
                                int requestCount = 0;
                                while (makeRequest)
                                {
                                    if (_settings.JWT != string.Empty)
                                    {
                                        deferredPutClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _settings.JWT);
                                    }
                                    // Postman default headers
                                    //deferredPutClient.DefaultRequestHeaders.Add("Accept", "*/*");
                                    //deferredPutClient.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate, br");
                                    //deferredPutClient.DefaultRequestHeaders.Add("Connection", "keep-alive");
                                    //deferredPutClient.DefaultRequestHeaders.Add("Cache-Control", "no-cache");
                                    if (request.DataStream.Position != request.StreamPosition)
                                    {
                                        request.DataStream.Seek(request.StreamPosition, SeekOrigin.Begin);
                                    }

                                    ByteArrayContent sendData = await MakeByteArrayContent(request.DataStream);
                                    if (sendData != null)
                                    {
                                        sendData.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

                                        makeRequest = false;
                                        try
                                        {
                                            HttpResponseMessage putResponse = await deferredPutClient.PostAsync(requestUri, sendData);
                                            requestCount++;
                                            response.Status = Convert.ToInt32(putResponse.StatusCode);
                                            if (putResponse.StatusCode == System.Net.HttpStatusCode.OK)
                                            {
                                                // Success
                                                response.Status = 200;
                                                string tokenResponse = await putResponse.Content.ReadAsStringAsync();
                                                try
                                                {
                                                    DeferredResult tokenObject = JsonConvert.DeserializeObject<DeferredResult>(tokenResponse);
                                                    response.DeferredToken = tokenObject.DeferredToken.ToString();
                                                }
                                                catch (Exception)
                                                {
                                                    // Probably old api giving plain text response
                                                    response.DeferredToken = tokenResponse;
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
                                                    else
                                                    {
                                                        response.Status = Convert.ToInt32(putResponse.StatusCode);
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                if (response.Status == 429)
                                                {
                                                    // Rate limiter has stepped in
                                                    response.Message = "Request has been rate limited";
                                                }
                                                if (response.Status == 413)
                                                {
                                                    // Body too large
                                                    response.Message = "Request body is too large";
                                                }
                                            }
                                        }
                                        catch (HttpRequestException e)
                                        {
                                            response.Status = 500;
                                            response.Message = $"HTTP request exception: {e.Message}";
                                        }
                                        catch (WebException e)
                                        {
                                            response.Status = 500;
                                            response.Message = $"Web exception calling POST: {e.Message}";
                                        }
                                        catch (Exception e)
                                        {
                                            response.Status = 99;
                                            response.Message = $"Exception making calling POST or handling response: {e.Message}";
                                        }

                                        sendData.Dispose();
                                    }
                                    else
                                    {
                                        response.Status = 99;
                                        response.Message = "Failed to create HttpContent for request";
                                    }
                                }
                            }
                        }
                    }
                }
                catch (WebException e)
                {
                    Console.WriteLine($"InitiateDeferredPut: {e.Message}");
                }
                catch (Exception e)
                {
                    Console.WriteLine($"InitiateDeferredPut: {e.Message}");
                }
            }

            if (response.Status == 0)
            {
                Console.WriteLine("Something wrong");
            }

            return await Task.FromResult(response);
        }

        private async Task<DeleteManifestResult> ProcessDeleteManifestRequest(DeleteManifestRequest request)
        {

            using ManifestWorker manifestWorker = new ManifestWorker(this, _settings, _server, _dataPath, _secretsPath);
            return await manifestWorker.ProcessDeleteManifestRequest(request);
        }

        private async Task<GetManifestResult> ProcessGetManifestRequest(GetManifestRequest request)
        {
            using ManifestWorker manifestWorker = new ManifestWorker(this, _settings, _server, _dataPath, _secretsPath);
            return await manifestWorker.ProcessGetManifestRequest(request);
        }

        private async Task<PutManifestResult> ProcessPutManifestRequest(PutManifestRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            if (request.ChunkSize == 0)
            {
                // Throw in the default
                if (_settings.DefaultChunkSize != 0)
                {
                    request.ChunkSize = _settings.DefaultChunkSize;
                }
                else
                {
                    request.ChunkSize = await GetUploadSizeLimit($"{_server}{_dataPath}query/uploadsizelimit");
                }
            }
            using ManifestWorker manifestWorker = new ManifestWorker(this, _settings, _server, _dataPath, _secretsPath);
            return await manifestWorker.ProcessPutManifestRequest(request);
        }

        private async Task<StreamContent> MakeStreamContent(Stream content)
        {
            try
            {
                return await Task.FromResult(new StreamContent(content));
            }
            catch
            {
                return null;
            }
        }

        private async Task<ByteArrayContent> MakeByteArrayContent(Stream content)
        {
            try
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    await content.CopyToAsync(ms, 65536);
                    return await Task.FromResult(new ByteArrayContent(ms.ToArray()));
                }
            }
            catch
            {
                return null;
            }
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
                    requestUriString = $"{requestUriString}?displaytag=both";
                    Uri requestUri = new Uri(requestUriString);
                    using (HttpClient httpClient = _httpClientFactory.CreateClient("ClassificationClient"))
                    {
                        if (_settings.JWT != string.Empty)
                        {
                            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _settings.JWT);
                        }

                        Task<HttpResponseMessage> getTask = httpClient.GetAsync(requestUri);
                        HttpResponseMessage getResponse = await getTask;
                        result.StatusCode = Convert.ToInt32(getResponse.StatusCode);
                        if (getResponse.StatusCode == System.Net.HttpStatusCode.OK)
                        {
                            // Success
                            try
                            {
                                string classResponse = await getResponse.Content.ReadAsStringAsync();
                                // Now depending on api being used this could be a string list or an object list
                                try
                                {
                                    List<ClassificationPair> classificationPairs = JsonConvert.DeserializeObject<List<ClassificationPair>>(classResponse);
                                    if (classificationPairs.Count > 0)
                                    {
                                        result.ClassificationDictionary = new Dictionary<int, string>();
                                        result.ClassificationIdList = new List<int>();
                                        result.ClassificationList = new List<string>();

                                        foreach (ClassificationPair pair in classificationPairs)
                                        {
                                            result.ClassificationDictionary.Add(pair.id, pair.label);
                                            result.ClassificationIdList.Add(pair.id);
                                            result.ClassificationList.Add(pair.label);
                                        }
                                    }
                                }
                                catch (Exception)
                                {
                                    // Most likely string list response from old api
                                    result.ClassificationList = JsonConvert.DeserializeObject<List<string>>(classResponse);
                                }
                            }
                            catch (Exception e)
                            {
                                result.StatusMessage = $"SDK exception handling GET response content: {e.Message}";
                                result.StatusCode = 99;
                            }
                        }
                        else
                        {
                            // Not Ok
                            if (result.StatusCode == 429)
                            {
                                // Rate limiter has steppeed in
                                try
                                {
                                    result.StatusMessage = await getResponse.Content.ReadAsStringAsync();
                                }
                                catch
                                {
                                    result.StatusMessage = "Request has been rate limited";
                                }
                            }
                            else
                            {
                                // Just put in the result content
                                try
                                {
                                    result.StatusMessage = await getResponse.Content.ReadAsStringAsync();
                                }
                                catch
                                {
                                    result.StatusMessage = "API response in unexpected format";
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

            return await Task.FromResult(result);
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
                        Username = _settings.IonburstId,
                        Password = _settings.IonburstKey
                    };
                    StringContent authorisationBodyString = new StringContent(JsonConvert.SerializeObject(authorisationBody));
                    authorisationBodyString.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                    using (HttpClient httpClient = _httpClientFactory.CreateClient("JWTClient"))
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
                                response.StatusMessage = $"Cannot connect to Ionburst";
                            }
                            else if (response.StatusCode == 503)
                            {
                                response.StatusMessage = $"Ionburst unavailable";
                            }
                            else
                            {
                                response.StatusMessage = $"Ionburst response to JWT request is {jwtResponse.StatusCode}";
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

            return await Task.FromResult(response);
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

        internal async Task<long> GetUploadSizeLimit(string request)
        {
            long limit = 0;

            try
            {
                Uri requestUri = new Uri(request);
                using (HttpClient httpClient = _httpClientFactory.CreateClient("SizeClient"))
                {
                    HttpResponseMessage sizeResponse = await httpClient.GetAsync(requestUri);
                    if (sizeResponse.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        limit = Convert.ToInt64(sizeResponse.Content.ReadAsStringAsync().Result);
                    }
                }
            }
            catch (WebException)
            {
            }
            catch (Exception)
            {
            }

            return await Task.FromResult(limit);
        }

        internal async Task<bool> CheckApi(string request)
        {
            bool apiResponds = false;

            try
            {
                Uri requestUri = new Uri(request);
                using (HttpClient httpClient = _httpClientFactory.CreateClient("APICheckClient"))
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

            return await Task.FromResult(apiResponds);
        }
        internal async Task<string> GetAPIVersion(string request)
        {
            string version = "Unknown";

            try
            {
                Uri requestUri = new Uri(request);
                using (HttpClient httpClient = _httpClientFactory.CreateClient("VersionClient"))
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

            return await Task.FromResult(version);
        }

        internal void SimulateBadToken()
        {
            _settings.JWT = "ABCDEFGH";
            _settings.JWTUpdateTime = _settings.JWTUpdateTime.AddHours(-1);
        }
    }
}
