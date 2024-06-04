using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;

namespace NitroBolt.Wui
{
	public class HttpRequestCoreAdapter : IRequestAdapter<RequestData, ContentResult>
	{
		public readonly static HttpRequestCoreAdapter Instance = new HttpRequestCoreAdapter();

		public JObject? Content(RequestData request)
		{
			return request.Body;
		}

		public bool IsGetMethod(RequestData request)
		{
			return request.Method == HttpMethods.Get;
		}

		public ContentResult? RawResponse(HtmlResult<HElement> result)
		{
			HtmlResult? htmlResult = result as HtmlResult;
			if (htmlResult != null && htmlResult.RawResponse != null)
			{
				ContentData raw = htmlResult.RawResponse;
				return new ContentResult() { 
					Content = raw.Content, ContentType = raw.ContentType, StatusCode = raw.StatusCode 
				};
			}
			return null;
		}

		public ContentResult ToResponse(string content, string contentType, HtmlResult<HElement> result)
		{
			return new ContentResult() { Content = content, ContentType = contentType };
		}
	}
}
