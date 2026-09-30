using System.Net;
using System.Text;
using Google.Protobuf.Reflection;

namespace BreakfastProvider.Api.Contracts;

/// <summary>
/// Renders the gRPC contract's documentation page from its descriptor set: services and their methods, messages and
/// their fields, enums and their values, each with the comment it carries in the proto. The page is static HTML with no
/// script and no external request, and is deterministic, so the same bytes are served here, committed as
/// docs/grpc.html and published on GitHub Pages.
/// </summary>
public static class GrpcContractPage
{
    // Field numbers in descriptor.proto that address an element in SourceCodeInfo.Location.path.
    private const int FileMessageType = 4, FileEnumType = 5, FileService = 6;
    private const int MessageField = 2, MessageNestedType = 3, MessageEnumType = 4;
    private const int ServiceMethod = 2;
    private const int EnumValue = 2;

    public static string Render(FileDescriptorSet set)
    {
        var files = string.Join(", ", set.File.Select(f => f.Name));
        var html = new StringBuilder();
        html.Append($$"""
            <!DOCTYPE html>
            <html lang="en">
            <head>
            <meta charset="utf-8">
            <meta name="viewport" content="width=device-width, initial-scale=1">
            <title>Breakfast Provider — gRPC API</title>
            <style>
            {{Styles}}
            </style>
            </head>
            <body>
            <nav class="contract-bar">
            <span class="title">gRPC</span>
            <a class="doc" href="v1.json">v1.json</a>
            <a class="doc" href="protos/breakfast.proto">breakfast.proto</a>
            </nav>
            <main>
            <h1>Breakfast Provider — gRPC API</h1>
            <p class="lead">gRPC over HTTP/2. Server reflection is enabled (<code>grpc.reflection.v1</code>), so grpcurl, Postman and Kreya can discover and call the service. The contract is published as <a href="v1.json">a descriptor set in JSON</a> and as <a href="protos/breakfast.proto">its proto file</a>.</p>

            """);

        foreach (var file in set.File)
        {
            var page = new FilePage(file);
            page.RenderServices(html);
            page.RenderMessages(html);
            page.RenderEnums(html);
        }

        html.Append($"""
            </main>
            <footer>Generated from {Encode(files)} by Breakfast Provider.</footer>
            </body>
            </html>

            """);
        return html.ToString();
    }

    private sealed class FilePage(FileDescriptorProto file)
    {
        private readonly string _prefix = file.Package.Length == 0 ? "." : $".{file.Package}.";
        private readonly Dictionary<string, DescriptorProto> _messages = CollectMessages(file);
        private readonly HashSet<string> _enums = CollectEnums(file);

        public void RenderServices(StringBuilder html)
        {
            for (var s = 0; s < file.Service.Count; s++)
            {
                var service = file.Service[s];
                var fullName = FullName(service.Name);
                html.Append($"<section class=\"service\">\n<h2 id=\"{Encode(fullName)}\">{Encode(fullName)}</h2>\n");
                html.Append(Paragraphs(Comment(FileService, s)));
                html.Append("<div class=\"scroll\"><table>\n<thead><tr><th>Method</th><th>Request</th><th>Response</th><th>Kind</th><th>Description</th></tr></thead>\n<tbody>\n");
                for (var m = 0; m < service.Method.Count; m++)
                {
                    var method = service.Method[m];
                    html.Append("<tr>")
                        .Append($"<td><code>{Encode(method.Name)}</code></td>")
                        .Append($"<td>{TypeReference(method.InputType)}</td>")
                        .Append($"<td>{TypeReference(method.OutputType)}</td>")
                        .Append($"<td class=\"kind\">{Kind(method)}</td>")
                        .Append($"<td>{Inline(Comment(FileService, s, ServiceMethod, m))}</td>")
                        .Append("</tr>\n");
                }
                html.Append("</tbody>\n</table></div>\n</section>\n\n");
            }
        }

        public void RenderMessages(StringBuilder html)
        {
            var any = false;
            foreach (var (message, name, path) in Messages())
            {
                if (message.Options?.MapEntry == true)
                    continue;
                if (!any)
                {
                    html.Append("<h2 id=\"messages\">Messages</h2>\n\n");
                    any = true;
                }
                html.Append($"<section class=\"message\">\n<h3 id=\"{Encode(name)}\">{Encode(ShortName(name))}</h3>\n");
                html.Append(Paragraphs(Comment(path)));
                if (message.Field.Count == 0)
                {
                    html.Append("<p class=\"empty\">No fields.</p>\n</section>\n\n");
                    continue;
                }
                html.Append("<div class=\"scroll\"><table>\n<thead><tr><th>Field</th><th>Number</th><th>Type</th><th>JSON name</th><th>Description</th></tr></thead>\n<tbody>\n");
                for (var f = 0; f < message.Field.Count; f++)
                {
                    var field = message.Field[f];
                    html.Append("<tr>")
                        .Append($"<td><code>{Encode(field.Name)}</code></td>")
                        .Append($"<td>{field.Number}</td>")
                        .Append($"<td>{FieldType(field)}</td>")
                        .Append($"<td><code>{Encode(field.JsonName)}</code></td>")
                        .Append($"<td>{Inline(Comment([.. path, MessageField, f]))}</td>")
                        .Append("</tr>\n");
                }
                html.Append("</tbody>\n</table></div>\n</section>\n\n");
            }
        }

