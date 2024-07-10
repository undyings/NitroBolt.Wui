using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NitroBolt.Functional;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace NitroBolt.Wui
{
  public static class HWebApiSynchronizeHandler
  {
    public static readonly string NitroBolt_Wui_js = "/scripts/NitroBolt.Wui.2.0.48.js";

    public static HElement?[] Scripts(string frame = null, bool isDebug = false, TimeSpan? refreshPeriod = null, string syncJsName = null)
    {
      return HtmlJavaScriptDiffer.Scripts(new HElementProvider(), isDebug: isDebug, refreshPeriod: refreshPeriod, isInlineSyncScript: false, syncJsName: syncJsName ?? NitroBolt_Wui_js, frame: frame);
    }

    public static TResponse Process<TRequest, TResponse, TState>(TRequest request, IRequestAdapter<TRequest, TResponse> requestAdapter, 
      Func<WuiInitiator, TState, JsonData[], TRequest, HtmlResult<HElement>> page) where TState : class, new()
    {
      if (requestAdapter.IsGetMethod(request))
      {
        //TState state = new TState() { CallKind = WuiCallKind.FirstHtml };
        TState state = new();
        var result = page(new WuiInitiator(WuiCallKind.FirstHtml), state, Array<JsonData>.Empty, request);
        var rawResponse = requestAdapter.RawResponse(result);
        if (rawResponse != null)
          return rawResponse;

        var html = result.Html;
        var head = html?.Element("head") ?? new HElement("head");

        var startHead = new HElement(head.Name,
                                 head.Attributes,
                                 Scripts(frame: Guid.NewGuid().ToString(), refreshPeriod: result.RefreshPeriod ?? TimeSpan.FromSeconds(10)),
                                 head.Nodes
                               );
        var firstHtmlTransformer = result.As<HtmlResult>()?.FirstHtmlTransformer ?? FirstHtmlTransformer;
        html = firstHtmlTransformer(new HElement("html", startHead, html.Nodes.Where(node => node.As<HElement>()?.Name.LocalName != "head")));
        var toHtmlText = result.As<HtmlResult>()?.ToHtmlText ?? ToHtmlText;
        return requestAdapter.ToResponse(toHtmlText(html), "text/html; charset=utf-8", result);
      }
      else
      {
        var json = new JsonData(requestAdapter.Content(request));

        //var route = requestAdapter.Frame(request) ?? "<null>";

        //var frame = route + ":" + json.JPath("frame")?.ToString();
				var frame = json.JPath("frame")?.ToString();
				var cycle = ConvertHlp.ToInt(json.JPath("cycle")).OrDefault(0);
        var prev = PopUpdate(frame, cycle);

        var json_commands = (json.JPath("commands").As<JArray>()?.Select(j => new JsonData(j)).ToArray()).OrEmpty();

        HtmlResult<HElement> result = null;
        var watch = System.Diagnostics.Stopwatch.StartNew();
        try
        {
          TState state = prev?.Item2?.State.As<TState>() ?? new TState();

					result = page(new WuiInitiator(WuiCallKind.View), state, json_commands, request);
        }
        catch (Exception exc) //HACK ловятся все ошибки для того, чтобы не зациклилась страница. Добавить обработку ошибок на сторону js.
        {
          result = new HtmlResult<HElement> { State = prev.Item2.State, Html = prev.Item2.Page };
        }
        watch.Stop();
        var rawResponse = requestAdapter.RawResponse(result);
        if (rawResponse != null)
          return rawResponse;

        var isPartial = result.Html.Name.LocalName != "html";
        var toBody = isPartial ? html => html : (Func<HElement, HElement>)(html => html?.Element("body"));

        var js_updates = HtmlJavaScriptDiffer.JsSync(new HElementProvider(), toBody(prev?.Item2?.Page), toBody(result.Html)).ToArray();
        var jupdate = new Dictionary<string, object>() { { "cycle", prev.Item1 }, { "prev_cycle", cycle }, { "processed_commands", json_commands.Length }, { "updates", js_updates } };

        PushUpdate(frame, prev.Item1, result.Html, result.State, watch.Elapsed);

        return requestAdapter.ToResponse(JsonConvert.SerializeObject(jupdate), "application/javascript", result);
      }
    }
    static HElement FirstHtmlTransformer(HElement element)
    {
      return new HElement("html", element.Element("head"), new HElement("body", ""));
    }

    static string ToHtmlText(HElement element)
    {
      return element?.ToHtmlText();
    }


    //static JsonData Parse(string t)
    //{
    //  if (t.IsNullOrEmpty())
    //    return new JsonData(null);
    //  return new JsonData(JObject.Parse(t));
    //}


    public static Tuple<int, HUpdate> PopUpdate(string frame, int cycle)
    {
      for (; ; )
      {
        HFrame prev;
        if (Frames.TryGetValue(frame, out prev))
        {
          var next = prev.With(prev.Cycle + 1, prev.Updates.RemoveRange(prev.Updates.Keys.Where(key => key < cycle)));
          if (Frames.TryUpdate(frame, next, prev))
            return Tuple.Create(next.Cycle, prev.Updates.Find(cycle));
        }
        else
        {
          if (Frames.TryAdd(frame, new HFrame(cycle + 1)))
            return Tuple.Create(cycle + 1, (HUpdate)null);
        }
      }
    }
    public static void PushUpdate(string frame, int cycle, HElement page, object state, TimeSpan elapsed)
    {
      for (; ; )
      {
        HFrame hframe;
        if (Frames.TryGetValue(frame, out hframe))
        {
          var nextFrame = hframe.With(updates: hframe.Updates.Add(cycle, new HUpdate(cycle, page, state, elapsed)));
          if (Frames.TryUpdate(frame, nextFrame, hframe))
            return;
        }
        else
        {
          if (Frames.TryAdd(frame, new HFrame(cycle, ImmutableDictionary<int, HUpdate>.Empty.Add(cycle, new HUpdate(cycle, page, state, elapsed)))))
            return;
        }
      }
    }
    public static readonly ConcurrentDictionary<string, HFrame> Frames = new ConcurrentDictionary<string, HFrame>();

    public class HFrame
    {
      public HFrame(int cycle = 1, ImmutableDictionary<int, HUpdate> updates = null)
      {
        this.Cycle = cycle;
        this.Updates = updates ?? ImmutableDictionary<int, HUpdate>.Empty;
      }
      public readonly int Cycle;
      public readonly ImmutableDictionary<int, HUpdate> Updates;

      public HFrame With(int? cycle = null, ImmutableDictionary<int, HUpdate> updates = null)
      {
        return new HFrame(cycle ?? this.Cycle, updates ?? Updates);
      }
    }
    public class HUpdate
    {
      public HUpdate(int cycle, HElement page, object state, TimeSpan elapsed)
      {
        this.Cycle = cycle;
        this.Page = page;
        this.State = state;
        this.Elapsed = elapsed;
      }
      public readonly int Cycle;
      public readonly HElement Page;
      public readonly object State;
      public readonly TimeSpan Elapsed;
    }

  }

  public class HtmlResult : HtmlResult<HElement>
  {
    //public Action<HttpResponseMessage> ResponseProcessor = null;
    public Func<HElement, HElement>? FirstHtmlTransformer = null;
    public Func<HElement, string>? ToHtmlText = null;
    public ContentData? RawResponse = null;
  }

  public class HtmlResult<TElement>
  {
    public object? State;
    public TElement? Html;
    /// <summary>
    /// Имеет силу только при первой загрузке страницы
    /// </summary>
    public TimeSpan? RefreshPeriod;
  }
}
