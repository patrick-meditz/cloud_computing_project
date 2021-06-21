using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;
using Web_Api.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Web_Api.Controllers
{
    [Route("/[controller]")]
    [ApiController]
    public class userController : ControllerBase
    {
        private readonly ILogger<userController> _logger;
        private CosmosClient _cosmosClient;
        private Database _database;
        private Container _container;

        public userController(ILogger<userController> logger, IConfiguration configuration)
        {
            _logger = logger;
            CreateClientAndDatabase(configuration);
        }

        private void CreateClientAndDatabase(IConfiguration configuration)
        {
            _cosmosClient = new CosmosClient(configuration.GetConnectionString("CosmosDBString")); ;

            _cosmosClient.CreateDatabaseIfNotExistsAsync("ccstandarddb");
            _database = _cosmosClient.GetDatabase("ccstandarddb");
            _database.CreateContainerIfNotExistsAsync("users", "/id");
            _container = _cosmosClient.GetContainer("ccstandarddb", "users");
            // _logger.LogInformation("Container found!");
        }

        // GET: api/<UserController>
        [HttpGet]
        public IEnumerable<Web_Api.Models.User> GetAll()
        {
            //Web_Api.Models.User user1 = new Web_Api.Models.User("herbert", "huber", "hhuber@test.at", "sajdkasdwieqn");
            //Web_Api.Models.User user2 = new Web_Api.Models.User("maria", "mia", "mmia@test.at", "hana34adgbvdfg");

            Web_Api.Models.User user1 = new Web_Api.Models.User();
            Web_Api.Models.User user2 = new Web_Api.Models.User();

            return new Web_Api.Models.User[] { user1, user2 };
        }

        // GET api/<UserController>/5
        [HttpGet("{id}")]
        public string Get(int id)
        {
            return "value";
        }

        // POST api/<UserController>
        [HttpPost("{id}")]
        public void Post([FromBody] string value)
        {
        }

        // PUT api/<UserController>/5
        [HttpPut]
        public void Put(int id, [FromBody] string value)
        {
        }

        // DELETE api/<UserController>/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
    }
}
