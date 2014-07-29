namespace WebApi.OData.Controllers
{
	// Testing

	public class Test
	{
		[Key]
		public string Message { get; set; }
	}

	public class TestController<DB> : StandardODataControllerBase<DB>
	{
		[EnableQuery]
		public IEnumerable<Test> Get ()
		{
			yield return new Test() { Message = "The OData server is on-line. The local time is " + DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss tt") + "." };
		}
	}
}