// Copyright Ionburst Limited 2021
using System.Threading.Tasks;
using System.Collections.Generic;

using Ionburst.SDK.Model;
using System;

namespace Ionburst.SDK
{
    public interface IIonburstDataMethods
    {
        Task<long> GetDataUploadSizeLimit();

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
    }
}
