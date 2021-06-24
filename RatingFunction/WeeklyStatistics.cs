using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Table;
using web_api_project.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Azure.Cosmos;
using User = web_api_project.Models.User;

namespace CreateStatisticsNew
{
    public static class TimeFunction
    {
        [FunctionName("WeeklyFunction")]
        [return: Table("Bewertung", Connection = "AzureWebJobsStorage")]
        public static Statistics Run([TimerTrigger("* * * * * 0")] TimerInfo myTimer, ILogger log, [CosmosDB(
        databaseName: "ccstandarddb",
        collectionName: "instruments",
        ConnectionStringSetting = "azurecosmoscsmeditz",
        SqlQuery = "select * from instruments")] IEnumerable<Instrument> instruments, [CosmosDB(
        databaseName: "ccstandarddb",
        collectionName: "users",
        ConnectionStringSetting = "azurecosmoscsmeditz",
        SqlQuery = "select * from users")] IEnumerable<User>  users)
        {

            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");

            int countInstruments = 0;
            int countUsers = 0;

            Hashtable statistic = new Hashtable();

            foreach (Instrument instrument in instruments)
            {
                log.LogInformation(instrument.model);
                countInstruments++;
                if (!statistic.Contains(instrument.model))
                    statistic.Add(instrument.model, 1);
            }

            foreach (User user in users)
            {
                log.LogInformation(user.userid);
                countUsers++;
            }

            return new Statistics
            {
                PartitionKey = DateTime.Now.DayOfYear.ToString(),
                RowKey = Guid.NewGuid().ToString(),
                sumInstruments = countInstruments.ToString(),
                sumModels = statistic.Count.ToString(),
                sumUser = countUsers.ToString()
            };
        }
    }
}