// Copyright Ionburst Limited 2022
using System;
using System.Threading.Tasks;
using System.Text;
using System.IO;
using System.Security.Cryptography;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;

using Newtonsoft.Json;

using Ionburst.SDK.Model;

namespace Ionburst.SDK
{
    internal class DeleteManifestChunk
    {
        public DeleteObjectResult DeleteResult { get; set; }
        public int StatusCode { get; set; }
        public string StatusMessage { get; set; }

        public DeleteManifestChunk()
        {
            DeleteResult = new DeleteObjectResult();
        }
    }

    internal class GetManifestChunk
    {
        public GetObjectResult GetResult { get; set; }
        public IonburstChunk Chunk { get; set; }
        public int StatusCode { get; set; }
        public string StatusMessage { get; set; }

        public GetManifestChunk()
        {
            GetResult = new GetObjectResult();
            Chunk = new IonburstChunk();
        }
    }

    internal class PutManifestChunk
    {
        public PutObjectResult PutResult { get; set; }
        public int StatusCode { get; set; }
        public string StatusMessage { get; set; }

        public PutManifestChunk()
        {
            PutResult = new PutObjectResult();
        }
    }

    public class ManifestWorker : IDisposable
    {
        private readonly ApiHandler _apiHandler;
        private readonly string _server;
        private readonly string _dataPath;
        private readonly string _secretsPath;

        private const long MIN_CHUNK = 65536;

        private readonly ConcurrentBag<PutManifestChunk> _putResultCollection = new ConcurrentBag<PutManifestChunk>();

        public ManifestWorker(ApiHandler apiHandler, string server, string dataPath, string secretsPath)
        {
            _apiHandler = apiHandler;
            _server = server;
            _dataPath = dataPath;
            _secretsPath = secretsPath;
        }

        public void Dispose()
        {
            _putResultCollection.Clear();
        }

