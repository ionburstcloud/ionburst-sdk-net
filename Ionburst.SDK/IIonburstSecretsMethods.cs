// Copyright Ionburst Limited 2021
using System.Threading.Tasks;
using System.Collections.Generic;

using Ionburst.SDK.Model;
using System;

namespace Ionburst.SDK
{
    public interface IIonburstSecretsMethods
    {
        Task<long> GetSecretsUploadSizeLimit();

        CheckObjectResult SecretsCheck(CheckObjectRequest request);
        Task<CheckObjectResult> SecretsCheckAsync(CheckObjectRequest request);
        bool SecretsCheckWithCallback(CheckObjectRequest request);

        DeleteObjectResult SecretsDelete(DeleteObjectRequest request);
        Task<DeleteObjectResult> SecretsDeleteAsync(DeleteObjectRequest request);
        bool SecretsDeleteWithCallback(DeleteObjectRequest request);

        GetObjectResult SecretsGet(GetObjectRequest request);
        Task<GetObjectResult> SecretsGetAsync(GetObjectRequest request);
        bool SecretsGetWithCallback(GetObjectRequest request);

        PutObjectResult SecretsPut(PutObjectRequest request);
        Task<PutObjectResult> SecretsPutAsync(PutObjectRequest request);
        bool SecretsPutWithCallback(PutObjectRequest request);

        Task<DeferredActionResult> SecretsStartDeferredActionAsync(ObjectRequest request);
        Task<DeferredCheckResult> SecretsCheckDeferredActionAsync(ObjectRequest request);
    }
}
