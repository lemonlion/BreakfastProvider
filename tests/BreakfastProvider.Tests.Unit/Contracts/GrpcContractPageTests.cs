using BreakfastProvider.Api.Contracts;
using Google.Protobuf.Reflection;

namespace BreakfastProvider.Tests.Unit.Contracts;

/// <summary>
/// The documentation page's handling of what breakfast.proto does not use yet — other streaming kinds, enums, nested
/// and map types, optional fields, external types, multi-paragraph comments, markup in comments. The component
/// scenarios cover the page the service actually serves.
/// </summary>
public class GrpcContractPageTests
{
    private static readonly string Page = GrpcContractPage.Render(Descriptors());

    [Fact]
    public void Each_method_shows_its_streaming_kind()
    {
        Row("Get").Should().Contain(">unary<");
        Row("Watch").Should().Contain(">server streaming<");
        Row("Upload").Should().Contain(">client streaming<");
        Row("Chat").Should().Contain(">bidirectional streaming<");
    }

    [Fact]
    public void Enums_are_listed_with_their_values_and_comments()
    {
        Page.Should().Contain("<h3 id=\"kitchen.Station\">Station</h3>");
        Row("GRIDDLE").Should().Contain("<td>1</td>").And.Contain("The hot plate.");
        Page.Should().Contain("<h3 id=\"kitchen.Ticket.Priority\">Ticket.Priority</h3>");
    }

    [Fact]
    public void Field_types_are_written_the_proto_way()
    {
        Row("tags").Should().Contain("repeated <code>string</code>");
        Row("note").Should().Contain("optional <code>string</code>");
        Row("counts").Should().Contain("map&lt;<code>string</code>, <code>int32</code>&gt;");
        Row("station").Should().Contain("<a href=\"#kitchen.Station\"><code>Station</code></a>");
        Row("line").Should().Contain("<a href=\"#kitchen.Ticket.Line\"><code>Ticket.Line</code></a>");
    }

    [Fact]
    public void A_map_entry_is_not_listed_as_a_message_of_its_own()
    {
        Page.Should().NotContain("CountsEntry");
    }

    [Fact]
    public void A_type_the_page_does_not_describe_is_named_without_a_link()
    {
        Row("placed_at").Should().Contain("<code>google.protobuf.Timestamp</code>").And.NotContain("href=");
    }

    [Fact]
    public void A_blank_line_in_a_comment_starts_a_new_paragraph()
    {
        Page.Should().Contain("<p>Kitchen tickets.</p>\n<p>Second paragraph.</p>");
        Row("Get").Should().Contain("Gets a ticket.<br>Unary.");
    }

    [Fact]
    public void Markup_in_a_comment_is_shown_not_run()
    {
        Page.Should().Contain("&lt;script&gt;alert(1)&lt;/script&gt;").And.NotContain("<script>");
    }

    [Fact]
    public void Rendering_is_deterministic()
    {
        GrpcContractPage.Render(Descriptors()).Should().Be(Page);
    }

    private static string Row(string name) =>
        Page.Split("</tr>").Single(row => row.Contains($"<tr><td><code>{name}</code></td>"));

    private static FileDescriptorSet Descriptors()
    {
        var file = new FileDescriptorProto { Name = "kitchen.proto", Package = "kitchen", Syntax = "proto3" };

        var ticket = new DescriptorProto { Name = "Ticket" };
        ticket.Field.Add(Field("tags", 1, FieldDescriptorProto.Types.Type.String, repeated: true));
        ticket.Field.Add(Field("note", 2, FieldDescriptorProto.Types.Type.String, optional: true));
        ticket.Field.Add(Field("counts", 3, FieldDescriptorProto.Types.Type.Message, ".kitchen.Ticket.CountsEntry", repeated: true));
        ticket.Field.Add(Field("station", 4, FieldDescriptorProto.Types.Type.Enum, ".kitchen.Station"));
        ticket.Field.Add(Field("line", 5, FieldDescriptorProto.Types.Type.Message, ".kitchen.Ticket.Line"));
        ticket.Field.Add(Field("placed_at", 6, FieldDescriptorProto.Types.Type.Message, ".google.protobuf.Timestamp"));
        var countsEntry = new DescriptorProto { Name = "CountsEntry", Options = new MessageOptions { MapEntry = true } };
        countsEntry.Field.Add(Field("key", 1, FieldDescriptorProto.Types.Type.String));
        countsEntry.Field.Add(Field("value", 2, FieldDescriptorProto.Types.Type.Int32));
        ticket.NestedType.Add(countsEntry);
        ticket.NestedType.Add(new DescriptorProto { Name = "Line" });
        var priority = new EnumDescriptorProto { Name = "Priority" };
        priority.Value.Add(new EnumValueDescriptorProto { Name = "NORMAL", Number = 0 });
        ticket.EnumType.Add(priority);
        file.MessageType.Add(ticket);

        var station = new EnumDescriptorProto { Name = "Station" };
        station.Value.Add(new EnumValueDescriptorProto { Name = "STATION_UNSPECIFIED", Number = 0 });
        station.Value.Add(new EnumValueDescriptorProto { Name = "GRIDDLE", Number = 1 });
        file.EnumType.Add(station);

        var service = new ServiceDescriptorProto { Name = "Kitchen" };
        service.Method.Add(Method("Get", clientStreaming: false, serverStreaming: false));
        service.Method.Add(Method("Watch", clientStreaming: false, serverStreaming: true));
        service.Method.Add(Method("Upload", clientStreaming: true, serverStreaming: false));
        service.Method.Add(Method("Chat", clientStreaming: true, serverStreaming: true));
        file.Service.Add(service);

        file.SourceCodeInfo = new SourceCodeInfo();
        Comment(file, " Kitchen tickets.\n\n Second paragraph.\n", 6, 0);
        Comment(file, " Holds <script>alert(1)</script> as text.\n", 4, 0);
        Comment(file, " Gets a ticket.\n\n Unary.\n", 6, 0, 2, 0);
        Comment(file, " The hot plate.\n", 5, 0, 2, 1);

        var set = new FileDescriptorSet();
        set.File.Add(file);
        return set;
    }

    private static FieldDescriptorProto Field(string name, int number, FieldDescriptorProto.Types.Type type,
        string? typeName = null, bool repeated = false, bool optional = false)
    {
        var field = new FieldDescriptorProto
        {
            Name = name,
            Number = number,
            Type = type,
            JsonName = name,
            Label = repeated ? FieldDescriptorProto.Types.Label.Repeated : FieldDescriptorProto.Types.Label.Optional
        };
        if (typeName is not null)
            field.TypeName = typeName;
        if (optional)
            field.Proto3Optional = true;
        return field;
    }

    private static MethodDescriptorProto Method(string name, bool clientStreaming, bool serverStreaming) => new()
    {
        Name = name,
        InputType = ".kitchen.Ticket",
        OutputType = ".kitchen.Ticket",
        ClientStreaming = clientStreaming,
        ServerStreaming = serverStreaming
    };

    private static void Comment(FileDescriptorProto file, string comment, params int[] path)
    {
        var location = new SourceCodeInfo.Types.Location { LeadingComments = comment };
        location.Path.Add(path);
        file.SourceCodeInfo.Location.Add(location);
    }
}
