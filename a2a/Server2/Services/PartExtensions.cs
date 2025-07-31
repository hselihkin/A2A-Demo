using A2A.Models;
using System.Text;
using System.Text.Json;

public static class PartExtensions
{

    public static string ToText(this Part part)
    {
        ArgumentNullException.ThrowIfNull(part);
        switch (part)
        {
            case TextPart textPart:
                return textPart.Text;
            case FilePart filePart:
                var fileContentBuilder = new StringBuilder();
                fileContentBuilder.AppendLine("----- FILE -----");
                if (!string.IsNullOrWhiteSpace(filePart.File.Name)) fileContentBuilder.AppendLine($"Name    : {filePart.File.Name}");
                if (!string.IsNullOrWhiteSpace(filePart.File.MimeType)) fileContentBuilder.AppendLine($"MIME    : {filePart.File.MimeType}");
                if (!string.IsNullOrWhiteSpace(filePart.File.Bytes)) fileContentBuilder.AppendLine($"Size    : {Convert.FromBase64String(filePart.File.Bytes).Length}");
                else if (filePart.File.Uri is not null) fileContentBuilder.AppendLine($"URI     : {filePart.File.Uri}");
                fileContentBuilder.AppendLine("----------------");
                return fileContentBuilder.ToString();
            case DataPart dataPart:
                var jsonContentBuilder = new StringBuilder();
                jsonContentBuilder.AppendLine("```json");
                jsonContentBuilder.AppendLine(JsonSerializer.Serialize(dataPart.Data));
                jsonContentBuilder.AppendLine("```");
                return jsonContentBuilder.ToString();
            default:
                throw new NotSupportedException($"The specified part type '{part.Type ?? "None"}' is not supported");
        }
    }

}