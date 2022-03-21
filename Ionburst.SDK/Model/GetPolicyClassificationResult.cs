// Copyright Ionburst Limited 2019
using System.Collections.Generic;

namespace Ionburst.SDK.Model
{
    public class GetPolicyClassificationResult : ObjectResult
    {
        public List<string> ClassificationList { get; set; }
        public List<int> ClassificationIdList { get; set; }
        public Dictionary<int,string> ClassificationDictionary { get; set; }
    }
}
