using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace web_api_project.Models
{
    public class Instrument
    {
        public string instrumentid { get; set; }
        public string manufacture { get; set; }
        public string model { get; set; }
        public int year_of_manufacture { get; set; }
        public string colour { get; set; }
        public float price { get; set; }
        public string type { get; set; }
        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}
