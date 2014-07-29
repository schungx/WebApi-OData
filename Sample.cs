using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Net.Http;
using System.ServiceModel.Activation;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using System.Web.Http.OData;
using System.Web.Http.OData.Batch;
using System.Web.Http.OData.Builder;
using System.Web.Http.OData.Routing;
using System.Web.Http.OData.Routing.Conventions;
using System.Web.Routing;

namespace WebApi.OData.Controllers
{
	// Assuming Entity1 has a string key
	public class Entities1Controller : StandardODataController<DB, Entity1, string> { }

	// Composite keys require manual handling, assuming Entity2 has Key1: string and Key2: int
	public class Entities2Controller : StandardODataControllerBase<DB>
	{
		[EnableQuery]
		public IEnumerable<Entity2> Get () { return db.Entities2; }
		public Task<Entity2> Get ([FromODataUri] string Key1, [FromODataUri] int Key2) { return db.Entities2.FindAsync(Key1, Key2); }
		public async Task<HttpResponseMessage> Post ([FromBody] Entity2 entity) { return await PostEntity(db.Entities2, entity); }
		public async Task<Entity2> Put ([FromODataUri] string Key1, [FromODataUri] int Key2, [FromBody] Entity2 entity) { return await UpdateEntity(db.Entities2, await Get(Key1, Key2), entity); }
		[AcceptVerbs("PATCH", "MERGE")]
		public async Task<Entity2> Patch ([FromODataUri] string Key1, [FromODataUri] int Key2, [FromBody] Delta<Entity2> patch) { return await PatchEntity(db.Entities2, await Get(Key1, Key2), patch); }
		public async Task Delete ([FromODataUri] string Key1, [FromODataUri] int Key2) { await DeleteEntity(db.Entities2, await Get(Key1, Key2)); }
	}
}
