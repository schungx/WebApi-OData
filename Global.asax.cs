using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.OData;
using System.Web.OData.Batch;
using System.Web.OData.Builder;
using System.Web.OData.Extensions;
using System.Web.OData.Routing;
using System.Web.OData.Routing.Conventions;

namespace WebApi.OData.App
{
	public partial class Global : HttpApplication
	{
		private void Application_Start (object sender, EventArgs e)
		{
			GlobalConfiguration.Configure(config => {
				// Enable OData query
				config.EnableQuerySupport();

				// Build the OData EDM
				var builder = new ODataConventionModelBuilder();
				builder.EntitySet<WebApi.OData.Controllers.Test>("test");

				// Normal entities set
				builder.EntitySet<Entity1>("Entities1");
			
				// Composite key
				builder.EntitySet<Entity2>("Entities2").EntityType.HasKey(e => e.Key1).HasKey(e=> e.Key2);

				// Build model
				var model = builder.GetEdmModel();

				// Add routing conventions
				var routingConventions = ODataRoutingConventions.CreateDefault();
				routingConventions.Insert(0, new OData.CastRoutingConvention());
				routingConventions.Insert(1, new OData.NavigationIndexRoutingConvention());
				routingConventions.Insert(2, new OData.CompositeKeyRoutingConvention());

				// Map the OData route and the batch handler route
				config.MapODataServiceRoute("ODataRoute", "odata", model, new DefaultODataPathHandler(), routingConventions, new DefaultODataBatchHandler(GlobalConfiguration.DefaultServer));
			});
		}
	}
}
