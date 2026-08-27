using System.Text.Json.Serialization;

namespace Vion.Contracts.Hw.Modbus
{
    /// <summary>
    ///     Outcome of a Modbus request. The underlying values are the Modbus exception codes, but the JSON
    ///     representation is the member <b>name</b> — pinned by the <see cref="JsonStringEnumConverter" /> on the type
    ///     itself so the wire shape does not depend on the caller's <c>JsonSerializerOptions</c>.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum ModbusResponseCode : byte
    {
        /// <summary>The request was successful.</summary>
        Ok = 0x00,

        /// <summary>The function code received in the query is not an allowable action for the server.</summary>
        IllegalFunction = 0x01,

        /// <summary>The data address received in the query is not an allowable address for the server.</summary>
        IllegalDataAddress = 0x02,

        /// <summary>A value contained in the query data field is not an allowable value for the server.</summary>
        IllegalDataValue = 0x03,

        /// <summary>An unrecoverable error occurred while the server was attempting to perform the requested action.</summary>
        ServerDeviceFailure = 0x04,

        /// <summary>
        ///     Specialized use in conjunction with programming commands. The server has accepted the request and is
        ///     processing it, but a long duration of time will be required to do so.
        /// </summary>
        Acknowledge = 0x05,

        /// <summary>
        ///     Specialized use in conjunction with programming commands. The server is engaged in processing a
        ///     long-duration program command.
        /// </summary>
        ServerDeviceBusy = 0x06,

        /// <summary>
        ///     Specialized use in conjunction with function codes 20 and 21 and reference type 6, to indicate that the
        ///     extended file area failed to pass a consistency check.
        /// </summary>
        MemoryParityError = 0x08,

        /// <summary>
        ///     Specialized use in conjunction with gateways: the gateway was unable to allocate an internal
        ///     communication path from the input port to the output port for processing the request.
        /// </summary>
        GatewayPathUnavailable = 0x0A,

        /// <summary>
        ///     Specialized use in conjunction with gateways: no response was obtained from the target device.
        /// </summary>
        GatewayTargetDeviceFailedToRespond = 0x0B,

        /// <summary>An error unrelated to the Modbus protocol, e.g., transport or infrastructure failures.</summary>
        NonProtocolError = 0xFF,
    }
}
