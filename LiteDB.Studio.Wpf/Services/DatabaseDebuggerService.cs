using LiteDB;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace LiteDB.Studio.Wpf.Services;

/// <summary>
/// Local HTTP page-dump server providing LiteDB internals inspection.
/// Ports LiteDB.Studio.DatabaseDebugger / HtmlPageDump / HtmlPageList to WPF services layer.
/// </summary>
internal sealed class DatabaseDebuggerService : IDisposable
{
    private LiteDatabase? _db;
    private HttpListener? _listener;

    public int Port { get; private set; }
    public bool IsRunning { get; private set; }

    public Task StartAsync(LiteDatabase db, int port)
    {
        _db = db;
        Port = port;
        IsRunning = true;
        _ = Task.Run(RunListener);
        return Task.CompletedTask;
    }

    public void Stop()
    {
        try
        {
            _listener?.Stop();
        }
        catch
        {
            // ignore errors on stop
        }

        IsRunning = false;
    }

    public void Dispose() => Stop();

    private void RunListener()
    {
        _listener = new HttpListener();
        _listener.Prefixes.Add($"http://localhost:{Port}/");
        _listener.Start();

        while (true)
        {
            try
            {
                var context = _listener.GetContext();
                _ = Task.Run(() => HandleRequest(context));
            }
            catch
            {
                break;
            }
        }
    }

    private void HandleRequest(HttpListenerContext context)
    {
        var body = string.Empty;
        var status = 200;
        var url = context.Request.RawUrl ?? "/";

        try
        {
            if (Regex.IsMatch(url, @"^\/(\d+)?$"))
            {
                var pageID = url == "/" ? 0 : int.Parse(url.TrimStart('/'));
                var page = context.Request.HttpMethod == "GET"
                    ? _db!.GetCollection($"$dump({pageID})").Query().FirstOrDefault()
                    : GetPost(context.Request.InputStream);

                body = page == null
                    ? $"Page {pageID} not found in database"
                    : new HtmlPageDump(page).Render();
            }
            else if (Regex.IsMatch(url, @"^\/list\/(\d+)$"))
            {
                var pageID = int.Parse(url.Substring(6));
                body = new HtmlPageList(_db!.GetCollection($"$page_list({pageID})").Query().Limit(1000).ToEnumerable()).Render();
            }
            else
            {
                body = "Url not found";
                status = 404;
            }
        }
        catch (Exception ex)
        {
            body = $"<pre>Error: {WebUtility.HtmlEncode(ex.Message)}</pre>";
            status = 500;
        }

        var message = Encoding.UTF8.GetBytes(body);
        context.Response.StatusCode = status;
        context.Response.ContentType = "text/html; charset=utf-8";
        context.Response.ContentLength64 = message.Length;
        context.Response.OutputStream.Write(message, 0, message.Length);
        context.Response.OutputStream.Close();
    }

    private static BsonDocument GetPost(Stream input)
    {
        var text = WebUtility.UrlDecode(new StreamReader(input).ReadToEnd().Substring(2));
        var bytes = text.Trim().Split(' ');
        var buffer = new byte[bytes.Length];

        for (var i = 0; i < bytes.Length; i++)
        {
            buffer[i] = Convert.ToByte(bytes[i].Trim(), 16);
        }

        return new BsonDocument { ["buffer"] = buffer };
    }

    // -------------------------------------------------------------------------
    // Nested renderer: HTML page dump (ported from LiteDB.Studio.HtmlPageDump)
    // -------------------------------------------------------------------------
    private sealed class HtmlPageDump
    {
        private const int BlockWidth = 30;
        private const int BlocksPerLine = 32;
        private const int PageHeaderSize = 32;

        private enum PageType { Empty = 0, Header = 1, Collection = 2, Index = 3, Data = 4 }

        private readonly BsonDocument _page;
        private readonly uint _pageID;
        private readonly PageType _pageType;
        private readonly byte[] _buffer;
        private readonly StringBuilder _writer = new();
        private readonly List<PageItem> _items = [];
        private readonly string[] _colors = ["#B2EBF2", "#FFECB3"];

        internal HtmlPageDump(BsonDocument page)
        {
            _page = page;
            _buffer = page["buffer"].AsBinary;
            _pageID = BitConverter.ToUInt32(_buffer, 0);
            _pageType = (PageType)_buffer[4];
            page.Remove("buffer");

            LoadItems();
            SpanCaptionItems();
        }

