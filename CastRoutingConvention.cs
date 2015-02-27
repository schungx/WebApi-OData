using Microsoft.OData.Edm;
using System.Linq;
using System.Net.Http;
using System.Web.Http.Controllers;
using System.Web.OData.Routing;
using System.Web.OData.Routing.Conventions;

namespace WebApi.OData
{
	// This class is used for handling the "~/entityset/cast" route in OData

	public class CastRoutingConvention : EntitySetRoutingConvention
	{
		public const string CastTypeParameterName = "type";

		public override string SelectAction (ODataPath odataPath, HttpControllerContext context, ILookup<string, HttpActionDescriptor> actionMap)
		{
			if (context.Request.Method != HttpMethod.Get || odataPath.PathTemplate != "~/entityset/cast") return null;

			var actionName = new[] { "GetType" }.FirstOrDefault(a => actionMap.Contains(a));
			if (actionName == null) return null;

			var castSegment = odataPath.Segments[1] as CastPathSegment;

			switch (castSegment.CastType.TypeKind) {
				case EdmTypeKind.Entity: {
						context.RouteData.Values[CastTypeParameterName] = castSegment.CastType.FullTypeName();
						break;
					}

				default: return null;
			}

			return actionName;
		}
	}
}