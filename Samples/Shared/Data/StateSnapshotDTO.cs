using NLog;
using Shared.Enums;
using Shared.Utilities;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Shared.Data
{
    public class PropertySnapshotDTO
    {
        [JsonPropertyName("function")]
        public Function Function { get; set; }

        [JsonPropertyName("content")]
        public object Content { get; set; }

        [JsonPropertyName("updatedTime")]
        public long UpdatedTime { get; set; }

        public PropertySnapshotDTO()
        {
        }

        public PropertySnapshotDTO(Function function, object content, long updatedTime)
        {
            Function = function;
            Content = content;
            UpdatedTime = updatedTime;
        }

        public object GetClrValue()
        {
            if (Content is JsonElement element)
            {
                switch (element.ValueKind)
                {
                    case JsonValueKind.True:
                        return true;
                    case JsonValueKind.False:
                        return false;
                    case JsonValueKind.Number:
                        if (element.TryGetInt32(out int intVal))
                            return intVal;
                        if (element.TryGetInt64(out long longVal))
                            return longVal;
                        if (element.TryGetDouble(out double doubleVal))
                            return doubleVal;
                        return element.GetRawText();
                    case JsonValueKind.String:
                        return element.GetString();
                    case JsonValueKind.Null:
                    case JsonValueKind.Undefined:
                        return null;
                    default:
                        return element.GetRawText();
                }
            }
            return Content;
        }
    }

    public class StateSnapshotDTO
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        [JsonPropertyName("timestamp")]
        public long Timestamp { get; set; } = DateTime.UtcNow.Ticks;

        [JsonPropertyName("properties")]
        public List<PropertySnapshotDTO> Properties { get; set; } = new List<PropertySnapshotDTO>();

        public void AddProperty(Function function, object content, long updatedTime)
        {
            Properties.Add(new PropertySnapshotDTO(function, content, updatedTime));
        }

        public bool TryGetProperty(Function function, out PropertySnapshotDTO propertySnapshot)
        {
            for (int i = 0; i < Properties.Count; i++)
            {
                if (Properties[i].Function == function)
                {
                    propertySnapshot = Properties[i];
                    return true;
                }
            }
            propertySnapshot = null;
            return false;
        }

        public string ToJson()
        {
            try
            {
                return JsonSerializer.Serialize(this);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to serialize StateSnapshotDTO to JSON.");
                return string.Empty;
            }
        }

        public static StateSnapshotDTO FromJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }

            try
            {
                return JsonSerializer.Deserialize<StateSnapshotDTO>(json);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to deserialize StateSnapshotDTO from JSON.");
                return null;
            }
        }
    }
}
