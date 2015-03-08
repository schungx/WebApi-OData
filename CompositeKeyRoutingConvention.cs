using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Controllers;
using System.Web.OData.Routing;
using System.Web.OData.Routing.Conventions;

namespace WebApi.OData
{
	// This class is used for handling composite keys in OData routing
	// Source: http://code.msdn.microsoft.com/Support-Composite-Key-in-d1d53161

	public class CompositeKeyRoutingConvention : IODataRoutingConvention
	{
		private readonly EntityRoutingConvention entityRoutingConvention = new EntityRoutingConvention();

		public virtual string SelectController (ODataPath odataPath, HttpRequestMessage request)
		{
			return entityRoutingConvention.SelectController(odataPath, request);
		}

		public virtual string SelectAction (ODataPath odataPath, HttpControllerContext controllerContext, ILookup<string, HttpActionDescriptor> actionMap)
		{
			try {
				var action = entityRoutingConvention.SelectAction(odataPath, controllerContext, actionMap);

				if (action == null) return null;

				var routeValues = controllerContext.RouteData.Values;

				object value;
				if (!routeValues.TryGetValue(ODataRouteConstants.Key, out value)) return action;

				var compoundKeyPairs = ((string) value).Split(',');

				if (compoundKeyPairs.Length <= 0) return null;
				if (compoundKeyPairs.Length == 1) return action;

				compoundKeyPairs
					.Select(kv => kv.Split('='))
					.Select(kv => {
						var key = kv[0].Trim();
						var valstr = kv[1].Trim();

						if (string.IsNullOrWhiteSpace(key)) throw new ApplicationException("Missing key for value: " + valstr);
						if (string.IsNullOrWhiteSpace(valstr)) throw new ApplicationException("Missing value for key " + key + ": " + valstr);

						object val = null;

						if (valstr.StartsWith("'") && valstr.EndsWith("'")) {
							val = valstr;
						} else if (valstr.StartsWith("datetime'") && valstr.EndsWith("'")) {
							val = DateTime.Parse(valstr.Substring(9, valstr.Length - 10));
						} else if (valstr.All(x => char.IsDigit(x))) {
							val = int.Parse(valstr);
						} else if (valstr.All(x => char.IsDigit(x) || x == '.') && valstr.Count(x => x == '.') == 1) {
							val = double.Parse(valstr);
						} else {
							val = valstr;
						}
						return new KeyValuePair<string, object>(key, val);
					}).ForEach(routeValues.Add);

				return action;
			} catch (Exception ex) {
				throw new HttpResponseException(new HttpResponseMessage(HttpStatusCode.BadRequest) { Content = new StringContent(ex.Message) });
			}
		}
	}
}
