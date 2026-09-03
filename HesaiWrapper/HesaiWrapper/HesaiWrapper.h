#pragma once

#ifdef HESAIWRAPPER_EXPORTS
#define HESAI_API __declspec(dllexport)
#else
#define HESAI_API __declspec(dllimport)
#endif

extern "C" {
    /// <summary>
    /// Statistics structure matching VB.NET HesaiSdkStats
    /// </summary>
    struct HesaiSdkStats {
        unsigned long long packets_received;
        unsigned long long packets_dropped;
        unsigned long long checksum_errors;
        unsigned long long out_of_order_packets;
        unsigned long long total_bytes;
        long long last_packet_timestamp;  // Unix timestamp (ms)
    };

    /// <summary>
    /// ✅ FIXED: Extended configuration structure for per-device settings
    /// All string pointers can be NULL for defaults
    /// NOTE: Using int for booleans to match VB.NET MarshalAs(UnmanagedType.Bool) = 4-byte BOOL
    /// </summary>
    struct HesaiDeviceConfig {
        const char* device_id;              // Required: Device identifier
        const char* ip_address;             // Required: LiDAR IP address
        int data_port;                      // Required: UDP data port

        // ✅ Optional: Calibration files (NULL = use SDK embedded defaults)
        const char* correction_file_path;   // Angle correction file path
        const char* firetimes_path;         // Firetime correction file path

        // ✅ Optional: Network configuration (NULL = use defaults)
        const char* host_ip_address;        // Host IP (NULL = "0.0.0.0" bind to any)
        const char* multicast_ip_address;   // Multicast IP (NULL = no multicast)

        // ✅ Optional: PTC configuration
        int ptc_port;                       // PTC port (0 = default 9347)
        int use_ptc_connected;              // ✅ FIXED: int instead of bool (matches VB.NET BOOL)

        // ✅ Optional: Threading configuration
        int enable_parser_thread;           // ✅ FIXED: int instead of bool
        int enable_udp_thread;              // ✅ FIXED: int instead of bool

        // ✅ NEW: Validation-only mode (no UDP binding)
        // When true: SDK tracks statistics WITHOUT binding to UDP port
        // Use this when PcapDotNet handles actual packet capture
        int validation_only;                // ✅ NEW: 1 = validation only (no UDP bind)
    };

    /// <summary>
    /// Gets statistics for a specific LiDAR device
    /// </summary>
    /// <param name="deviceId">Device identifier (e.g., "LiDAR1")</param>
    /// <param name="stats">Output statistics structure</param>
    /// <returns>0 on success, -1 on error</returns>
    HESAI_API int hesai_get_device_stats(const char* deviceId, HesaiSdkStats* stats);

    /// <summary>
    /// Resets statistics counters for a device
    /// </summary>
    /// <param name="deviceId">Device identifier</param>
    /// <returns>0 on success, -1 on error</returns>
    HESAI_API int hesai_reset_device_stats(const char* deviceId);

    /// <summary>
    /// Initializes the Hesai SDK (call once at startup)
    /// </summary>
    /// <returns>0 on success, -1 on error</returns>
    HESAI_API int hesai_initialize();

    /// <summary>
    /// Shuts down the Hesai SDK (call once at exit)
    /// </summary>
    HESAI_API void hesai_shutdown();

    /// <summary>
    /// ✅ LEGACY: Simple device registration (uses defaults)
    /// Kept for backward compatibility - prefer hesai_register_device_ex
    /// </summary>
    /// <param name="deviceId">Device identifier</param>
    /// <param name="ipAddress">LiDAR IP address</param>
    /// <param name="dataPort">UDP data port</param>
    /// <returns>0 on success, -1 on error</returns>
    HESAI_API int hesai_register_device(const char* deviceId, const char* ipAddress, int dataPort);

    /// <summary>
    /// ✅ NEW: Extended device registration with full configuration
    /// Allows per-device customization of all SDK parameters
    /// </summary>
    /// <param name="config">Configuration structure (device_id, ip_address, data_port required)</param>
    /// <returns>0 on success, -1 on error</returns>
    HESAI_API int hesai_register_device_ex(const HesaiDeviceConfig* config);

    /// <summary>
    /// ✅ NEW: Register device in VALIDATION-ONLY mode (no UDP binding)
    /// Use this when PcapDotNet handles packet capture and you only need SDK statistics.
    /// The SDK will NOT bind to UDP ports, avoiding conflicts with PcapDotNet.
    /// Call hesai_validate_packet() to feed captured packets for validation.
    /// </summary>
    /// <param name="deviceId">Device identifier</param>
    /// <param name="ipAddress">LiDAR IP address (for identification only)</param>
    /// <param name="dataPort">UDP data port (for identification only)</param>
    /// <returns>0 on success, -1 on error</returns>
    HESAI_API int hesai_register_device_validation_only(const char* deviceId, const char* ipAddress, int dataPort);

    /// <summary>
    /// ✅ NEW: Feed a captured packet to the SDK for validation
    /// Use this in validation-only mode to get checksum/sequence statistics
    /// without the SDK binding to UDP ports.
    /// </summary>
    /// <param name="deviceId">Device identifier</param>
    /// <param name="packetData">Raw UDP payload bytes</param>
    /// <param name="packetLength">Length of packet data</param>
    /// <returns>0 on success (valid packet), negative on error/invalid</returns>
    HESAI_API int hesai_validate_packet(const char* deviceId, const unsigned char* packetData, int packetLength);

    /// <summary>
    /// Unregisters a device and stops its capture
    /// </summary>
    /// <param name="deviceId">Device identifier</param>
    /// <returns>0 on success, -1 on error</returns>
    HESAI_API int hesai_unregister_device(const char* deviceId);

    // ====================================================================
    // PTC Manifest Queries (capture-manifest metadata)
    // These open a short-lived PTC TCP connection independent of any
    // registered device (works even in validation_only mode, since no
    // ManagedDevice/HesaiLidarSdk instance is required).
    // ====================================================================

    /// <summary>
    /// Inventory info (PTC command 0x07). Field layout and lengths match the
    /// P128 TCP API v1.9 PTC_COMMAND_GET_INVENTORY_INFO response payload
    /// (228 bytes total). All string fields are NUL-padded, not necessarily
    /// NUL-terminated if they fill the entire field width.
    /// </summary>
    struct HesaiInventoryInfo {
        char sn[19];                   // 18 bytes + NUL
        char date_of_manufacture[17];  // 16 bytes + NUL
        unsigned char mac[6];          // raw MAC bytes
        char sw_ver[17];               // 16 bytes + NUL
        char hw_ver[17];               // 16 bytes + NUL
        char control_fw_ver[17];       // 16 bytes + NUL
        char sensor_fw_ver[17];        // 16 bytes + NUL
        unsigned short angle_offset;   // big-endian on wire, native here
        unsigned char model;           // 3 = Pandar128
        unsigned char motor_type;      // 0 = single direction, 1 = dual
        unsigned char num_of_lines;    // channel count
        char pn[33];                  // 32 bytes + NUL
        unsigned char customer_pn_enable;
        char customer_pn[21];         // 20 bytes + NUL
        char duns[10];                 // 9 bytes + NUL
        char vpps[15];                 // 14 bytes + NUL
        char boot_ver[17];             // 16 bytes + NUL
        char cruise_pn[9];             // 8 bytes + NUL
        char gm_sw_pn[9];               // 8 bytes + NUL
        char gm_hw_pn[9];               // 8 bytes + NUL
    };

    /// <summary>
    /// Config info (PTC command 0x08). Matches PTC_COMMAND_GET_CONFIG_INFO
    /// response payload from the P128 TCP API v1.9 doc.
    /// </summary>
    struct HesaiConfigInfo {
        unsigned char ipaddr[4];
        unsigned char mask[4];
        unsigned char gateway[4];
        unsigned char dest_ipaddr[4];
        unsigned short dest_lidar_udp_port;
        unsigned short dest_gps_udp_port;
        unsigned short spin_rate;
        unsigned char sync;
        unsigned short sync_angle;
        unsigned short start_angle;    // not used per spec
        unsigned short stop_angle;     // not used per spec
        unsigned char clock_source;    // 0 = GPS, 1 = PTP
        unsigned char trigger_method;  // 0 = angle based, 1 = time based
        unsigned char return_mode;     // 0..5, see PTC API doc
        unsigned char standby_mode;    // 0 = operation, 1 = standby
        unsigned char motor_status;
        unsigned char vlan_flag;
        unsigned short vlan_id;
        unsigned char clock_data_fmt;
        unsigned char noise_filtering;
        unsigned char reflectivity_mapping;
    };

    /// <summary>
    /// Live status (PTC command 0x09). Matches PTC_COMMAND_GET_LIDAR_STATUS
    /// response payload from the P128 TCP API v1.9 doc (54 bytes total).
    /// </summary>
    struct HesaiLidarStatus {
        unsigned int system_uptime;         // seconds
        unsigned short motor_speed;          // rpm
        float temperature[8];                // 8 channel temperatures, deg C
        unsigned char gps_pps_lock;           // 0 = unlock, 1 = lock
        unsigned char gps_gprmc_status;       // 0 = unlock, 1 = lock
        unsigned int startup_times;
        unsigned int total_operation_time;    // seconds
        unsigned char ptp_clock_status;       // 0 free,1 tracking,2 locked,3 frozen
        float humidity;                       // 0.1 %rh units already applied
    };

    /// <summary>
    /// Queries PTC command 0x07 (inventory info) directly from the LiDAR,
    /// independent of any registered device.
    /// </summary>
    /// <returns>0 on success, -1 on connection/parse error</returns>
    HESAI_API int hesai_get_inventory_info(const char* ipAddress, int ptcPort, HesaiInventoryInfo* outInfo);

    /// <summary>
    /// Queries PTC command 0x08 (config info) directly from the LiDAR.
    /// </summary>
    /// <returns>0 on success, -1 on connection/parse error</returns>
    HESAI_API int hesai_get_config_info(const char* ipAddress, int ptcPort, HesaiConfigInfo* outInfo);

    /// <summary>
    /// Queries PTC command 0x09 (lidar status) directly from the LiDAR.
    /// </summary>
    /// <returns>0 on success, -1 on connection/parse error</returns>
    HESAI_API int hesai_get_lidar_status(const char* ipAddress, int ptcPort, HesaiLidarStatus* outStatus);

    /// <summary>
    /// Queries PTC command 0x05 (angle correction file) directly from the
    /// LiDAR. The response is plain-text CSV, e.g. "Laser id,Elevation,Azimuth".
    /// Caller supplies a buffer; the function copies up to bufferLength bytes
    /// and returns the actual byte count written via outLength (truncated if
    /// the buffer is too small, in which case the return value is -2).
    /// </summary>
    /// <returns>0 on success, -1 on connection error, -2 if buffer too small</returns>
    HESAI_API int hesai_get_correction_info(const char* ipAddress, int ptcPort, char* outBuffer, int bufferLength, int* outLength);

    /// <summary>
    /// Queries PTC commands 0x07, 0x08, 0x09, and 0x05 (inventory, config,
    /// status, and angle correction) over a SINGLE persistent PTC session.
    /// The Pandar128E3X PTC TCP server only accepts one connection at a time
    /// and does not tolerate rapid reconnects between queries; issuing all
    /// manifest queries over one open connection avoids the "invalid input
    /// parameter" (return code 1) failures observed when each query opened
    /// its own short-lived connection.
    /// Each "has*" out-param is set to 1 if the corresponding query
    /// succeeded, 0 otherwise; the query results are only valid when the
    /// matching has* flag is 1. The function itself returns 0 if the
    /// connection was established (regardless of individual query outcomes),
    /// or -1 if the connection could not be opened at all.
    /// </summary>
    HESAI_API int hesai_get_manifest_info(
        const char* ipAddress, int ptcPort,
        HesaiInventoryInfo* outInventory, int* hasInventory,
        HesaiConfigInfo* outConfig, int* hasConfig,
        HesaiLidarStatus* outStatus, int* hasStatus,
        char* correctionBuffer, int correctionBufferLength, int* correctionLength, int* hasCorrection);
}
