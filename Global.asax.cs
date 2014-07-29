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

namespace WebApi.OData.App
{
	public partial class Global : HttpApplication
	{
		private void Application_Start (object sender, EventArgs e)
		{
			GlobalConfiguration.Configure(config => {
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
			});
		}
	}
}
