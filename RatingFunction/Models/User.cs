using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace web_api_project.Models
{
    public class User
    {
        public string userid { get; set; }
        public string name { get; set; }
        public string surename { get; set; }
        public string username { get; set; }
        public string mail_adress { get; set; }
        public string department { get; set; }
        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}
