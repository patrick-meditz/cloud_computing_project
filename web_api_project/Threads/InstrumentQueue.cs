using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Azure.Storage.Blobs;
using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using Newtonsoft.Json;
using web_api_project.Models;
using Microsoft.Azure.Cosmos;

namespace Web_Api.Threads
{
    public class InstrumentQueue
    {
        QueueClient queue;
        string QueueName = "addinstrumentqueue";
        private CosmosClient _cosmosClient;
        private Database _database;
        private Container _container;

        public InstrumentQueue(IConfiguration configuration)
        {

            CreateClientAndDatabase(configuration);
        }

        private void CreateClientAndDatabase(IConfiguration configuration)
        {
            //Init queue

            queue = new QueueClient(configuration.GetConnectionString("azurecsmeditz"), QueueName);
            queue.CreateIfNotExists();
            //Init CosmosDB
            _cosmosClient = new CosmosClient(configuration.GetConnectionString("azurecsmeditzcosmos"));
            _cosmosClient.CreateDatabaseIfNotExistsAsync("ccstandarddb");
            _database = _cosmosClient.GetDatabase("ccstandarddb");
            _database.CreateContainerIfNotExistsAsync("instrument", "/id");
            _container = _cosmosClient.GetContainer("ccstandarddb", "instrument");
        }

        public async void run()
        {
            while (true)
            {
                foreach (QueueMessage message in queue.ReceiveMessages(maxMessages: 10).Value)
                {
                    Instrument newInstrument = JsonConvert.DeserializeObject<Instrument>(message.Body.ToString());

                    try
                    {
                        var item = await _container.CreateItemAsync<Instrument>(newInstrument, new PartitionKey(newInstrument.ISBN));
                        // Log message to console
                        Console.WriteLine($"Message: {message.Body}");

                        // Let the service know we're finished with the message and
                        // it can be safely deleted.
                        queue.DeleteMessage(message.MessageId, message.PopReceipt);

                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"Adding book failed with error: {e}");
                        throw;
                    }
                }
            }
        }
    }
}
