using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Queues;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using web_api_project.Models;


// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Web_Api.Controllers
{
    [Route("/[controller]")]
    [ApiController]
    public class InstrumentController : ControllerBase
    {
        private readonly ILogger<InstrumentController> _logger;
        private CosmosClient _cosmosClient;
        private Database _database;
        private Container _container;

        private string containerName1 = "guitar";
        private string containerName2 = "drums";
        private string containerName3 = "piano";
        private string containerName4 = "bass";
        private string InstrumentQueueName1 = "addinstrument";
        private string InstrumentQueueName2 = "deleteinstrument";
        private BlobServiceClient blobServiceClient;
        private BlobContainerClient containerClient1;
        private BlobContainerClient containerClient2;
        private BlobContainerClient containerClient3;
        private BlobContainerClient containerClient4;
        private QueueClient queue1;
        private QueueClient queue2;

        //Test, only used in Azure Function
        /*
        private CloudStorageAccount storageAccount;
        private CloudTableClient tableClient;
        private string tableName = "Statistik";
        private CloudTable table;
        */

        public InstrumentController(ILogger<InstrumentController> logger, IConfiguration configuration)
        {
            _logger = logger;
            CreateClientAndDatabase(configuration);
        }

        private void CreateClientAndDatabase(IConfiguration configuration)
        {
            //Init CosmosDB
            _cosmosClient = new CosmosClient(configuration.GetConnectionString("azurecsmeditzcosmos")); ;
            _cosmosClient.CreateDatabaseIfNotExistsAsync("ccstandarddb");
            _database = _cosmosClient.GetDatabase("ccstandarddb");
            _database.CreateContainerIfNotExistsAsync("instrument", "/id");
            _container = _cosmosClient.GetContainer("ccstandarddb", "instrument");
            // _logger.LogInformation("Container found!");

            //Init Blob Storage
            //Container created in deployment
            blobServiceClient = new BlobServiceClient((configuration.GetConnectionString("azurecsmeditz")));
            containerClient1 = blobServiceClient.GetBlobContainerClient(containerName1);
            containerClient1.CreateIfNotExists();
            containerClient2 = blobServiceClient.GetBlobContainerClient(containerName2);
            containerClient2.CreateIfNotExists();
            containerClient3 = blobServiceClient.GetBlobContainerClient(containerName3);
            containerClient3.CreateIfNotExists();
            containerClient4 = blobServiceClient.GetBlobContainerClient(containerName4);
            containerClient4.CreateIfNotExists();

            //Init queue
            queue1 = new QueueClient(configuration.GetConnectionString("azurecsmeditz"), InstrumentQueueName1);
            queue1.CreateIfNotExists();
            queue2 = new QueueClient(configuration.GetConnectionString("azurecsmeditz"), InstrumentQueueName2);
            queue2.CreateIfNotExists();


            //Init TableClient
            //Test, only used in Azure Function
            /*
            storageAccount = CloudStorageAccount.Parse(configuration.GetConnectionString("AzureStorageConnect"));
            tableClient = storageAccount.CreateCloudTableClient(new TableClientConfiguration());
            table = tableClient.GetTableReference(tableName);
            table.CreateIfNotExists();
            */
        }

        // GET: api/<ValuesController>
        [HttpGet]
        public IActionResult GetAll()
        {
            List<Instrument> instruments = new List<Instrument>();
            foreach (Instrument matchingInstrument in _container.GetItemLinqQueryable<Instrument>(true))
            {
                instruments.Add(matchingInstrument);
            }

            return Ok(instruments);
        }

        // GET api/<ValuesController>/5
        [HttpGet("{id}")]
        public IActionResult Get(string id)
        {
            Instrument instrument = new Instrument();

            foreach (Instrument matchingInstrument in _container.GetItemLinqQueryable<Instrument>(true)
                       .Where(b => b.instrumentid == id))
            {
                instrument = matchingInstrument;
            }

            if (instrument.instrumentid == null)
            {
                return new NotFoundResult();
            }
            return Ok(instrument);
        }

        // POST api/<ValuesController>
        [HttpPost("{id}")]
        public async Task<IActionResult> Post([FromBody] Instrument request)
        {
            ItemResponse<Instrument> response = await _container.ReplaceItemAsync(
                partitionKey: new PartitionKey(request.instrumentid),
                id: request.instrumentid,
                item: request);

            Instrument updated = response.Resource;
            return Ok(updated);
        }

        // PUT api/<ValuesController>/5
        [HttpPut]
        public IActionResult Put([FromBody] Instrument request)
        {
            //Web_Api.Models.Users user2 = new Web_Api.Models.Users();
            //IEnumerable<Web_Api.Models.Books> allBooks = this.GetAll();
            //return user;
            //return allBooks;

            var newInstrument = new Instrument
            {
                instrumentid = request.instrumentid,
                manufacture = request.manufacture,
                model = request.model,
                year_of_manufacture = request.year_of_manufacture,
                colour = request.colour,
                price = request.price,
                type = request.type,
            };


            try
            {
                var json = JsonConvert.SerializeObject(newInstrument);
                //var item = await _container.CreateItemAsync<Book>(newBook, new PartitionKey(newBook.ISBN));
                queue1.SendMessage(json);
                //newBook.ETag = item.ETag;
            }
            catch (Exception e)
            {
                _logger.LogError($"Adding book failed with error: {e}");
                throw;
            }

            return Ok(newInstrument);
        }


        // DELETE api/<ValuesController>/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            ItemResponse<Instrument> response = await _container.DeleteItemAsync<Instrument>(
                partitionKey: new PartitionKey(id),
                id: id);

            //Book deleted = response.StatusCode;
            return Ok();

        }


        [HttpGet("/image/")]
        public IActionResult ImageGet(string blobName)
        {
            //return "value";

            BlobImage returnBlob = new BlobImage();
            returnBlob.blobName = blobName;

            BlobClient blob = containerClient1.GetBlobClient(blobName);
            byte[] content;

            try
            {
                BlobDownloadResult downloadBlob = blob.DownloadContent();
                content = downloadBlob.Content.ToArray();
            }
            catch (Exception e)
            {
                Console.WriteLine($"Getting blob failed with error: {e}");
                return new NotFoundResult();
            }

            //byte [] content = downloadBlob.Content.ToArray();
            byte[] decompressedContent;

            //Gzip Decompressor
            using (var compressedStream = new MemoryStream(content))
            using (var zipStream = new GZipStream(compressedStream, CompressionMode.Decompress))
            using (var resultStream = new MemoryStream())
            {
                zipStream.CopyTo(resultStream);
                decompressedContent = resultStream.ToArray();
            }

            // Convert the array to a base 64 string.
            string data = Convert.ToBase64String(decompressedContent);

            returnBlob.Base64Data = data;

            //Debug to console
            Console.WriteLine(data);

            //Save file if needed
            /*
            string localPath = "./data/";
            string fileName = Guid.NewGuid().ToString() + ".txt";
            string localFilePath = Path.Combine(localPath, fileName);
            string downloadFilePath = localFilePath.Replace(".txt", ".jpg");
            Console.WriteLine("\nDownloading blob to\n\t{0}\n", downloadFilePath);
            // Download the blob's contents and save it to a file
            using (BinaryWriter writer = new BinaryWriter(System.IO.File.Open(downloadFilePath, FileMode.Create)))
            {
                writer.Write(downloadBlob.Content);
            }
            */

            return Ok(returnBlob);
        }

        [HttpPut("/image")]
        public IActionResult PutImage([FromBody] BlobImage uploadBlob)
        {

            string type = uploadBlob.type;
            BlobClient blob = null;

            if (type == "guitar") {
                 blob = containerClient1.GetBlobClient(uploadBlob.blobName);
                
            }
            else if (type == "drums")
            {
                 blob = containerClient2.GetBlobClient(uploadBlob.blobName);
            }
            else if (type == "piano")
            {
                 blob = containerClient3.GetBlobClient(uploadBlob.blobName);
            }
            else if (type == "bass")
            {
                  blob = containerClient4.GetBlobClient(uploadBlob.blobName);
            }
            else
            {
                Console.WriteLine("invalid container");
                
            }


            byte[] content = Convert.FromBase64String(uploadBlob.Base64Data);
       


            //File compressor Gzip
            using (var compressedStream = new MemoryStream())
            using (var zipStream = new GZipStream(compressedStream, CompressionMode.Compress))
            {
                zipStream.Write(content, 0, content.Length);
                zipStream.Close();
               
            }

            //Compressor Debug
            Console.WriteLine("Uncompressed: {0}", content.Length);
            

            //Create binary object and upload
            BinaryData data = new BinaryData(content);
            blob.Upload(data);

            //Debug to console
            Console.WriteLine(data);
            return Ok();
        }

        [HttpDelete("/image")]
        public IActionResult DeleteImage(string blobName)
        {
            BlobClient blob = containerClient1.GetBlobClient(blobName);
            Azure.Response<bool> response = blob.DeleteIfExists();

            return Ok(response);
        }

        // POST api/<ValuesController>
        [HttpPost("/image")]
        public IActionResult PostImage([FromBody] BlobImage replaceBlob)
        {
            //A post to an existing object changes the content
            BlobClient blob = containerClient1.GetBlobClient(replaceBlob.blobName);
            byte[] content = Convert.FromBase64String(replaceBlob.Base64Data);
            BinaryData data = new BinaryData(content);

            blob.Upload(data);

            //Debug to console
            Console.WriteLine(data);
            return Ok();
        }
    }
}

