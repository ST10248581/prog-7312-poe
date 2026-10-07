using System.ComponentModel.DataAnnotations;
using SmartX.Api.Models.Validation;

namespace SmartX.Api.Models.Requests;

/// <summary>Edits a device's registration record. Same rules as registration.</summary>
public class UpdateSensorPayloadRequest
{
    [Required(ErrorMessage = "MAC address is required.")]
    [RegularExpression(SensorRules.MacAddressPattern,
        ErrorMessage = "MAC address must be six hex pairs, e.g. 5C:A1:2A:2C:3C:6F.")]
    public string MacAddress { get; set; } = string.Empty;

    [Required(ErrorMessage = "Room is required.")]
    [StringLength(SensorRules.RoomMaxLength, MinimumLength = SensorRules.RoomMinLength,
        ErrorMessage = "Room must be 2 to 40 characters.")]
    [RegularExpression(SensorRules.RoomPattern,
        ErrorMessage = "Room may only contain letters, digits, spaces and . - /.")]
    public string Room { get; set; } = string.Empty;

    [Required(ErrorMessage = "Zone is required.")]
    [RegularExpression(SensorRules.ZonePattern, ErrorMessage = "Zone must be one of the mesh zones, e.g. Zone A.")]
    public string Zone { get; set; } = string.Empty;

    [Required(ErrorMessage = "Node ID is required.")]
    [RegularExpression(SensorRules.NodeIdPattern,
        ErrorMessage = "Node ID must be a 2-5 letter prefix and a 3-4 digit number, e.g. ENV-041.")]
    public string NodeId { get; set; } = string.Empty;

    [EnumDataType(typeof(SensorCategory), ErrorMessage = "Category is not a recognised sensor category.")]
    public SensorCategory Category { get; set; }
}
