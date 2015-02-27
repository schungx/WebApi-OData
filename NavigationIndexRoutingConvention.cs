using Microsoft.OData.Edm;
using System.Linq;
using System.Net.Http;
using System.Web.Http.Controllers;
using System.Web.OData.Routing;
using System.Web.OData.Routing.Conventions;

namespace WebApi.OData
{
	// This class is used for handling the "~/entityset/key/navigation/key" route in OData

	public class NavigationIndexRoutingConvention : EntitySetRoutingConvention
	{
		public override string SelectAction (ODataPath odataPath, HttpControllerContext context, ILookup<string, HttpActionDescriptor> actionMap)
		{
			if (context.Request.Method == HttpMethod.Get && odataPath.PathTemplate == "~/entityset/key/navigation/key") {
				var navigationSegment = odataPath.Segments[2] as NavigationPathSegment;
				var navigationProperty = navigationSegment.NavigationProperty.Partner;
				var declaringType = navigationProperty.DeclaringType as IEdmEntityType;

				var actionName = new[] { "Get", "Get" + declaringType.Name }.FirstOrDefault(a => actionMap.Contains(a));
				if (actionName == null) return null;

				if (actionMap.Contains(actionName)) {
					// Add keys to route data, so they will bind to action parameters.
					var keyValueSegment = odataPath.Segments[1] as KeyValuePathSegment;
					context.RouteData.Values[ODataRouteConstants.Key] = keyValueSegment.Value;

					var relatedKeySegment = odataPath.Segments[3] as KeyValuePathSegment;
					context.RouteData.Values[ODataRouteConstants.RelatedKey] = relatedKeySegment.Value;

					return actionName;
				}
			}

			return null;
		}
	}
}