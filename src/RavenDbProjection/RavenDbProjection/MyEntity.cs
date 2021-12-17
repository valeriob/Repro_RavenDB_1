using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RavenDbProjection
{
    public class MyEntity
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public int EntityValue { get; set; }
        public int EntityValue2 { get; set; }
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
        public int EntityValue { get; set; }
        public int EntityValue2 { get; set; }
        public string Details_Description { get; set; }
        public int Details_Value { get; set; }
    }
}
