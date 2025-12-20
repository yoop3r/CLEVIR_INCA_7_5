#include "pch.h"
#include "HesaiWrapper.h"
#include <map>
#include <string>
#include <mutex>
#include <chrono>
#include <memory>

// ✅ Include actual Hesai SDK headers
#include "../../HesaiLidar_SDK_2.0-master/driver/hesai_lidar_sdk.hpp"

// ====================================================================
// Device Management
// ====================================================================
struct ManagedDevice {
    std::shared_ptr<hesai::lidar::HesaiLidarSdk<hesai::lidar::LidarPointXYZIRT>> sdk;  // ✅ Changed from Lidar to HesaiLidarSdk
    std::string device_id;
    unsigned long long last_packet_count = 0;
};

static std::map<std::string, ManagedDevice> g_devices;
static std::mutex g_deviceMutex;
static bool g_initialized = false;

// ====================================================================
// Exported Functions
// ====================================================================

extern "C" {

    HESAI_API int hesai_initialize() {
        std::lock_guard<std::mutex> lock(g_deviceMutex);

        if (g_initialized) {
            return 0;  // Already initialized
        }

        // Initialize Hesai SDK (if needed)
        // Note: SDK typically initializes per-device, not globally

        g_initialized = true;
        return 0;
    }

    HESAI_API void hesai_shutdown() {
        std::lock_guard<std::mutex> lock(g_deviceMutex);

        if (!g_initialized) {
            return;
        }

        // Stop all managed devices
        for (auto& pair : g_devices) {
            if (pair.second.sdk) {
                pair.second.sdk->Stop();  // ✅ Now using HesaiLidarSdk::Stop()
            }
        }

        g_devices.clear();
        g_initialized = false;
    }

    /// <summary>
    /// Registers a LiDAR device for statistics tracking
    /// Must be called before get_device_stats
    /// </summary>
    HESAI_API int hesai_register_device(const char* deviceId, const char* ipAddress, int dataPort) {
        if (!deviceId || !ipAddress) {
            return -1;
        }

        std::lock_guard<std::mutex> lock(g_deviceMutex);

        std::string devId(deviceId);

        // Check if already registered
        if (g_devices.find(devId) != g_devices.end()) {
            return 0;  // Already registered
        }

        try {
            // Create Hesai SDK driver configuration
            hesai::lidar::DriverParam param;

            // ✅ CRITICAL FIX: Disable PTC to prevent blocking
            param.input_param.use_ptc_connected = false;  // ← FIXED: Use boolean flag instead of enum
            param.input_param.correction_file_path = "C:\DEV\HesaiLidar_SDK_2.0-master\correction\angle_correction\Pandar128E3X_Angle Correction File.csv";
                param.input_param.firetimes_path = "C:\DEV\HesaiLidar_SDK_2.0-master\correction\firetime_correction\Pandar128E3X_Firetime Correction File.csv";
            // Configure device connection
            param.input_param.device_ip_address = ipAddress;
            param.input_param.ptc_port = 9347;
            param.input_param.udp_port = dataPort;
            param.input_param.multicast_ip_address = "239.192.20.10";
            param.input_param.host_ip_address = "10.5.55.201"; // point cloud destination ip, local ip
            param.input_param.source_type = hesai::lidar::DATA_FROM_LIDAR;

            // ✅ OPTIONAL: Disable correction file download (also blocks)
            //param.decoder_param.enable_correction = false;

            // Configure decoder
            param.decoder_param.enable_parser_thread = true;
            param.decoder_param.enable_udp_thread = true;

            // ✅ Create HesaiLidarSdk instance
            auto sdk = std::make_shared<hesai::lidar::HesaiLidarSdk<hesai::lidar::LidarPointXYZIRT>>();

            // Initialize the device
            if (!sdk->Init(param)) {
                return -1;  // Initialization failed
            }

            // Start the SDK
            sdk->Start();

            // Store in managed devices
            ManagedDevice device;
            device.sdk = sdk;
            device.device_id = devId;
            g_devices[devId] = device;

            return 0;

        }
        catch (const std::exception& ex) {
            // Log error (can't easily log from C++ to VB.NET)
            return -1;
        }
    }

    /// <summary>
    /// Unregisters a device and stops its capture
    /// </summary>
    HESAI_API int hesai_unregister_device(const char* deviceId) {
        if (!deviceId) {
            return -1;
        }

        std::lock_guard<std::mutex> lock(g_deviceMutex);

        std::string devId(deviceId);
        auto it = g_devices.find(devId);

        if (it == g_devices.end()) {
            return -1;  // Not found
        }

        // Stop the SDK
        if (it->second.sdk) {
            it->second.sdk->Stop();  // ✅ Correct method
        }

        g_devices.erase(it);
        return 0;
    }

    HESAI_API int hesai_get_device_stats(const char* deviceId, HesaiSdkStats* stats) {
        if (!deviceId || !stats) {
            return -1;  // Invalid parameters
        }

        std::lock_guard<std::mutex> lock(g_deviceMutex);

        std::string devId(deviceId);
        auto it = g_devices.find(devId);

        if (it == g_devices.end()) {
            // Device not registered - return zeros
            memset(stats, 0, sizeof(HesaiSdkStats));
            return -1;
        }

        try {
            auto& device = it->second;

            if (!device.sdk || !device.sdk->lidar_ptr_) {
                memset(stats, 0, sizeof(HesaiSdkStats));
                return -1;
            }

            // ✅ Access the underlying Lidar object for statistics
            auto lidar = device.sdk->lidar_ptr_;

            // Get packet loss statistics from the parser
            auto parser = lidar->GetGeneralParser();
            if (parser) {
                auto& loss_msg = parser->seqnum_loss_message_;
                stats->packets_received = loss_msg.total_packet_count;
                stats->packets_dropped = loss_msg.total_loss_count;
                stats->total_bytes = 0;  // TODO: Calculate if needed
                stats->checksum_errors = 0;  // TODO: Get from parser if available
                stats->out_of_order_packets = 0;  // TODO: Get from parser if available
                stats->last_packet_timestamp = 0;  // TODO: Get last timestamp
            }
            else {
                memset(stats, 0, sizeof(HesaiSdkStats));
            }

            return 0;

        }
        catch (const std::exception& ex) {
            memset(stats, 0, sizeof(HesaiSdkStats));
            return -1;
        }
    }

    HESAI_API int hesai_reset_device_stats(const char* deviceId) {
        if (!deviceId) {
            return -1;
        }

        std::lock_guard<std::mutex> lock(g_deviceMutex);

        std::string devId(deviceId);
        auto it = g_devices.find(devId);

        if (it == g_devices.end()) {
            return -1;  // Device not found
        }

        try {
            // ✅ Reset statistics in SDK
            if (it->second.sdk && it->second.sdk->lidar_ptr_) {
                auto parser = it->second.sdk->lidar_ptr_->GetGeneralParser();
                if (parser) {
                    parser->seqnum_loss_message_.total_packet_count = 0;
                    parser->seqnum_loss_message_.total_loss_count = 0;
                }
            }

            // Reset local tracking
            it->second.last_packet_count = 0;

            return 0;

        }
        catch (const std::exception& ex) {
            return -1;
        }
    }

} // extern "C"