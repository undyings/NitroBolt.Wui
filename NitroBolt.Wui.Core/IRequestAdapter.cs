using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NitroBolt.Wui
{
	public interface IRequestAdapter<TRequest, TResponse>
	{
		bool IsGetMethod(TRequest request);
		JObject? Content(TRequest request);
		//string Frame(TRequest request);

		TResponse? RawResponse(HtmlResult<HElement> result);

		TResponse ToResponse(string content, string contentType, HtmlResult<HElement> result);
	}

	public interface IWuiState
	{
		WuiCallKind CallKind { get; set; }
	}

	public enum WuiCallKind
	{
		FirstHtml,
		View,
		Json
	}

	public class ContentData
	{	
		public string? Content;
		public string? ContentType;
		public int? StatusCode;
	}

	public class RequestData
	{
		public readonly string Method;
		public readonly JObject? Body;

		public RequestData(string method, JObject? body)
		{
			this.Method = method;
			this.Body = body;
		}
	}
}
