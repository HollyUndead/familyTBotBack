using System.Text.Json.Serialization;


public record TgResponse<T>(
    [property: JsonPropertyName("ok")] bool Ok,
    [property: JsonPropertyName("result")] T? Result);

public record TgUserPhotos(
    [property: JsonPropertyName("total_count")] int TotalCount,
    [property: JsonPropertyName("photos")] List<List<TgPhotoSize>> Photos);

public record TgPhotoSize(
    [property: JsonPropertyName("file_id")] string FileId,
    [property: JsonPropertyName("file_unique_id")] string FileUniqueId,
    [property: JsonPropertyName("width")] int Width,
    [property: JsonPropertyName("height")] int Height);

public record TgFile(
    [property: JsonPropertyName("file_id")] string FileId,
    [property: JsonPropertyName("file_path")] string? FilePath);
    
