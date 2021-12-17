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
    public class LoadFromIndexVsDocument : RavenTestDriver
    {
        static LoadFromIndexVsDocument()
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
                Details = new MyEntity.EntityDetails
                {
                    Description = "Test Description",
                    Value = value,
                }
            };

            using (var s = store.OpenAsyncSession())
            {
                await new MyEntityIndex().ExecuteAsync(store);
                await s.StoreAsync(e);
                await s.SaveChangesAsync();
            }

            WaitForIndexing(store);

            using (var s = store.OpenAsyncSession())
            {
                var r1 = await Query1_OK(s);
                Assert.Collection(r1, a => Assert.Equal(entityValue, a.EntityValue));

                var r2 = await FromIndex_do_not_find_data(s);
                Assert.Collection(r2, a => Assert.Equal(0, a.EntityValue));
                Assert.Collection(r2, a => Assert.Equal(0, a.Details_Value));

                var r3 = await FromDocument_should_find_data(s);
                Assert.Collection(r3, a => Assert.Equal(entityValue, a.EntityValue));
                Assert.Collection(r3, a => Assert.Equal(value, a.Details_Value));
            }
        }

        async Task<MyEntityDto[]> Query1_OK(IAsyncDocumentSession s)
        {
            var q = s.Query<MyEntity>().Select(r => new MyEntityDto
            {
                Id = r.Id,
                Name = r.Name,
                EntityValue = r.EntityValue,
                Details_Description = r.Details.Description,
                Details_Value = r.Details.Value
            });
            return await q.ToArrayAsync();
        }

        async Task<MyEntityDto[]> FromIndex_do_not_find_data(IAsyncDocumentSession s)
        {
            var q = s.Query<MyEntity, MyEntityIndex>();
            var p = from r in q.Customize(r=> r.Projection(ProjectionBehavior.FromIndex))
                    select new MyEntityDto
                    {
                        Id = r.Id,
                        Name = r.Name,
                        EntityValue = r.EntityValue,
                        Details_Description = r.Details.Description,
                        Details_Value = r.Details.Value
                    };
            return await p.ToArrayAsync();
        }

        async Task<MyEntityDto[]> FromDocument_should_find_data(IAsyncDocumentSession s)
        {
            var q = s.Query<MyEntity, MyEntityIndex>();
            var p = from r in q.Customize(r => r.Projection(ProjectionBehavior.FromDocument))
                    select new MyEntityDto
                    {
                        Id = r.Id,
                        Name = r.Name,
                        EntityValue = r.EntityValue,
                        Details_Description = r.Details.Description,
                        Details_Value = r.Details.Value
                    };
            return await p.ToArrayAsync();
        }
    }



}
