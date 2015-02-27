namespace WebApi.OData.Controllers
{
	// Testing

	public class TestController : StandardODataControllerBase<HomeNetDB>
	{
		[EnableQuery]
		public string Get ()
		{
			return "The OData server is on-line. The time is " + DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss tt") + ".";
		}
	}
}