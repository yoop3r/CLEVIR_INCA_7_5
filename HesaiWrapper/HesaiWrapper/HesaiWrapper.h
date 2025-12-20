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
    /// Gets statistics for a specific LiDAR device
    /// </summary>
    /// <param name="deviceId">Device identifier (e.g., "10.5.55.14")</param>
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
	/*****************************/
    // ✅ NEW: Device registration functions
    HESAI_API int hesai_register_device(const char* deviceId, const char* ipAddress, int dataPort);
    HESAI_API int hesai_unregister_device(const char* deviceId);

    HESAI_API int hesai_get_device_stats(const char* deviceId, HesaiSdkStats* stats);
    HESAI_API int hesai_reset_device_stats(const char* deviceId);
}