        public async Task<DeleteManifestResult> ProcessDeleteManifestRequest(DeleteManifestRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            DeleteManifestResult result = new DeleteManifestResult();

            try
            {
                // Get the manifest
                GetObjectRequest manifestRequest = new GetObjectRequest()
                {
                    Particle = request.Particle,
                    Server = request.Server,
                    Routing = request.Routing
                };
                GetObjectResult manifestResult = await _apiHandler.ProcessRequest(manifestRequest) as GetObjectResult;

                if (manifestResult != null && manifestResult.StatusCode == 200)
                {
                    StreamReader manifestReader = new StreamReader(manifestResult.DataStream);
                    string manifestJson = manifestReader.ReadToEnd();
                    IonburstManifest ionburstManifest = null;
                    try
                    {
                        ionburstManifest = JsonConvert.DeserializeObject<IonburstManifest>(manifestJson);
                    }
                    catch (Exception e)
                    {
                        // Doesn't look like this is a manifest
                        result.StatusCode = 400;
                        result.StatusMessage = $"{request.Particle} does not appear to be a manifest: {e.Message}";
                    }

                    if (ionburstManifest != null)
                    {
                        DeleteManifestResult objectDeletionResult = await DeleteManifestChunks(request, ionburstManifest);
                        if (objectDeletionResult != null)
                        {
                            result.ManifestActivities = objectDeletionResult.ManifestActivities;
                            if (objectDeletionResult.StatusCode == 200)
                            {
                                // Delete the manifest
                                DeleteObjectRequest manifestDeleteRequest = new DeleteObjectRequest()
                                {
                                    Particle = request.Particle,
                                    Server = request.Server,
                                    Routing = request.Routing
                                };
                                DeleteObjectResult manifestDeleteResult = await _apiHandler.ProcessRequest(manifestDeleteRequest) as DeleteObjectResult;
                                if (manifestDeleteResult != null)
                                {
                                    result.ActivityToken = manifestDeleteResult.ActivityToken;
                                    if (manifestDeleteResult.StatusCode == 200 || manifestDeleteResult.StatusCode == 204 || manifestDeleteResult.StatusCode == 404)
                                    {
                                        result.StatusCode = 200;
                                    }
                                }
                            }
                            else
                            {
                                result.StatusCode = objectDeletionResult.StatusCode;
                                result.StatusMessage = objectDeletionResult.StatusMessage;
                            }
                        }
                    }
                    else
                    {
                        result.StatusCode = 99;
                        result.StatusMessage = "Get manifest request gave NULL response";
                    }
                }
                else
                {
                    result.StatusCode = manifestResult.StatusCode;
                    result.StatusMessage = manifestResult.StatusMessage;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Exception processing manifest DELETE: {e.Message}");
                result.StatusCode = 99;
                result.StatusMessage = e.Message;
            }

            return await Task.FromResult(result);
        }

        private async Task<DeleteManifestResult> DeleteManifestChunks(DeleteManifestRequest request, IonburstManifest ionburstManifest)
        {
            DeleteManifestResult result = new DeleteManifestResult()
            {
                ManifestActivities = new List<Guid>()
            };

            if (ionburstManifest != null)
            {
                /*DeleteManifestChunk[] deleteChunkResults = new DeleteManifestChunk[ionburstManifest.Chunks.Count];
                for (int i = 0; i < deleteChunkResults.Length; i++)
                {
                    deleteChunkResults[i] = new DeleteManifestChunk();
                }

                foreach (IonburstChunk chunk in ionburstManifest.Chunks)
                {
                int arrayIndex = chunk.Ord - 1;
                DeleteObjectRequest chunkDeleteRequest = new DeleteObjectRequest()
                {
                    Particle = chunk.Id.ToString(),
                    Server = request.Server,
                    Routing = request.Routing
                };
                deleteChunkResults[arrayIndex].DeleteResult = await _apiHandler.ProcessRequest(chunkDeleteRequest) as DeleteObjectResult;
                */
                ConcurrentBag<DeleteManifestChunk> resultCollection = new ConcurrentBag<DeleteManifestChunk>();
                /*Parallel.ForEach(ionburstManifest.Chunks, async c =>
                {
                    resultCollection.Add(await DeleteManifestChunk(request, c));
                });*/
                var tasks = ionburstManifest.Chunks.Select(async c =>
                {
                    resultCollection.Add(await DeleteManifestChunkAsync(request, c));
                });
                await Task.WhenAll(tasks);

                // Check the results
                bool deleteFailed = false;
                Parallel.ForEach(resultCollection, i =>
                {
                    i.StatusCode = i.DeleteResult.StatusCode;
                    i.StatusMessage = i.DeleteResult.StatusMessage;
                });
                foreach (DeleteManifestChunk deleteChunkResult in resultCollection)
                {
                    result.ManifestActivities.Add(deleteChunkResult.DeleteResult.ActivityToken);
                    if (deleteChunkResult.StatusCode != 200 && deleteChunkResult.StatusCode != 204 && deleteChunkResult.StatusCode != 404)
                    {
                        deleteFailed = true;
                        if (deleteChunkResult.StatusCode == 429)
                        {
                            result.StatusCode = 429;
                            result.StatusMessage = "Manifest processing has been rate limited";
                            return await Task.FromResult(result);
                        }
                        else
                        {
                            result.StatusCode = deleteChunkResult.StatusCode;
                            result.StatusMessage = deleteChunkResult.StatusMessage;
                            return await Task.FromResult(result);
                        }
                    }
                }

                resultCollection.Clear();

                if (!deleteFailed)
                {
                    result.StatusCode = 200;
                }
            }

            return await Task.FromResult(result);
        }

        private async Task<DeleteManifestChunk> DeleteManifestChunkAsync(DeleteManifestRequest request, IonburstChunk chunk)
        {
            DeleteManifestChunk currentDeleteResult = new DeleteManifestChunk();
            DeleteObjectRequest chunkDeleteRequest = new DeleteObjectRequest()
            {
                Particle = chunk.Id.ToString(),
                Server = request.Server,
                Routing = request.Routing
            };
            currentDeleteResult.DeleteResult = await _apiHandler.ProcessRequest(chunkDeleteRequest) as DeleteObjectResult;
            return await Task.FromResult(currentDeleteResult);
        }

        public async Task<GetManifestResult> ProcessGetManifestRequest(GetManifestRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            GetManifestResult result = new GetManifestResult()
            {
                ManifestActivities = new List<Guid>()
            };

            try
            {
                // Get the manifest
                GetObjectRequest manifestRequest = new GetObjectRequest()
                {
                    Particle = request.Particle,
                    Server = request.Server,
                    Routing = request.Routing
                };
                GetObjectResult manifestResult = await _apiHandler.ProcessRequest(manifestRequest) as GetObjectResult;
                if (manifestResult != null)
                {
                    result.ActivityToken = manifestResult.ActivityToken;
                    if (manifestResult.StatusCode == 200)
                    {
                        StreamReader manifestReader = new StreamReader(manifestResult.DataStream);
                        string manifestJson = manifestReader.ReadToEnd();
                        IonburstManifest ionburstManifest = null;
                        try
                        {
                            ionburstManifest = JsonConvert.DeserializeObject<IonburstManifest>(manifestJson);
                        }
                        catch (Exception e)
                        {
                            // Doesn't look like this is a manifest
                            result.StatusCode = 400;
                            result.StatusMessage = $"{request.Particle} does not appear to be a manifest: {e.Message}";
                        }
                        if (ionburstManifest != null)
                        {
                            /*GetManifestChunk[] getChunkResults = new GetManifestChunk[ionburstManifest.Chunks.Count];
                            for (int i = 0; i < getChunkResults.Length; i++)
                            {
                                getChunkResults[i] = new GetManifestChunk();
                            }

                            foreach (IonburstChunk chunk in ionburstManifest.Chunks)
                            {
                                int arrayIndex = chunk.Ord - 1;
                                getChunkResults[arrayIndex].Chunk = chunk;
                                GetObjectRequest chunkGetRequest = new GetObjectRequest()
                                {
                                    Particle = chunk.Id.ToString(),
                                    Server = request.Server,
                                    Routing = request.Routing
                                };
                                getChunkResults[arrayIndex].GetResult = await _apiHandler.ProcessRequest(chunkGetRequest) as GetObjectResult;
                            }*/
                            ConcurrentBag<GetManifestChunk> resultCollection = new ConcurrentBag<GetManifestChunk>();
                            /*Parallel.ForEach(ionburstManifest.Chunks, async c =>
                            {
                                resultCollection.Add(await GetManifestChunk(request, c));
                            });*/
                            var tasks = ionburstManifest.Chunks.Select(async c =>
                            {
                                resultCollection.Add(await GetManifestChunkAsync(request, c));
                            });
                            await Task.WhenAll(tasks);

                            // Check the results
                            bool getFailed = false;
                            Parallel.ForEach(resultCollection, i =>
                            {
                                i.StatusCode = i.GetResult.StatusCode;
                                i.StatusMessage = i.GetResult.StatusMessage;
                            });
                            foreach (GetManifestChunk getChunkResult in resultCollection)
                            {
                                result.ManifestActivities.Add(getChunkResult.GetResult.ActivityToken);
                                if (getChunkResult.GetResult.StatusCode != 200)
                                {
                                    getFailed = true;
                                }
                                else
                                {
                                    getChunkResult.GetResult.DataStream.Seek(0, SeekOrigin.Begin);
                                    byte[] hashBytes = SHA256.Create().ComputeHash(getChunkResult.GetResult.DataStream);
                                    if (getChunkResult.Chunk.Hash != Convert.ToBase64String(hashBytes))
                                    {
                                        // Hash doesn't match
                                        getFailed = true;
                                    }
                                }
                            }

                            if (!getFailed)
                            {
                                using MemoryStream objectStream = new MemoryStream();
                                /*foreach (GetManifestChunk getChunkResult in resultCollection)
                                {
                                    getChunkResult.GetResult.DataStream.Seek(0, SeekOrigin.Begin);
                                    getChunkResult.GetResult.DataStream.CopyTo(objectStream);
                                }*/
                                // Who knows what order the chunks in ConcurrentBag will be
                                for (int i = 0; i < resultCollection.Count; i++)
                                {
                                    GetManifestChunk getChunkResult = resultCollection.First(cr => cr.Chunk.Ord == i + 1);
                                    getChunkResult.GetResult.DataStream.Seek(0, SeekOrigin.Begin);
                                    await getChunkResult.GetResult.DataStream.CopyToAsync(objectStream);
                                }
                                foreach (GetManifestChunk getChunkResult in resultCollection)
                                {
                                    getChunkResult.GetResult.DataStream.Dispose();
                                    getChunkResult.GetResult.DataStream = null;
                                }

                                // The object stream is going to be disposed outside this scope so SDL client gets a copy to handle
                                result.DataStream = new MemoryStream();
                                objectStream.Seek(0, SeekOrigin.Begin);
                                await objectStream.CopyToAsync(result.DataStream);
                                result.DataStream.Seek(0, SeekOrigin.Begin);
                                result.StatusCode = 200;
                            }
                            else
                            {
                                // Was there something like rate limiting?
                                foreach (GetManifestChunk getChunkResult in resultCollection)
                                {
                                    if (getChunkResult.StatusCode == 429)
                                    {
                                        result.StatusCode = 429;
                                        result.StatusMessage = "Manifiest processing halted by rate limiting";
                                        break;
                                    }
                                    if (getChunkResult.StatusCode == 403)
                                    {
                                        result.StatusCode = 403;
                                        result.StatusMessage = "Manifiest processing halted because party quota was exceeded";
                                        break;
                                    }
                                }
                            }

                            // Clear chunk result collection
                            resultCollection.Clear();
                        }
                    }
                }
                else
                {
                    result.StatusCode = 99;
                    result.StatusMessage = "Get manifest request gave NULL response";
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Exception processing manifest GET: {e.Message}");
                result.StatusCode = 99;
                result.StatusMessage = e.Message;
            }

            return await Task.FromResult(result);
        }

        private async Task<GetManifestChunk> GetManifestChunkAsync(GetManifestRequest request, IonburstChunk chunk)
        {
            GetManifestChunk currentGetResult = new GetManifestChunk()
            {
                Chunk = chunk
            };
            GetObjectRequest chunkGetRequest = new GetObjectRequest()
            {
                Particle = chunk.Id.ToString(),
                Server = request.Server,
                Routing = request.Routing
            };
            currentGetResult.GetResult = await _apiHandler.ProcessRequest(chunkGetRequest) as GetObjectResult;
            return await Task.FromResult(currentGetResult);
        }

        public async Task<PutManifestResult> ProcessPutManifestRequest(PutManifestRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            PutManifestResult result = new PutManifestResult
            {
                StatusMessage = string.Empty,
                ManifestActivities = new List<Guid>()
            };

            try
            {
                // Validate chunk size
                long dataLimit = await _apiHandler.GetUploadSizeLimit($"{_server}{_dataPath}query/uploadsizelimit");
                if (request.ChunkSize > dataLimit)
                {
                    result.StatusCode = 400;
                    result.StatusMessage = $"Requested chunk size cannot exceed {dataLimit}";
                    return await Task.FromResult(result);
                }
                if (request.ChunkSize < MIN_CHUNK)
                {
                    result.StatusCode = 400;
                    result.StatusMessage = $"Requested chunk size cannot be less than {MIN_CHUNK}";
                    return await Task.FromResult(result);
                }

                // Check nothing with the external reference exists
                CheckObjectRequest checkRequest = new CheckObjectRequest()
                {
                    Particle = request.Particle,
                    Server = request.Server,
                    Routing = request.Routing
                };
                CheckObjectResult checkResult = await _apiHandler.ProcessRequest(checkRequest) as CheckObjectResult;
                if (checkResult != null && checkResult.StatusCode != 200)
                {
                    // Do initial chunking calculations
                    long inputSize = request.DataStream.Length;
                    long offset = request.ChunkSize;
                    long chunks = (inputSize / offset) + (inputSize % offset > 0 ? 1 : 0);

                    // Create the manifest
                    IonburstManifest manifest = new IonburstManifest()
                    {
                        Name = request.Particle,
                        Size = inputSize,
                        ChunkCount = chunks,
                        ChunkSize = offset
                    };

                    bool waitForPuts = true;
                    try
                    {
                        using (BinaryReader binaryReader = new BinaryReader(request.DataStream))
                        {
                            int i = 0;
                            for (long l = 0; l < inputSize; l += offset)
                            {
                                long boundary = l + offset;
                                if (boundary > inputSize)
                                {
                                    boundary = inputSize;
                                }
                                long currentChunkSize = boundary - l;

                                IonburstChunk newChunk = new IonburstChunk()
                                {
                                    Ord = i + 1
                                };

                                byte[] buffer = new byte[currentChunkSize];
                                binaryReader.BaseStream.Seek(l, SeekOrigin.Begin);
                                binaryReader.Read(buffer, 0, (int)currentChunkSize);

                                byte[] hashBytes = SHA256.Create().ComputeHash(buffer);
                                newChunk.Hash = Convert.ToBase64String(hashBytes);

                                manifest.Chunks.Add(newChunk);

                                PutObjectRequest chunkRequest = new PutObjectRequest()
                                {
                                    Particle = newChunk.Id.ToString(),
                                    DataStream = new MemoryStream(buffer),
                                    Server = request.Server,
                                    Routing = request.Routing,
                                    RequestResult = new ResultDelegate(HandleChunkPutComplete),
                                    DelegateTag = newChunk.Id.ToString(),
                                    RequestTimeout = new TimeSpan(0, 5, 0),
                                    TimeoutSpecified = true
                                };
                                if (request.PolicyClassification != null && request.PolicyClassification != string.Empty)
                                {
                                    chunkRequest.PolicyClassification = request.PolicyClassification;
                                }
                                else
                                {
                                    chunkRequest.PolicyClassificationId = request.PolicyClassificationId;
                                }
                                _ = InvokeChunkPut(chunkRequest);
                                chunkRequest.DataStream.Dispose();
                                buffer = null;
                                i++;
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"Exception chunking data and making PUT requests: {e.Message}");
                        result.StatusCode = 99;
                        result.StatusMessage = $"Exception chunking data and making PUT requests: {e.Message}";
                        waitForPuts = false;
                    }

                    int stopCounter = 0;
                    while (waitForPuts && _putResultCollection.Count < chunks && stopCounter++ < 1000000)
                    {
                        await Task.Delay(5);
                    }

                    // Check the chunk put results
                    /*Parallel.ForEach(putChunkResults, i =>
                    {
                        i.StatusCode = i.PutResult.StatusCode;
                        i.StatusMessage = i.PutResult.StatusMessage;
                    });*/
                    /*foreach (PutManifestChunk chunkResult in putChunkResults)
                    {
                        chunkResult.StatusCode = chunkResult.PutResult.StatusCode;
                        chunkResult.StatusMessage = chunkResult.PutResult.StatusMessage;
                    }*/
                    if (_putResultCollection.Count == chunks)
                    {
                        foreach (PutManifestChunk chunkResult in _putResultCollection)
                        {
                            if (chunkResult.PutResult != null)
                            {
                                result.ManifestActivities.Add(chunkResult.PutResult.ActivityToken);
                                if (chunkResult.PutResult.StatusCode != 200)
                                {
                                    // Failed
                                    result.StatusCode = chunkResult.PutResult.StatusCode;
                                    result.StatusMessage = chunkResult.PutResult.StatusMessage;
                                    if (result.StatusCode == 429)
                                    {
                                        result.StatusMessage = "Manifiest processing halted by rate limiting";
                                        break;
                                    }
                                    if (result.StatusCode == 403)
                                    {
                                        result.StatusMessage = "Manifiest processing halted because party quota was exceeded";
                                        break;
                                    }
                                }
                            }
                            else
                            {
                                // Failed
                                result.StatusCode = 99;
                                result.StatusMessage = "Unexpected NULL PutObjectResult";
                                break;
                            }
                        }
                    }
                    else
                    {
                        result.StatusCode = 99;
                        result.StatusMessage = "Failed to store all chunks";
                    }

                    if (result.StatusCode == 0)
                    {
                        // Store the manifest
                        string serializedManifest = JsonConvert.SerializeObject(manifest);
                        byte[] manifestBytes = Encoding.Default.GetBytes(serializedManifest);
                        MemoryStream manifestStream = new MemoryStream(manifestBytes);
                        {
                            PutObjectRequest manifestRequest = new PutObjectRequest()
                            {
                                Particle = request.Particle,
                                DataStream = manifestStream,
                                Server = request.Server,
                                Routing = request.Routing
                            };
                            if (request.PolicyClassification != null && request.PolicyClassification != string.Empty)
                            {
                                manifestRequest.PolicyClassification = request.PolicyClassification;
                            }
                            else
                            {
                                manifestRequest.PolicyClassificationId = request.PolicyClassificationId;
                            }
                            PutObjectResult manifestResult = await _apiHandler.ProcessRequest(manifestRequest) as PutObjectResult;
                            if (manifestResult != null)
                            {
                                result.ActivityToken = manifestResult.ActivityToken;
                                if (manifestResult.StatusCode == 200)
                                {
                                    result.StatusCode = 200;
                                }
                            }
                        }
                        manifestStream.Dispose();
                    }
                    else
                    {
                        DeleteManifestRequest cleanupRequest = new DeleteManifestRequest()
                        {
                            Server = request.Server,
                            Routing = request.Routing
                        };
                        // Need to clear any stored chunks
                        _ = DeleteManifestChunks(cleanupRequest, manifest);
                        // In theory delete cleaner will pick up any that failed
                    }
                }
                else
                {
                    result.StatusCode = 409;
                    result.StatusMessage = $"External reference {request.Particle} already exists in Ionburst";
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Exception processing manifest PUT: {e.Message}");
                result.StatusCode = 99;
                result.StatusMessage = e.Message;
            }

            return await Task.FromResult(result);
        }

        private async Task<Task> InvokeChunkPut(PutObjectRequest request)
        {
            PutObjectResult result = await _apiHandler.ProcessRequest(request) as PutObjectResult;
            result.DelegateTag = request.DelegateTag;
            request.RequestResult?.Invoke(result);
            request.DataStream.Close();
            request.DataStream.Dispose();
            return Task.CompletedTask;
        }

        private void HandleChunkPutComplete(IObjectResult result)
        {
            PutManifestChunk manifestChunkResult = new PutManifestChunk();
            manifestChunkResult.PutResult = result as PutObjectResult;
            manifestChunkResult.StatusCode = manifestChunkResult.PutResult.StatusCode;
            manifestChunkResult.StatusMessage = manifestChunkResult.PutResult.StatusMessage;
            _putResultCollection.Add(manifestChunkResult);    
        }
    }
}