        private void LoadItems()
        {
            for (var i = 0; i < _buffer.Length; i++)
            {
                _items.Add(new PageItem { Index = i, Value = _buffer[i], Color = -1 });
            }
        }

        private void SpanCaptionItems()
        {
            SpanPageHeader();
            SpanSegments();

            if (_pageType == PageType.Header) { SpanHeaderPage(); }
            else if (_pageType == PageType.Collection) { SpanCollectionPage(); }
        }

        private void SpanPageHeader()
        {
            var h = 0;
            h += SpanPageID(h, "PageID", false, false);
            h += SpanItem<byte>(h, 0, null, "PageType", null);
            h += SpanPageID(h, "PrevPageID", false, false);
            h += SpanPageID(h, "NextPageID", false, true);
            h += SpanItem<byte>(h, 0, null, "Slot", null);
            h += SpanItem(h, 3, null, "TransactionID", BitConverter.ToUInt32);
            h += SpanItem<byte>(h, 0, null, "IsConf", null);
            h += SpanItem(h, 3, null, "ColID", BitConverter.ToUInt32);
            h += SpanItem<byte>(h, 0, null, "Items", null);
            h += SpanItem(h, 1, null, "UsedBytes", BitConverter.ToUInt16);
            h += SpanItem(h, 1, null, "FragmentedBytes", BitConverter.ToUInt16);
            h += SpanItem(h, 1, null, "NextFreePo", BitConverter.ToUInt16);
            h += SpanItem<byte>(h, 0, null, "HghIdx", null);
            h += SpanItem<byte>(h, 0, null, "Reserv", null);
        }

        private void SpanSegments()
        {
            var highestIndex = _buffer[30];

            if (highestIndex >= byte.MaxValue) { return; }

            var colorIndex = 0;

            for (var i = 0; i <= highestIndex; i++)
            {
                var posAddr = _buffer.Length - ((i + 1) * 4) + 2;
                var lenAddr = _buffer.Length - (i + 1) * 4;

                var position = BitConverter.ToUInt16(_buffer, posAddr);
                var length = BitConverter.ToUInt16(_buffer, lenAddr);

                _items[lenAddr].Span = 1;
                _items[lenAddr].Caption = i.ToString();
                _items[lenAddr].Text = length.ToString();

                _items[posAddr].Span = 1;
                _items[posAddr].Caption = i.ToString();
                _items[posAddr].Text = position.ToString();
                _items[posAddr].Href = "#" + i;

                if (position != 0)
                {
                    _items[posAddr].Color = _items[lenAddr].Color = colorIndex++ % _colors.Length;
                    _items[position].Id = i.ToString();

                    if (_pageType == PageType.Data)
                    {
                        SpanItem<byte>(position, 0, null, "Extend", null);
                        SpanPageID(position + 1, "NextBlockID", true, false);
                    }
                    else
                    {
                        SpanItem<byte>(position, 0, null, "Slot", null);
                        SpanItem<byte>(position + 1, 0, null, "Level", null);
                        SpanPageID(position + 2, "DataBlock", true, false);
                        SpanPageID(position + 7, "NextNode", true, false);

                        for (var j = 0; j < _buffer[position + 1]; j++)
                        {
                            SpanPageID(position + 12 + (j * 5 * 2), "Prev #" + j, true, false);
                            SpanPageID(position + 12 + (j * 5 * 2) + 5, "Next #" + j, true, false);
                        }

                        var p = position + 12 + (_buffer[position + 1] * 5 * 2);
                        SpanItem<byte>(p, 0, null, "Type", null);

                        if (_buffer[p] == 6 || _buffer[p] == 9)
                        {
                            SpanItem<byte>(++p, 0, null, "Len", null);
                        }
                    }

                    for (var j = position; j < position + length; j++)
                    {
                        _items[j].Color = colorIndex - 1;
                    }
                }
            }

            // fix zebra segment colors
            var current = 0;
            var color = 0;

            for (var i = 0; i < _buffer.Length; i++)
            {
                if (_items[i].Color == -1) { continue; }

                if (_items[i].Color != current)
                {
                    color++;
                }

                current = _items[i].Color;
                _items[i].Color = color % _colors.Length;
            }
        }

