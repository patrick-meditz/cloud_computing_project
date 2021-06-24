using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace web_api_project.Models
{
        public class BlobImage
        {
            public string blobName { get; set; }
            public string type { get; set; }

            public string Base64Data { get; set; }

            public override string ToString()
            {
                return JsonConvert.SerializeObject(this);
            }
        }
    }
