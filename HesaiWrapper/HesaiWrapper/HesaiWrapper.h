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
}