using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Newtonsoft.Json.Linq;

namespace NitroBolt.Wui.Example.Core
{
	[ApiController]
	public class MainController : ControllerBase
	{
		[HttpPost]
		[Route("")]
		public ContentResult RoutePost([FromBody] JObject body)
		{
			return HWebApiSynchronizeHandler.Process<RequestData, ContentResult, MainState>(
				new RequestData(this.Request.Method, body),
				HttpRequestCoreAdapter.Instance, WebStateMachine.HView
			);
		}

		[HttpGet]
		[Route("")]
		public ContentResult RouteGet()
		{
			return HWebApiSynchronizeHandler.Process<RequestData, ContentResult, MainState>(
				new RequestData(this.Request.Method, null),
				HttpRequestCoreAdapter.Instance, WebStateMachine.HView
			);
		}
	}

	//public class RawJsonBodyInputFormatter : InputFormatter
	//{
	//	public RawJsonBodyInputFormatter()
	//	{
	//		this.SupportedMediaTypes.Add("application/json");
	//	}

	//	public override async Task<InputFormatterResult> ReadRequestBodyAsync(InputFormatterContext context)
	//	{
	//		var request = context.HttpContext.Request;
	//		using (var reader = new StreamReader(request.Body))
	//		{
	//			var content = await reader.ReadToEndAsync();
	//			return await InputFormatterResult.SuccessAsync(content);
	//		}
	//	}

	//	protected override bool CanReadType(Type type)
	//	{
	//		return type == typeof(string);
	//	}
	//}
}
