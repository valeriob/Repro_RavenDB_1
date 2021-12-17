using Raven.Client.Documents;
using Raven.Client.Documents.Indexes;
using Raven.Client.Documents.Queries;
using Raven.Client.Documents.Session;
using Raven.TestDriver;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace RavenDbProjection
{
    public class IndexStoreFields : RavenTestDriver
    {
        static IndexStoreFields()
        {
            ConfigureServer(new TestServerOptions
            {
                FrameworkVersion = null,
            });
        }

        [Fact]
        public async Task Do()
        {
            var store = GetDocumentStore();

            var id = "myEntity/1";
            var entityValue = 10;
            var value = 100;

            var e = new MyEntity
            {
                Id = id,
                Name = "Test",
                EntityValue = entityValue,
                EntityValue2 = entityValue,
                Details = new MyEntity.EntityDetails
                {
                    Description = "Test Description",
                    Value = value,
                }
            };

            using (var s = store.OpenAsyncSession())
            {
                await new MyEntityIndexStore().ExecuteAsync(store);
                await s.StoreAsync(e);
                await s.SaveChangesAsync();
            }

            WaitForIndexing(store);

            using (var s = store.OpenAsyncSession())
            {
                var r1 = await Query1_OK(s);
                Assert.Collection(r1, a => Assert.Equal(entityValue, a.EntityValue));
                Assert.Collection(r1, a => Assert.Equal(entityValue, a.EntityValue2));

                var r2 = await FromIndex_field_not_stored_on_index_should_be_loaded_from_document(s);
                Assert.Collection(r2, a => Assert.Equal(entityValue, a.EntityValue));
                Assert.Collection(r2, a => Assert.Equal(entityValue, a.EntityValue2));
                Assert.Collection(r2, a => Assert.Equal(value, a.Details_Value));
            }
        }

        async Task<MyEntityDto[]> Query1_OK(IAsyncDocumentSession s)
        {
            var q = s.Query<MyEntity>().Select(r => new MyEntityDto
            {
                Id = r.Id,
                Name = r.Name,
                EntityValue = r.EntityValue,
                EntityValue2 = r.EntityValue2,
                Details_Description = r.Details.Description,
                Details_Value = r.Details.Value
            });
            return await q.ToArrayAsync();
        }

        async Task<MyEntityDto[]> FromIndex_field_not_stored_on_index_should_be_loaded_from_document(IAsyncDocumentSession s)
        {
            var q = s.Query<MyEntity, MyEntityIndexStore>();
            var p = from r in q.Customize(r=> r.Projection(ProjectionBehavior.FromIndex))
                    select new MyEntityDto
                    {
                        Id = r.Id,
                        Name = r.Name,
                        EntityValue = r.EntityValue,
                        EntityValue2 = r.EntityValue2,
                        Details_Description = r.Details.Description,
                        Details_Value = r.Details.Value
                    };
            return await p.ToArrayAsync();
        }
    }


    class MyEntityIndexStore : AbstractIndexCreationTask<MyEntity>
    {
        public MyEntityIndexStore()
        {
            Map = entities => from e in entities
                              select new
                              {
                                  Id = e.Id,
                                  EntityValue = e.EntityValue,
                                  EntityValue2 = e.EntityValue2,
                                  Search = new object[] { e.Name, e.Details.Description }
                              };

            Store("EntityValue", FieldStorage.Yes);
            Index("Search", FieldIndexing.Search);
        }
    }
}