        private void SpanHeaderPage()
        {
            var h = PageHeaderSize;
            var color = 0;

            h += SpanItem(h, 26, null, "HeaderInfo", (byte[] b, int i) => Encoding.UTF8.GetString(b, i, 27));
            h += SpanItem<byte>(h, 0, null, "FileVersion", null);
            h += SpanPageID(h, "FreeEmptyPageList", false, true);
            h += SpanPageID(h, "LastPageID", false, false);
            h += SpanItem(h, 7, null, "CreationTime", (byte[] b, int i) => new DateTime(BitConverter.ToInt64(b, i)).ToString("o"));
            h += SpanItem(h, 3, null, "UserVersion", BitConverter.ToInt32);
            h += SpanItem(h, 3, null, "LCID", BitConverter.ToInt32);
            h += SpanItem(h, 3, null, "SortOptions", BitConverter.ToInt32);

            var collectionPosition = 192;
            SpanItem(collectionPosition, 3, null, "Length", BitConverter.ToInt32);

            var p = collectionPosition + 4;

            while (p < collectionPosition + 4 + BitConverter.ToInt32(_buffer, collectionPosition) - 5)
            {
                var initial = p;
                p++;
                p += SpanCString(p, "Name");
                p += SpanPageID(p, "PageID", false, false);

                for (var k = initial; k < p; k++)
                {
                    _items[k].Color = color % _colors.Length;
                }

                color++;
            }
        }

        private void SpanCollectionPage()
        {
            var color = 0;

            for (var i = 0; i < 5; i++)
            {
                SpanPageID(PageHeaderSize + (i * 4), "DataPageList #" + i, false, true);
            }

            var h = 96;
            var indexes = _buffer[h];
            h += SpanItem<byte>(h, 0, null, "Indexes", null);

            for (var i = 0; i < indexes; i++)
            {
                var initial = h;
                h += SpanItem<byte>(h, 0, null, "Slot", null);
                h += SpanItem<byte>(h, 0, null, "Type", null);
                h += SpanCString(h, "Name");
                h += SpanCString(h, "Expr");
                h += SpanItem<byte>(h, 0, null, "Unique", null);
                h += SpanPageID(h, "Head", true, false);
                h += SpanPageID(h, "Tail", true, false);
                h += SpanItem<byte>(h, 0, null, "MaxLevel", null);
                h += SpanPageID(h, "IndexPageList", false, true);

                for (var k = initial; k < h; k++)
                {
                    _items[k].Color = color % _colors.Length;
                }

                color++;
            }
        }

        private int SpanItem<T>(int index, int span, string? href, string caption, Func<byte[], int, T>? convert)
        {
            _items[index].Span = span;
            _items[index].Caption = caption;
            _items[index].Text = convert == null ? _items[index].Value.ToString() : convert(_buffer, index)?.ToString() ?? string.Empty;
            _items[index].Href = href?.Replace("{text}", _items[index].Text);
            return span + 1;
        }

        private int SpanPageID(int index, string caption, bool pageAddress, bool pageList)
        {
            var pageID = BitConverter.ToUInt32(_buffer, index);
            _items[index].Span = 3;
            _items[index].Caption = caption;
            _items[index].Text = pageID == uint.MaxValue ? "-" : pageID.ToString();
            _items[index].Href = pageID == uint.MaxValue || index == 0
                ? null
                : "/" + (pageList ? "list/" : "") + pageID + (pageAddress ? "#" + _buffer[index + 4] : "");

            if (pageAddress)
            {
                _items[index + 4].Caption = "Index";
                _items[index + 4].Text = _items[index + 4].Value == byte.MaxValue ? "-" : _items[index + 4].Value.ToString();
            }

            if (pageList) { _items[index].Target = "list"; }

            return 4 + (pageAddress ? 1 : 0);
        }

        private int SpanCString(int index, string caption)
        {
            var p = index;

            while (_buffer[p] != 0) { p++; }

            var length = p - index;

            if (length > 0)
            {
                _items[index].Text = Encoding.UTF8.GetString(_buffer, index, length);
                _items[index].Span = length - 1;
                _items[index].Caption = caption;
            }

            return length + 1;
        }

        internal string Render()
        {
            if (_page == null) { return "Page not found"; }

            RenderHeader();
            RenderInfo();
            RenderConvert();
            RenderPage();
            RenderFooter();

            return _writer.ToString();
        }

