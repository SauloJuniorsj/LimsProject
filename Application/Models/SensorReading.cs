using System.Text.Json.Serialization;

namespace LimsProject.Application.Models;

public record SensorReading([property: JsonRequired] decimal Temperature);
