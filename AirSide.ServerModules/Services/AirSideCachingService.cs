using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AirSide.ServerModules.Services
{
    class AirSideCachingService
    {
        private readonly string r_end_point;
        private readonly string r_auth_key;
        private readonly DocumentClient client;

        public AirSideCachingService() { }

        public AirSideCachingService(string end_point, string auth_key)
        {
            r_end_point = end_point;
            r_auth_key = auth_key;
            client = new DocumentClient(new Uri(r_end_point), r_auth_key);
        }



        #region Database and Collection Tasks

        public async Task<Database> CreateOrReadDatabase(string databaseName)
        {
            if (client.CreateDatabaseQuery().Where(x => x.Id == databaseName).AsEnumerable().Any())
            {
                return client.CreateDatabaseQuery().Where(x => x.Id == databaseName).AsEnumerable().FirstOrDefault();
            }
            return await client.CreateDatabaseAsync(new Database { Id = databaseName });
        }
        
        public async Task<DocumentCollection> CreateOrReadCollection(Database database, string collectionName)
        {
            if (client.CreateDocumentCollectionQuery(database.SelfLink).Where(c => c.Id == collectionName).ToArray().Any())
            {
                return client.CreateDocumentCollectionQuery(database.SelfLink).Where(c => c.Id == collectionName).ToArray().FirstOrDefault();
            }
            return await client.CreateDocumentCollectionAsync(database.SelfLink, new DocumentCollection { Id = collectionName });
        }

        #endregion
    }
}