        private void RenderHeader()
        {
            _writer.AppendLine("<html><head>");
            _writer.AppendLine($"<title>LiteDB Debugger: #{_pageID:0000} - {_pageType}</title>");
            _writer.AppendLine("<style>");
            _writer.AppendLine("* { box-sizing: border-box; }");
            _writer.AppendLine("body { font-family: monospace; }");
            _writer.AppendLine("h1 { border-bottom: 2px solid #545454; color: #545454; margin: 0; }");
            _writer.AppendLine("textarea { margin: 0px; width: 1000px; height: 61px; vertical-align: top; }");
            _writer.AppendLine(".page { display: flex; min-width: 1245px; }");
            _writer.AppendLine($".rules > div {{ padding: 9px 0 0; height: 31px; width: {BlockWidth}px; color: gray; background-color: #f1f1f1; margin: 1px; text-align: center; position: relative; }}");
            _writer.AppendLine(".line { min-width: 1024px; }");
            _writer.AppendLine($".line > a {{ background-color: #d1d1d1; margin: 1px; min-width: {BlockWidth}px; height: 30px; display: inline-block; text-align: center; padding: 10px 0 0; position: relative; }}");
            _writer.AppendLine(".line:first-child > a { background-color: #a1a1a1; }");
            _writer.AppendLine(".line > a[href] { color: blue; }");
            _writer.AppendLine(".line > a:before { background-color: white; font-size: 7px; top: -1; left: 0; color: black; position: absolute; content: attr(st); }");
            _writer.AppendLine("iframe { border: none; flex: 1; min-width: 50px; }");

            foreach (var color in _items.Select(x => x.Color).Where(x => x != -1).Distinct())
            {
                _writer.AppendLine($".c{color} {{ background-color: {_colors[color % _colors.Length]} !important; }}");
            }

            _writer.AppendLine("</style></head>");
            _writer.AppendLine("<body>");
            _writer.AppendLine($"<h1>#{_pageID:0000} :: {_pageType} Page</h1>");
        }

        private void RenderInfo()
        {
            if (!_page.ContainsKey("pageID")) { return; }

            _writer.AppendLine("<div style='text-align: center; margin: 5px 0; width: 1245px'>");
            _writer.AppendLine($"Origin: [{_page["_origin"].AsString}] - Position: {_page["_position"].AsInt64} - Free bytes: {_page["freeBytes"]}");
            _writer.AppendLine("</div>");
        }

        private void RenderConvert()
        {
            _writer.AppendLine($"<form method='post' action='/{_pageID}'>");
            _writer.AppendLine("<textarea placeholder='Paste hex page body content here' name='b'></textarea>");
            _writer.AppendLine("<button type='submit'>View</button>");
            _writer.AppendLine("</form>");
        }

        private void RenderPage()
        {
            _writer.AppendLine("<div class='page'>");
            RenderRules();
            RenderBlocks();
            _writer.AppendLine("<iframe name='list' id='list'></iframe>");
            _writer.AppendLine("</div>");
        }

        private void RenderRules()
        {
            _writer.AppendLine("<div class='rules'>");

            for (var i = 0; i < _items.Count; i += BlocksPerLine)
            {
                _writer.AppendLine($"<div>{i}</div>");
            }

            _writer.AppendLine("</div>");
        }

        private void RenderBlocks()
        {
            var blocksLeft = BlocksPerLine;
            _writer.AppendLine("<div class='blocks'><div class='line'>");

            for (var i = 0; i < _items.Count; i++)
            {
                var item = _items[i];
                blocksLeft = RenderItem(item, blocksLeft);
                i += item.Span;
            }

            _writer.AppendLine("</div></div>");
        }

        private int RenderItem(PageItem item, int blocksLeftInLine)
        {
            var blocksToPrint = 1 + item.Span;
            var textToPrint = string.Empty;
            var textLeft = item.Text ?? item.Value.ToString();

            while (blocksToPrint > 0)
            {
                var overflow = false;
                blocksLeftInLine--;
                blocksToPrint--;

                _writer.Append($"<a title='{item.Index}'");

                if (!string.IsNullOrEmpty(item.Href)) { _writer.Append($" href='{item.Href}'"); }
                if (!string.IsNullOrEmpty(item.Target)) { _writer.Append($" target='{item.Target}'"); }
                if (item.Color >= 0) { _writer.Append($" class='c{item.Color}'"); }

                if (blocksToPrint > 0)
                {
                    var span = blocksToPrint;

                    if (blocksToPrint > blocksLeftInLine)
                    {
                        overflow = true;
                        span = blocksLeftInLine;
                        textToPrint = SliceText(ref textLeft, blocksLeftInLine, blocksToPrint);
                    }

                    _writer.Append($" style='min-width: {BlockWidth * (span + 1) + (span * 2)}px'");
                    blocksLeftInLine -= span;
                    blocksToPrint -= span;
                }

                if (!string.IsNullOrEmpty(item.Caption)) { _writer.Append($" st='{item.Caption}'"); }
                if (!string.IsNullOrEmpty(item.Id)) { _writer.Append($" id='{item.Id}'"); }

                _writer.Append(">");

                if (overflow)
                {
                    _writer.Append(textToPrint);
                    _writer.Append(" &#8594;");
                }
                else if (textLeft != string.Empty)
                {
                    _writer.Append(textLeft);
                }
                else
                {
                    _writer.Append("&#8594;");
                }

                _writer.Append("</a>");

                if (blocksLeftInLine == 0)
                {
                    _writer.AppendLine("</div><div class='line'>");
                    blocksLeftInLine = BlocksPerLine;
                }
            }

            return blocksLeftInLine;
        }

