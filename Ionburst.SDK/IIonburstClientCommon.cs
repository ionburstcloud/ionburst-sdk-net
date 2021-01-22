// Copyright Ionburst Limited 2021
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Ionburst.SDK.Model;

namespace Ionburst.SDK
{
    public interface IIonburstClientCommon
    {
        GetPolicyClassificationResult GetClassifications(GetPolicyClassificationRequest request);
        Task<GetPolicyClassificationResult> GetClassificationsAsync(GetPolicyClassificationRequest request);
        bool GetClassificationsWithCallback(GetPolicyClassificationRequest request);

        Task<long> GetUploadSizeLimit();

        Task<string> GetConfiguredUri();

        [Obsolete("Use CheckIonburstAPI")]
        Task<bool> CheckIonBurstAPI();

        Task<bool> CheckIonburstAPI();

        Task<List<string>> GetVersionDetails();

        void SimulateBadTokenForTesting();
    }
}
