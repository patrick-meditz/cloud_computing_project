using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Text;

namespace web_api_project.Models
{    public class Statistics : TableEntity
    {
        public string sumInstruments { get; set; }
        public string sumModels { get; set; }
        public string sumUser { get; set; }
    }
}