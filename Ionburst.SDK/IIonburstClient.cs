// Copyright Ionburst Limited 2019
using System.Threading.Tasks;
using System.Collections.Generic;

using Ionburst.SDK.Model;

namespace Ionburst.SDK
{
    public interface IIonburstClient
    {
        DeleteObjectResult Delete(DeleteObjectRequest request);
        Task<DeleteObjectResult> DeleteAsync(DeleteObjectRequest request);
        bool DeleteWithCallback(DeleteObjectRequest request);

        GetObjectResult Get(GetObjectRequest request);
        Task<GetObjectResult> GetAsync(GetObjectRequest request);
        bool GetWithCallback(GetObjectRequest request);

        PutObjectResult Put(PutObjectRequest request);
        Task<PutObjectResult> PutAsync(PutObjectRequest request);
        bool PutWithCallback(PutObjectRequest request);
        PutObjectResult RePut(PutObjectRequest request);
        Task<PutObjectResult> RePutAsync(PutObjectRequest request);

        Task<DeferredActionResult> StartDeferredActionAsync(ObjectRequest request);
        Task<DeferredCheckResult> CheckDeferredActionAsync(ObjectRequest request);

        GetPolicyClassificationResult GetClassifications(GetPolicyClassificationRequest request);
        Task<GetPolicyClassificationResult> GetClassificationsAsync(GetPolicyClassificationRequest request);
        bool GetClassificationsWithCallback(GetPolicyClassificationRequest request);

        Task<bool> CheckIonBurstAPI();

        Task<List<string>> GetVersionDetails();

        void SimulateBadTokenForTesting();
    }
}