        public void RenderEnums(StringBuilder html)
        {
            var any = false;
            foreach (var (enumType, name, path) in Enums())
            {
                if (!any)
                {
                    html.Append("<h2 id=\"enums\">Enums</h2>\n\n");
                    any = true;
                }
                html.Append($"<section class=\"enum\">\n<h3 id=\"{Encode(name)}\">{Encode(ShortName(name))}</h3>\n");
                html.Append(Paragraphs(Comment(path)));
                html.Append("<div class=\"scroll\"><table>\n<thead><tr><th>Value</th><th>Number</th><th>Description</th></tr></thead>\n<tbody>\n");
                for (var v = 0; v < enumType.Value.Count; v++)
                {
                    var value = enumType.Value[v];
                    html.Append("<tr>")
                        .Append($"<td><code>{Encode(value.Name)}</code></td>")
                        .Append($"<td>{value.Number}</td>")
                        .Append($"<td>{Inline(Comment([.. path, EnumValue, v]))}</td>")
                        .Append("</tr>\n");
                }
                html.Append("</tbody>\n</table></div>\n</section>\n\n");
            }
        }

        private IEnumerable<(DescriptorProto Message, string Name, int[] Path)> Messages()
        {
            for (var n = 0; n < file.MessageType.Count; n++)
                foreach (var nested in WithNested(file.MessageType[n], FullName(file.MessageType[n].Name), [FileMessageType, n]))
                    yield return nested;
        }

        private static IEnumerable<(DescriptorProto, string, int[])> WithNested(DescriptorProto message, string name, int[] path)
        {
            yield return (message, name, path);
            for (var k = 0; k < message.NestedType.Count; k++)
                foreach (var nested in WithNested(message.NestedType[k], $"{name}.{message.NestedType[k].Name}", [.. path, MessageNestedType, k]))
                    yield return nested;
        }

        private IEnumerable<(EnumDescriptorProto Enum, string Name, int[] Path)> Enums()
        {
            for (var e = 0; e < file.EnumType.Count; e++)
                yield return (file.EnumType[e], FullName(file.EnumType[e].Name), [FileEnumType, e]);
            foreach (var (message, name, path) in Messages())
                for (var e = 0; e < message.EnumType.Count; e++)
                    yield return (message.EnumType[e], $"{name}.{message.EnumType[e].Name}", [.. path, MessageEnumType, e]);
        }

        private string FieldType(FieldDescriptorProto field)
        {
            if (field.Type == FieldDescriptorProto.Types.Type.Message
                && _messages.TryGetValue(field.TypeName, out var entry) && entry.Options?.MapEntry == true)
                return $"map&lt;{FieldType(entry.Field[0])}, {FieldType(entry.Field[1])}&gt;";

            var type = field.Type switch
            {
                FieldDescriptorProto.Types.Type.Message or FieldDescriptorProto.Types.Type.Enum
                    or FieldDescriptorProto.Types.Type.Group => TypeReference(field.TypeName),
                var scalar => $"<code>{scalar.ToString().ToLowerInvariant()}</code>"
            };
            return field.Label == FieldDescriptorProto.Types.Label.Repeated ? $"repeated {type}"
                : field.Proto3Optional ? $"optional {type}"
                : type;
        }

        /// <summary>A message or enum type, linked to its section when this page describes it.</summary>
        private string TypeReference(string typeName)
        {
            var fullName = typeName.TrimStart('.');
            var text = $"<code>{Encode(ShortName(fullName))}</code>";
            return _messages.ContainsKey(typeName) || _enums.Contains(typeName)
                ? $"<a href=\"#{Encode(fullName)}\">{text}</a>"
                : text;
        }

        private static string Kind(MethodDescriptorProto method) => (method.ClientStreaming, method.ServerStreaming) switch
        {
            (false, false) => "unary",
            (false, true) => "server streaming",
            (true, false) => "client streaming",
            (true, true) => "bidirectional streaming"
        };

        private string FullName(string name) => $"{_prefix}{name}".TrimStart('.');

        private string ShortName(string fullName) =>
            file.Package.Length > 0 && fullName.StartsWith($"{file.Package}.", StringComparison.Ordinal)
                ? fullName[(file.Package.Length + 1)..]
                : fullName;

        private string? Comment(params int[] path)
        {
            var location = file.SourceCodeInfo?.Location.FirstOrDefault(l => l.Path.SequenceEqual(path));
            return location switch
            {
                { HasLeadingComments: true } => location.LeadingComments,
                { HasTrailingComments: true } => location.TrailingComments,
                _ => null
            };
        }

