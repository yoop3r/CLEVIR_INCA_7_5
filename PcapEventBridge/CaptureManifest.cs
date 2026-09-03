using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PcapEventBridge
{
    /// <summary>
    /// Sidecar metadata written alongside a LiDAR PCAP recording, intended for
    /// downstream tooling (e.g. replaying the PCAP through the Hesai ROS 2
    /// driver) that needs calibration, device identity, extrinsics, time-sync
    /// quality, and integrity information that a bare PCAP does not carry.
    ///
    /// Populated primarily from the Hesai HTTP JSON API (pandar.cgi) at capture
    /// time — see HesaiHttpApi.GetMetadata — with a disk-file fallback for fields
    /// that cannot be queried live (firetimes have no HTTP object; angle
    /// correction falls back to the configured CorrectionFilePath if the
    /// lidar_calibration query fails).
    ///
    /// Schema v2 replaced the PTC-based acquisition path (which proved unusable
    /// against live units) with HTTP, and replaced the boolean SourcedFromPtc
    /// flags with a neutral Source string.
    /// </summary>
    public sealed class CaptureManifest
    {
        /// <summary>
        /// Schema version for this manifest. Increment when making breaking
        /// changes to the JSON shape so downstream parsers can branch on it.
        ///
        /// v1: PTC-sourced, boolean SourcedFromPtc provenance flags.
        /// v2: HTTP-sourced, string Source provenance, additional device fields.
        /// </summary>
        public int ManifestVersion { get; set; } = 2;

        /// <summary>
        /// Configured device identifier this manifest describes (e.g. "LIDAR1").
        /// Distinguishes manifests when several are generated in one run.
        /// </summary>
        public string? DeviceId { get; set; }

        /// <summary>Device IP address the metadata was queried from.</summary>
        public string? DeviceIpAddress { get; set; }

        /// <summary>UTC timestamp at which this manifest was generated.</summary>
        public DateTime GeneratedUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// How this manifest was produced: "CaptureStop" when written automatically
        /// alongside a finished PCAP, or "OnDemand" when generated manually from
        /// the configuration screen without an associated recording.
        /// </summary>
        public string? GenerationTrigger { get; set; }

        public DeviceIdentity? Device { get; set; }
        public HesaiDeviceConfiguration? Configuration { get; set; }
        public LiveStatusSnapshot? StatusAtCaptureStart { get; set; }

        public CalibrationBlob? AngleCorrection { get; set; }
        public CalibrationBlob? Firetimes { get; set; }

        public ExtrinsicRecord? Extrinsic { get; set; }
        public string? FrameConvention { get; set; }

        public TimeSyncSnapshot? TimeSync { get; set; }
        public IntegrityCounters? Integrity { get; set; }

        public List<EventMarkerRecord> EventMarkers { get; set; } = new();

        public PcapLinkage? Pcap { get; set; }

        private static readonly JsonSerializerOptions SerializerOptions = new()
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };

        public string ToJson() => JsonSerializer.Serialize(this, SerializerOptions);

        public static CaptureManifest? FromJson(string json) =>
            JsonSerializer.Deserialize<CaptureManifest>(json, SerializerOptions);

        public void WriteToFile(string filePath) =>
            System.IO.File.WriteAllText(filePath, ToJson());
    }

    /// <summary>
    /// Static device identity, sourced from the HTTP object <c>device_info</c>.
    /// </summary>
    public sealed class DeviceIdentity
    {
        public string? SerialNumber { get; set; }
        public string? DateOfManufacture { get; set; }

        /// <summary>Product name, e.g. "Pandar128E3X".</summary>
        public string? Model { get; set; }

        public int NumberOfChannels { get; set; }
        public string? SoftwareVersion { get; set; }
        public string? ControlFirmwareVersion { get; set; }

        /// <summary>FPGA/sensor firmware version.</summary>
        public string? SensorFirmwareVersion { get; set; }

        public string? PartNumber { get; set; }

        /// <summary>Device MAC address, e.g. "EC:9F:0D:01:2F:CF".</summary>
        public string? MacAddress { get; set; }

        public string? HardwareVersion { get; set; }
        public string? BootVersion { get; set; }

        /// <summary>
        /// Factory azimuth offset applied by the device, in hundredths of a degree.
        /// Downstream conversion needs this to reconcile reported vs. true azimuth.
        /// </summary>
        public int AngleOffset { get; set; }

        /// <summary>0 - single direction, 1 - dual direction.</summary>
        public int MotorType { get; set; }

        /// <summary>
        /// Motor direction decoded from MotorType ("SingleDirection" or
        /// "DualDirection"). Null for an undocumented code.
        /// </summary>
        public string? MotorTypeName { get; set; }

        /// <summary>
        /// True if the device stamps a UDP sequence number into each packet.
        /// Downstream tooling uses this to decide whether packet-loss detection
        /// is possible from the PCAP alone.
        /// </summary>
        public bool UdpSequenceEnabled { get; set; }

        /// <summary>
        /// Reported verbatim from device_info. Documented by the vendor as
        /// "Not Used", so it carries no meaning for downstream conversion and is
        /// retained only for traceability.
        /// </summary>
        public int LidarDataFormat { get; set; }

        /// <summary>"Http" if queried live, otherwise "Unknown".</summary>
        public string Source { get; set; } = "Unknown";
    }

    /// <summary>
    /// Device configuration at capture time, sourced from the HTTP objects
    /// <c>lidar_config</c>, <c>lidar_data&amp;key=lidar_mode</c>, and <c>lidar_sync</c>.
    /// </summary>
    public sealed class HesaiDeviceConfiguration
    {
        /// <summary>0 - last, 1 - strongest, 2 - last and strongest, 3 - first, 4 - last and first, 5 - first and strongest.</summary>
        public int? ReturnMode { get; set; }

        /// <summary>
        /// Human-readable return mode decoded from ReturnMode, e.g. "Last",
        /// "Dual (Last + Strongest)". Null for an undocumented code.
        /// </summary>
        public string? ReturnModeName { get; set; }

        /// <summary>0 - GPS, 1 - PTP.</summary>
        public int? ClockSource { get; set; }

        /// <summary>Raw SpinSpeed code reported by the device (not RPM).</summary>
        public int? SpinSpeedCode { get; set; }

        /// <summary>
        /// Spin rate in RPM, decoded from SpinSpeedCode (2 - 600 rpm, 3 - 1200 rpm).
        /// Null when the device reports an undocumented code; consult
        /// SpinSpeedCode in that case.
        /// </summary>
        public int? SpinRateRpm { get; set; }

        public bool? SyncEnabled { get; set; }
        public int? SyncAngleHundredthsOfDegree { get; set; }

        /// <summary>0 - operation, 1 - standby.</summary>
        public int? StandbyMode { get; set; }

        /// <summary>0 - angle based, 1 - time based.</summary>
        public int? TriggerMethod { get; set; }

        public int? NoiseFiltering { get; set; }
        public int? ReflectivityMapping { get; set; }

        /// <summary>0 - clockwise, 1 - counter-clockwise.</summary>
        public int? RotateDirection { get; set; }

        /// <summary>
        /// Motor rotation decoded from RotateDirection ("Clockwise" or
        /// "Counterclockwise"). Null for an undocumented code.
        /// </summary>
        public string? RotateDirectionName { get; set; }

        /// <summary>UDP destination the device is currently transmitting to.</summary>
        public string? DestinationIp { get; set; }
        public int? DestinationPort { get; set; }

        /// <summary>GPS serial port used for NMEA input.</summary>
        public int? GpsPort { get; set; }

        /// <summary>Raw GPS NMEA data format code (0 - GPRMC, 1 - GPGGA).</summary>
        public int? ClockDataFormat { get; set; }

        /// <summary>
        /// NMEA sentence type decoded from ClockDataFormat ("GPRMC" or "GPGGA").
        /// Null when the device reports an undocumented code; consult
        /// ClockDataFormat in that case.
        /// </summary>
        public string? ClockDataFormatName { get; set; }

        /// <summary>PTP profile selector, and the raw PTP configuration JSON as reported.</summary>
        public int? PtpProfile { get; set; }

        /// <summary>
        /// Timing standard decoded from PtpProfile: "IEEE 1588v2", "IEEE 802.1AS",
        /// or "IEEE 802.1AS Automotive". Null for an undocumented code.
        /// </summary>
        public string? PtpProfileName { get; set; }

        public string? PtpConfigJson { get; set; }

        public int? InterstitialPoints { get; set; }
        public int? RetroMultiReflection { get; set; }

        /// <summary>"Http" if queried live, otherwise "Unknown".</summary>
        public string Source { get; set; } = "Unknown";
    }

    /// <summary>
    /// Live telemetry snapshot at capture time, sourced from the HTTP objects
    /// <c>lidar_monitor</c>, <c>TimeStatistic</c>, and <c>PTP_lock_offset</c>.
    /// </summary>
    public sealed class LiveStatusSnapshot
    {
        public long? SystemUptimeSeconds { get; set; }

        /// <summary>Cumulative powered-on time in seconds (TotalWorkingTime).</summary>
        public long? TotalOperationTimeSeconds { get; set; }
        public long? StartupTimes { get; set; }

        /// <summary>Current internal temperature in degrees Celsius.</summary>
        public float? TemperatureCelsius { get; set; }
        public float? HumidityPercentRh { get; set; }

        public bool? GpsPpsLocked { get; set; }
        public bool? GpsGprmcLocked { get; set; }

        /// <summary>Raw PTPStatus string, e.g. "Locked (offset: 2536 ns)".</summary>
        public string? PtpStatus { get; set; }

        /// <summary>PTP offset in nanoseconds, parsed out of PtpStatus when present.</summary>
        public long? PtpOffsetNanoseconds { get; set; }

        /// <summary>True when PtpStatus reports a locked clock.</summary>
        public bool? PtpLocked { get; set; }

        /// <summary>Configured PTP lock-offset upper limit, as reported by the device.</summary>
        public string? PtpLockOffsetLimit { get; set; }

        /// <summary>Input power rail telemetry from lidar_monitor, as reported (with units).</summary>
        public string? InputCurrent { get; set; }
        public string? InputVoltage { get; set; }
        public string? InputPower { get; set; }

        /// <summary>Configured phase offset, in degrees.</summary>
        public int? PhaseOffset { get; set; }

        /// <summary>"Http" if queried live, otherwise "Unknown".</summary>
        public string Source { get; set; } = "Unknown";
    }

    /// <summary>
    /// An embedded calibration table (angle correction or firetimes), stored as raw
    /// bytes so downstream tooling can write it directly to the file the vendor
    /// driver/SDK expects (both are plain CSV text on current firmware).
    /// </summary>
    public sealed class CalibrationBlob
    {
        /// <summary>"Http" if queried live from the device, "File" if read from a configured path on disk.</summary>
        public string Source { get; set; } = "Unknown";

        /// <summary>Original file path, when Source is "File".</summary>
        public string? FilePath { get; set; }

        /// <summary>Raw content, base64-encoded (JSON has no native byte-array support).</summary>
        public string? ContentBase64 { get; set; }

        /// <summary>
        /// Built from the live <c>lidar_calibration</c> HTTP response, whose body
        /// is the angle-correction CSV as plain text.
        /// </summary>
        public static CalibrationBlob FromHttp(string csvText) => new()
        {
            Source = "Http",
            ContentBase64 = Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes(csvText)),
        };

        public static CalibrationBlob FromFile(string filePath, byte[] content) => new()
        {
            Source = "File",
            FilePath = filePath,
            ContentBase64 = Convert.ToBase64String(content),
        };
    }

    /// <summary>
    /// Versioned sensor-to-vehicle mounting transform. Distinct from device
    /// calibration (intrinsics): this is per-vehicle, per-installation, and
    /// changes whenever the unit is remounted or realigned.
    /// </summary>
    public sealed class ExtrinsicRecord
    {
        /// <summary>Identifier for the alignment procedure/run that produced this transform.</summary>
        public string? CalibrationId { get; set; }

        public DateTime? DatePerformed { get; set; }
        public string? Method { get; set; }

        /// <summary>Translation in meters, vehicle frame.</summary>
        public double[] TranslationMeters { get; set; } = new double[3];

        /// <summary>Rotation as a quaternion [x, y, z, w].</summary>
        public double[] RotationQuaternion { get; set; } = new double[4];

        /// <summary>Alignment residual/quality metric, if the procedure produced one.</summary>
        public double? ResidualError { get; set; }

        /// <summary>
        /// True if this record reflects a real alignment result; false if it is an
        /// unset/identity placeholder, so downstream consumers can distinguish
        /// "aligned to identity" from "never aligned".
        /// </summary>
        public bool IsCalibrated { get; set; }
    }

    /// <summary>Time-sync provenance, sourced from ITimeSyncProvider at capture time.</summary>
    public sealed class TimeSyncSnapshot
    {
        public string? ProviderName { get; set; }
        public bool IsSynchronized { get; set; }
        public bool IsPtpSynchronized { get; set; }
        public string? PtpStatusText { get; set; }
        public string? NtpStatusText { get; set; }
        public DateTime? LastUpdateUtc { get; set; }
    }

    /// <summary>Data-quality counters accumulated over the capture session.</summary>
    public sealed class IntegrityCounters
    {
        public long PacketCount { get; set; }
        public long DroppedPackets { get; set; }
        public long ChecksumErrors { get; set; }
        public long OutOfOrderPackets { get; set; }
        public long ResyncCount { get; set; }
        public long BytesSkippedTotal { get; set; }
    }

    public sealed class EventMarkerRecord
    {
        public long FrameNumber { get; set; }
        public DateTime Timestamp { get; set; }
        public string? EventType { get; set; }
        public string? Message { get; set; }
        public int SequenceNumber { get; set; }
    }

    /// <summary>Links this manifest back to its PCAP file, so the pair can be validated if separated.</summary>
    public sealed class PcapLinkage
    {
        public string? FileName { get; set; }
        public long FileSizeBytes { get; set; }
        public string? Sha256Checksum { get; set; }
    }
}
