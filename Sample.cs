using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.OData;
using System.Web.Http.OData.Batch;
using System.Web.Http.OData.Builder;
using System.Web.Http.OData.Routing;
using System.Web.Http.OData.Routing.Conventions;

namespace WebApi.OData.App
{
	public partial class Global
	{
		private void SetupHomeNetODataControllers (HttpConfiguration config)
		{
			Contract.Requires(config != null);

			// Build the OData EDM

			var builder = new ODataConventionModelBuilder();
			builder.EntitySet<WebApi.OData.Controllers.Test>("test");

      // Normal entities set
			builder.EntitySet<Entity1>("Entities1");
			
			// Composite key
			builder.EntitySet<Entity2>("Entities2").EntityType.HasKey(e => e.Key1).HasKey(e=> e.Key2);

      // Build model
			var model = builder.GetEdmModel();

			// Add the composite key routing convention
			var routingConventions = ODataRoutingConventions.CreateDefault();
			routingConventions.Insert(0, new OData.CompositeKeyRoutingConvention());

			// Map the OData route and the batch handler route
			config.Routes.MapODataRoute("ODataRoute", "odata", model, new DefaultODataPathHandler(), routingConventions, new DefaultODataBatchHandler(GlobalConfiguration.DefaultServer));
		}
	}
}

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
