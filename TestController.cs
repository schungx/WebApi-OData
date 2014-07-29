namespace WebApi.OData.Controllers
{
	// Testing

	public class Test
	{
		[Key]
		public string Message { get; set; }
	}

	public class TestController : StandardODataControllerBase<HomeNetDB>
	{
		[EnableQuery]
		public IEnumerable<Test> Get ()
		{
			yield return new Test() { Message = "The OData server is on-line. The time is " + DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss tt") + "." };
		}
	}
}