        private static string SliceText(ref string text, int blocksLeftInLine, int blocksToPrint)
        {
            var textToPrint = string.Empty;

            if (blocksLeftInLine > 1 && (blocksLeftInLine > blocksToPrint - blocksLeftInLine || blocksLeftInLine > 30))
            {
                var len = 3 * blocksLeftInLine;
                textToPrint = len >= text.Length ? text : text.Substring(0, len);
            }

            text = text.Substring(textToPrint.Length);
            return textToPrint;
        }

        private void RenderFooter()
        {
            _writer.AppendLine("</body></html>");
        }

        private sealed class PageItem
        {
            public int Index { get; set; }
            public string? Id { get; set; }
            public string? Text { get; set; }
            public byte Value { get; set; }
            public int Span { get; set; }
            public string? Caption { get; set; }
            public int Color { get; set; }
            public string? Href { get; set; }
            public string? Target { get; set; }
        }
    }

    // -------------------------------------------------------------------------
    // Nested renderer: HTML page list (ported from LiteDB.Studio.HtmlPageList)
    // -------------------------------------------------------------------------
    private sealed class HtmlPageList
    {
        private readonly IEnumerable<BsonDocument> _pages;
        private readonly StringBuilder _writer = new();

        internal HtmlPageList(IEnumerable<BsonDocument> pages)
        {
            _pages = pages;
        }

        internal string Render()
        {
            RenderHeader();
            RenderInfo();
            RenderFooter();
            return _writer.ToString();
        }

        private void RenderHeader()
        {
            _writer.AppendLine("<html><head>");
            _writer.AppendLine("<title>LiteDB Explorer Debugger</title>");
            _writer.AppendLine("<style>");
            _writer.AppendLine("* { box-sizing: border-box; }");
            _writer.AppendLine("body { font-family: monospace; margin: 0; }");
            _writer.AppendLine("table { border-collapse: collapse; width: 100%; }");
            _writer.AppendLine("th { font-weight: bold; background-color: #a1a1a1; }");
            _writer.AppendLine("td, th { height: 32px; }");
            _writer.AppendLine("a[href] { color: blue; }");
            _writer.AppendLine("</style></head>");
            _writer.AppendLine("<body>");
        }

        private void RenderInfo()
        {
            _writer.AppendLine("<table border='1'><tr>");
            _writer.AppendLine("<th>PageID</th><th>PageType</th><th>Slot</th><th>FreeSpace</th><th>Items</th>");
            _writer.AppendLine("</tr>");

            var count = 0;

            foreach (var page in _pages)
            {
                _writer.AppendLine("<tr>");
                _writer.AppendLine($"<td style='text-align: center'><a target='_top' href='/{page["pageID"].AsInt32}'>{page["pageID"].AsInt32}</a></td>");
                _writer.AppendLine($"<td style='text-align: center'>{page["pageType"].AsString}</td>");
                _writer.AppendLine($"<td style='text-align: center'>{page["slot"].AsInt32}</td>");
                _writer.AppendLine($"<td style='text-align: right'>{page["freeBytes"].AsInt32}</td>");
                _writer.AppendLine($"<td style='text-align: center'>{page["itemsCount"].AsInt32}</td>");
                _writer.AppendLine("</tr>");
                count++;
            }

            _writer.AppendLine("</table>");
            _writer.AppendLine($"<div>Total: {count} pages</div>");
        }

        private void RenderFooter()
        {
            _writer.AppendLine("</body></html>");
        }
    }
}