        private static Dictionary<string, DescriptorProto> CollectMessages(FileDescriptorProto file)
        {
            var messages = new Dictionary<string, DescriptorProto>(StringComparer.Ordinal);
            var prefix = file.Package.Length == 0 ? "." : $".{file.Package}.";
            foreach (var message in file.MessageType)
                Add(message, $"{prefix}{message.Name}");
            return messages;

            void Add(DescriptorProto message, string name)
            {
                messages[name] = message;
                foreach (var nested in message.NestedType)
                    Add(nested, $"{name}.{nested.Name}");
            }
        }

        private static HashSet<string> CollectEnums(FileDescriptorProto file)
        {
            var prefix = file.Package.Length == 0 ? "." : $".{file.Package}.";
            var enums = new HashSet<string>(file.EnumType.Select(e => $"{prefix}{e.Name}"), StringComparer.Ordinal);
            foreach (var (name, message) in CollectMessages(file))
                foreach (var nested in message.EnumType)
                    enums.Add($"{name}.{nested.Name}");
            return enums;
        }
    }

    /// <summary>A comment as paragraphs: its lines trimmed, and a blank line starting a new paragraph.</summary>
    private static string Paragraphs(string? comment) =>
        string.Concat(ParagraphsOf(comment).Select(p => $"<p>{Encode(p)}</p>\n"));

    /// <summary>A comment inside a table cell: its paragraphs joined by line breaks.</summary>
    private static string Inline(string? comment) =>
        string.Join("<br>", ParagraphsOf(comment).Select(Encode));

    private static IEnumerable<string> ParagraphsOf(string? comment)
    {
        if (string.IsNullOrWhiteSpace(comment))
            yield break;
        var paragraph = new List<string>();
        foreach (var line in comment.ReplaceLineEndings("\n").Split('\n').Select(l => l.Trim()))
        {
            if (line.Length > 0)
            {
                paragraph.Add(line);
                continue;
            }
            if (paragraph.Count > 0)
                yield return string.Join(' ', paragraph);
            paragraph.Clear();
        }
        if (paragraph.Count > 0)
            yield return string.Join(' ', paragraph);
    }

    private static string Encode(string text) => WebUtility.HtmlEncode(text);

    private const string Styles = """
        :root { color-scheme: light dark; --text: #1f2937; --muted: #6b7280; --bg: #ffffff; --panel: #f9fafb; --border: #e5e7eb; --link: #4338ca; --code: #eef2ff; }
        @media (prefers-color-scheme: dark) {
          :root { --text: #e5e7eb; --muted: #9ca3af; --bg: #111827; --panel: #1f2937; --border: #374151; --link: #a5b4fc; --code: #2a2750; }
        }
        * { box-sizing: border-box; }
        body { margin: 0; background: var(--bg); color: var(--text); font: 15px/1.55 -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; }
        /* The bar every contract page opens with: the documents behind the page. */
        .contract-bar { display: flex; flex-wrap: wrap; align-items: center; gap: .25rem 1rem; padding: .5rem 1rem; font: 14px/1.4 -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; background: #4f46e5; color: #fff; }
        .contract-bar a { color: #fff; text-decoration: none; }
        .contract-bar a:hover { text-decoration: underline; }
        .contract-bar .title { font-weight: 600; margin-right: auto; }
        .contract-bar .doc { border: 1px solid rgba(255,255,255,.5); border-radius: 6px; padding: .1rem .5rem; font-family: ui-monospace, SFMono-Regular, Menlo, monospace; font-size: 13px; }
        main { max-width: 72rem; margin: 0 auto; padding: 1.5rem 1rem 3rem; }
        h1 { font-size: 1.75rem; margin: .5rem 0 .75rem; }
        h2 { font-size: 1.35rem; margin: 2.25rem 0 .5rem; padding-bottom: .25rem; border-bottom: 1px solid var(--border); }
        h3 { font-size: 1.1rem; margin: 1.75rem 0 .35rem; }
        .lead { color: var(--muted); }
        a { color: var(--link); }
        code { font-family: ui-monospace, SFMono-Regular, Menlo, monospace; font-size: .9em; background: var(--code); border-radius: 4px; padding: 0 .25rem; }
        /* A wide table scrolls inside its box, so a narrow screen never scrolls the page sideways. */
        .scroll { overflow-x: auto; margin: .5rem 0 1rem; }
        table { width: 100%; min-width: 40rem; border-collapse: collapse; background: var(--panel); }
        .kind { white-space: nowrap; }
        td:last-child { min-width: 18rem; }
        th, td { text-align: left; vertical-align: top; padding: .45rem .6rem; border: 1px solid var(--border); }
        th { font-weight: 600; }
        .empty { color: var(--muted); }
        footer { max-width: 72rem; margin: 0 auto; padding: 1rem; color: var(--muted); font-size: .85rem; border-top: 1px solid var(--border); }
        """;
}
