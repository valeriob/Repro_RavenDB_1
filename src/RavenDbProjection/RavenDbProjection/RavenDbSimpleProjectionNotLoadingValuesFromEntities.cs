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
    public class RavenDbSimpleProjectionNotLoadingValuesFromEntities : RavenTestDriver
    {
        static RavenDbSimpleProjectionNotLoadingValuesFromEntities()
        {
            ConfigureServer(new TestServerOptions
            {
                FrameworkVersion = null,
            });
        }

        [Fact]
        public async Task ProjectionFlatteningNotworkingForSimpleSelections()
        {
            var store = base.GetDocumentStore();
            var id = "myEntity/1";
            var value = 100;

            var e = new MyEntity
            {
                Id = id,
                Name = "Test",
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

            using (var s = store.OpenAsyncSession())
            {
                var r1 = await Query1_OK(s);
                Assert.Collection(r1, a => Assert.Equal(value, a.Details_Value));

                var r2 = await Query2_OK(s);
                Assert.Collection(r2, a => Assert.Equal(value, a.Details_Value));

                var r3 = await Query3_NOK(s);
                Assert.Collection(r3, a => Assert.Equal(value, a.Details_Value));
            }
        }

        async Task<MyEntityDto[]> Query1_OK(IAsyncDocumentSession s)
        {
            var q = s.Query<MyEntity>().Select(r => new MyEntityDto
            {
                Id = r.Id,
                Name = r.Name,
                Details_Description = r.Details.Description,
                Details_Value = r.Details.Value
            });
            return await q.ToArrayAsync();
        }

        async Task<MyEntityDto[]> Query2_OK(IAsyncDocumentSession s)
        {
            var q = s.Query<MyEntity, MyEntityIndex>();
            var p = from r in q
                    let dummyUselessLoadJustToMakeItWork = RavenQuery.Load<object>("none")
                    select new MyEntityDto
                    {
                        Id = r.Id,
                        Name = r.Name,
                        Details_Description = r.Details.Description,
                        Details_Value = r.Details.Value
                    };
            return await p.ToArrayAsync();
        }

        async Task<MyEntityDto[]> Query3_NOK(IAsyncDocumentSession s)
        {
            var q = s.Query<MyEntity, MyEntityIndex>();
            var p = from r in q
                    select new MyEntityDto
                    {
                        Id = r.Id,
                        Name = r.Name,
                        Details_Description = r.Details.Description,
                        Details_Value = r.Details.Value
                    };
            return await p.ToArrayAsync();
        }
    }

    public class MyEntity
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public EntityDetails Details { get; set; }

        public MyEntity()
        {
            Details = new EntityDetails();
        }

        public class EntityDetails
        {
            public string Description { get; set; }
            public int Value { get; set; }
        }
    }

    public class MyEntityDto
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Details_Description { get; set; }
        public int Details_Value { get; set; }
    }

    public class MyEntityIndex : AbstractIndexCreationTask<MyEntity>
    {
        public MyEntityIndex()
        {
            Map = entities => from e in entities
                              select new
                              {
                                  Id = e.Id,
                                  Search = new object[] { e.Name, e.Details.Description }
                              };

            Index("Search", FieldIndexing.Search);
        }
    }
}
