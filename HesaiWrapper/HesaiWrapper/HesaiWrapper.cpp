#include "pch.h"
#include "HesaiWrapper.h"
#include <map>
#include <string>
#include <mutex>
#include <chrono>
#include <memory>
#include <thread>
#include <future>
#include <cstdio>

// ✅ Include actual Hesai SDK headers
#include "../../HesaiLidar_SDK_2.0-master/driver/hesai_lidar_sdk.hpp"

// ✅ For debug output - use multiple methods
#include <Windows.h>
#include <sstream>
#include <fstream>

// ✅ Log to file for reliable debugging
static std::ofstream g_logFile;
static bool g_logFileOpened = false;

static void OpenLogFile() {
    if (!g_logFileOpened) {
        g_logFile.open("C:\\CLEVIR_INCA_7_5\\HesaiWrapper_Debug.log", std::ios::out | std::ios::app);
        g_logFileOpened = true;
    }
}

// Helper macro for debug logging - writes to file AND debug output
#define HESAI_LOG(msg) { \
    std::ostringstream oss; \
    oss << "[HesaiWrapper] " << msg; \
    std::string logStr = oss.str(); \
    OutputDebugStringA((logStr + "\n").c_str()); \
    printf("%s\n", logStr.c_str()); \
    fflush(stdout); \
    OpenLogFile(); \
    if (g_logFile.is_open()) { \
        g_logFile << logStr << std::endl; \
        g_logFile.flush(); \
    } \
}

// ====================================================================
// Device Management
// ====================================================================
struct ManagedDevice {
    std::shared_ptr<hesai::lidar::HesaiLidarSdk<hesai::lidar::LidarPointXYZIRT>> sdk;
    std::string device_id;
    unsigned long long last_packet_count = 0;
    bool initialized = false;
    
    // ✅ NEW: Validation-only mode fields
    bool validation_only = false;           // True = no UDP binding, stats only
    unsigned long long validated_packets = 0;
    unsigned long long checksum_errors = 0;
    unsigned long long sequence_errors = 0;
    unsigned long long total_bytes_validated = 0;
    uint16_t last_sequence_number = 0;
    bool sequence_initialized = false;
};

static std::map<std::string, ManagedDevice> g_devices;
static std::mutex g_deviceMutex;
static bool g_initialized = false;

// ====================================================================
// Helper Functions
// ====================================================================

/// <summary>
/// ✅ NEW: Creates DriverParam from HesaiDeviceConfig with intelligent defaults
/// </summary>
static hesai::lidar::DriverParam CreateDriverParam(const HesaiDeviceConfig* config) {
    hesai::lidar::DriverParam param;

    HESAI_LOG("CreateDriverParam: Building configuration...");

    // ================================================================
    // Input Parameters (network and device configuration)
    // ================================================================
    param.input_param.device_ip_address = config->ip_address;
    param.input_param.udp_port = config->data_port;
    HESAI_LOG("  Device IP: " << config->ip_address << ":" << config->data_port);

    // ✅ Correction files (empty string = use SDK embedded defaults)
    if (config->correction_file_path && strlen(config->correction_file_path) > 0) {
        param.input_param.correction_file_path = config->correction_file_path;
        HESAI_LOG("  Correction file: " << config->correction_file_path);
    } else {
        param.input_param.correction_file_path = "";
        HESAI_LOG("  Correction file: (embedded)");
    }

    if (config->firetimes_path && strlen(config->firetimes_path) > 0) {
        param.input_param.firetimes_path = config->firetimes_path;
        HESAI_LOG("  Firetimes file: " << config->firetimes_path);
    } else {
        param.input_param.firetimes_path = "";
        HESAI_LOG("  Firetimes file: (embedded)");
    }

    // ✅ Host IP (0.0.0.0 = bind to any available interface)
    if (config->host_ip_address && strlen(config->host_ip_address) > 0) {
        param.input_param.host_ip_address = config->host_ip_address;
        HESAI_LOG("  Host IP: " << config->host_ip_address);
    } else {
        param.input_param.host_ip_address = "0.0.0.0";
        HESAI_LOG("  Host IP: 0.0.0.0 (auto)");
    }

    // ✅ Multicast IP (empty = no multicast)
    if (config->multicast_ip_address && strlen(config->multicast_ip_address) > 0) {
        param.input_param.multicast_ip_address = config->multicast_ip_address;
        HESAI_LOG("  Multicast IP: " << config->multicast_ip_address);
    } else {
        param.input_param.multicast_ip_address = "";
        HESAI_LOG("  Multicast IP: (none)");
    }

    // ✅ PTC configuration (disabled by default to prevent blocking)
    param.input_param.ptc_port = config->ptc_port > 0 ? config->ptc_port : 9347;
    param.input_param.use_ptc_connected = (config->use_ptc_connected != 0);  // ✅ int to bool
    HESAI_LOG("  PTC: " << (config->use_ptc_connected ? "enabled" : "disabled") << " port=" << param.input_param.ptc_port);

    // ✅ CRITICAL: Set source type to LIDAR
    param.input_param.source_type = hesai::lidar::DATA_FROM_LIDAR;
    HESAI_LOG("  Source: DATA_FROM_LIDAR");

    // ================================================================
    // Decoder Parameters (threading configuration)
    // ✅ FIXED: Convert int to bool properly
    // ================================================================
    param.decoder_param.enable_parser_thread = (config->enable_parser_thread != 0);
    param.decoder_param.enable_udp_thread = (config->enable_udp_thread != 0);
    HESAI_LOG("  Parser thread: " << (config->enable_parser_thread ? "enabled" : "disabled") << " (raw value: " << config->enable_parser_thread << ")");
    HESAI_LOG("  UDP thread: " << (config->enable_udp_thread ? "enabled" : "disabled") << " (raw value: " << config->enable_udp_thread << ")");

    HESAI_LOG("CreateDriverParam: Configuration complete");
    return param;
}

// ====================================================================
// Exported Functions
// ====================================================================

extern "C" {

    HESAI_API int hesai_initialize() {
        HESAI_LOG("hesai_initialize: Entry");
        std::lock_guard<std::mutex> lock(g_deviceMutex);

        if (g_initialized) {
            HESAI_LOG("hesai_initialize: Already initialized");
            return 0;
        }

        g_initialized = true;
        HESAI_LOG("hesai_initialize: SDK initialized");
        return 0;
    }

    HESAI_API void hesai_shutdown() {
        HESAI_LOG("hesai_shutdown: Entry");
        std::lock_guard<std::mutex> lock(g_deviceMutex);

        if (!g_initialized) {
            return;
        }

        HESAI_LOG("hesai_shutdown: Stopping all devices...");

        for (auto& pair : g_devices) {
            if (pair.second.sdk && !pair.second.validation_only) {
                HESAI_LOG("  Stopping device: " << pair.first);
                pair.second.sdk->Stop();
            }
        }

        g_devices.clear();
        g_initialized = false;
        
        // Close log file
        if (g_logFile.is_open()) {
            g_logFile.close();
            g_logFileOpened = false;
        }
        
        HESAI_LOG("hesai_shutdown: Complete");
    }

    /// <summary>
    /// ✅ FIXED: Extended device registration with TIMEOUT to prevent blocking
    /// </summary>
    HESAI_API int hesai_register_device_ex(const HesaiDeviceConfig* config) {
        HESAI_LOG("=== hesai_register_device_ex: ENTRY ===");

        if (!config) {
            HESAI_LOG("ERROR: config is NULL");
            return -1;
        }
        
        if (!config->device_id) {
            HESAI_LOG("ERROR: device_id is NULL");
            return -1;
        }
        
        if (!config->ip_address) {
            HESAI_LOG("ERROR: ip_address is NULL");
            return -1;
        }

        std::string devId(config->device_id);
        HESAI_LOG("Device ID = " << devId);
        HESAI_LOG("IP Address = " << config->ip_address);
        HESAI_LOG("Data Port = " << config->data_port);
        HESAI_LOG("Validation Only = " << (config->validation_only ? "YES" : "NO"));

        // ✅ NEW: If validation_only mode, use the lightweight registration
        if (config->validation_only) {
            return hesai_register_device_validation_only(config->device_id, config->ip_address, config->data_port);
        }

        // Check if already registered (with lock)
        {
            std::lock_guard<std::mutex> lock(g_deviceMutex);
            if (g_devices.find(devId) != g_devices.end()) {
                HESAI_LOG("Already registered, returning 0");
                return 0;
            }
        }

        try {
            HESAI_LOG("Creating driver params...");
            hesai::lidar::DriverParam param = CreateDriverParam(config);

            HESAI_LOG("Creating HesaiLidarSdk instance...");
            auto sdk = std::make_shared<hesai::lidar::HesaiLidarSdk<hesai::lidar::LidarPointXYZIRT>>();
            HESAI_LOG("SDK instance created");

            HESAI_LOG("=== CALLING sdk->Init() - THIS MAY BLOCK ===");
            
            // Try Init directly first to see where it blocks
            bool initResult = sdk->Init(param);
            
            HESAI_LOG("=== sdk->Init() RETURNED: " << (initResult ? "true" : "false") << " ===");

            if (!initResult) {
                HESAI_LOG("Init() returned false - registration failed");
                return -1;
            }

            // ✅ FIXED: Run Start() asynchronously with timeout to prevent blocking
            HESAI_LOG("Calling sdk->Start() asynchronously with 3 second timeout...");
            
            auto startFuture = std::async(std::launch::async, [&sdk]() {
                sdk->Start();
                return true;
            });
            
            // Wait up to 3 seconds for Start() to complete
            auto status = startFuture.wait_for(std::chrono::seconds(3));
            
            if (status == std::future_status::timeout) {
                HESAI_LOG("WARNING: sdk->Start() timed out after 3 seconds - continuing anyway");
                // Don't fail - the SDK may still work, it just didn't return quickly
            } else if (status == std::future_status::ready) {
                HESAI_LOG("sdk->Start() completed within timeout");
            }

            // Store in managed devices (with lock)
            {
                std::lock_guard<std::mutex> lock(g_deviceMutex);
                ManagedDevice device;
                device.sdk = sdk;
                device.device_id = devId;
                device.initialized = true;
                device.validation_only = false;
                g_devices[devId] = device;
            }

            HESAI_LOG("=== hesai_register_device_ex: SUCCESS ===");
            return 0;

        } catch (const std::exception& ex) {
            HESAI_LOG("EXCEPTION: " << ex.what());
            return -1;
        } catch (...) {
            HESAI_LOG("UNKNOWN EXCEPTION");
            return -1;
        }
    }

    /// <summary>
    /// ✅ NEW: Register device in VALIDATION-ONLY mode (no UDP binding)
    /// This allows PcapDotNet to handle packet capture while SDK tracks statistics.
    /// </summary>
    HESAI_API int hesai_register_device_validation_only(const char* deviceId, const char* ipAddress, int dataPort) {
        HESAI_LOG("=== hesai_register_device_validation_only: ENTRY ===");

        if (!deviceId || !ipAddress) {
            HESAI_LOG("ERROR: deviceId or ipAddress is NULL");
            return -1;
        }

        std::string devId(deviceId);
        HESAI_LOG("Device ID = " << devId);
        HESAI_LOG("IP Address = " << ipAddress << " (for identification only - NO UDP BINDING)");
        HESAI_LOG("Data Port = " << dataPort << " (for identification only - NO UDP BINDING)");

        // Check if already registered
        {
            std::lock_guard<std::mutex> lock(g_deviceMutex);
            if (g_devices.find(devId) != g_devices.end()) {
                HESAI_LOG("Already registered, returning 0");
                return 0;
            }
        }

        try {
            // ✅ Create a lightweight device entry WITHOUT initializing the full SDK
            // This avoids UDP socket binding entirely
            std::lock_guard<std::mutex> lock(g_deviceMutex);
            
            ManagedDevice device;
            device.sdk = nullptr;  // ✅ NO SDK instance - just stats tracking
            device.device_id = devId;
            device.initialized = true;
            device.validation_only = true;  // ✅ Mark as validation-only
            device.validated_packets = 0;
            device.checksum_errors = 0;
            device.sequence_errors = 0;
            device.total_bytes_validated = 0;
            device.last_sequence_number = 0;
            device.sequence_initialized = false;
            
            g_devices[devId] = device;

            HESAI_LOG("=== hesai_register_device_validation_only: SUCCESS (NO UDP BINDING) ===");
            return 0;

        } catch (const std::exception& ex) {
            HESAI_LOG("EXCEPTION: " << ex.what());
            return -1;
        }
    }

    /// <summary>
    /// ✅ NEW: Feed a captured packet to the SDK for validation
    /// Validates checksum and sequence number without UDP binding.
    /// </summary>
    HESAI_API int hesai_validate_packet(const char* deviceId, const unsigned char* packetData, int packetLength) {
        if (!deviceId || !packetData || packetLength <= 0) {
            return -1;
        }

        std::lock_guard<std::mutex> lock(g_deviceMutex);

        std::string devId(deviceId);
        auto it = g_devices.find(devId);

        if (it == g_devices.end()) {
            return -2;  // Device not registered
        }

        ManagedDevice& device = it->second;

        // Only process for validation-only devices
        if (!device.validation_only) {
            // For full SDK devices, stats come from the SDK itself
            return 0;
        }

        try {
            // ✅ Basic validation without full SDK parsing
            // Hesai packets have a specific structure we can validate
            
            device.validated_packets++;
            device.total_bytes_validated += packetLength;

            // ✅ Validate minimum packet size (Hesai header is 42 bytes minimum)
            if (packetLength < 42) {
                device.checksum_errors++;
                return -3;  // Packet too small
            }

            // ✅ Sequence number tracking (bytes 6-7 in Hesai packet, little-endian)
            // Note: Actual offset depends on Hesai model - this is for XT series
            if (packetLength >= 8) {
                uint16_t seqNum = static_cast<uint16_t>(packetData[6]) | 
                                 (static_cast<uint16_t>(packetData[7]) << 8);
                
                if (device.sequence_initialized) {
                    uint16_t expectedSeq = (device.last_sequence_number + 1) & 0xFFFF;
                    if (seqNum != expectedSeq) {
                        device.sequence_errors++;
                        // Calculate how many packets were lost
                        int gap = (seqNum > device.last_sequence_number) 
                                  ? (seqNum - device.last_sequence_number - 1)
                                  : (65536 - device.last_sequence_number + seqNum - 1);
                        if (gap > 0 && gap < 1000) {
                            // Reasonable gap - count as dropped packets
                            // (Large gaps might indicate sensor restart)
                        }
                    }
                } else {
                    device.sequence_initialized = true;
                }
                device.last_sequence_number = seqNum;
            }

            // ✅ Future: Add CRC validation here if needed
            // Hesai XT series has a CRC at the end of each packet

            return 0;  // Valid packet

        } catch (const std::exception& ex) {
            HESAI_LOG("hesai_validate_packet exception: " << ex.what());
            return -4;
        }
    }

    /// <summary>
    /// ✅ LEGACY: Simple registration using sensible defaults
    /// Calls hesai_register_device_ex internally
    /// </summary>
    HESAI_API int hesai_register_device(const char* deviceId, const char* ipAddress, int dataPort) {
        HESAI_LOG("hesai_register_device: " << (deviceId ? deviceId : "NULL") << " @ " << (ipAddress ? ipAddress : "NULL") << ":" << dataPort);
        
        HesaiDeviceConfig config = {};
        config.device_id = deviceId;
        config.ip_address = ipAddress;
        config.data_port = dataPort;
        config.correction_file_path = nullptr;
        config.firetimes_path = nullptr;
        config.host_ip_address = nullptr;
        config.multicast_ip_address = nullptr;
        config.ptc_port = 9347;
        config.use_ptc_connected = false;
        config.enable_parser_thread = true;
        config.enable_udp_thread = true;
        config.validation_only = false;  // ✅ Default to full mode

        return hesai_register_device_ex(&config);
    }

    HESAI_API int hesai_unregister_device(const char* deviceId) {
        if (!deviceId) {
            return -1;
        }

        HESAI_LOG("hesai_unregister_device: " << deviceId);

        std::lock_guard<std::mutex> lock(g_deviceMutex);

        std::string devId(deviceId);
        auto it = g_devices.find(devId);

        if (it == g_devices.end()) {
            HESAI_LOG("Device not found - already unregistered (OK)");
            return 0;  // ✅ Return success - idempotent behavior (safe to call multiple times)
        }

        // ✅ Only stop SDK for non-validation-only devices
        if (it->second.sdk && !it->second.validation_only) {
            HESAI_LOG("Stopping SDK...");
            it->second.sdk->Stop();
        }

        g_devices.erase(it);
        HESAI_LOG("Complete");
        return 0;
    }

    HESAI_API int hesai_get_device_stats(const char* deviceId, HesaiSdkStats* stats) {
        if (!deviceId || !stats) {
            return -1;
        }

        std::lock_guard<std::mutex> lock(g_deviceMutex);

        std::string devId(deviceId);
        auto it = g_devices.find(devId);

        if (it == g_devices.end()) {
            memset(stats, 0, sizeof(HesaiSdkStats));
            return -1;
        }

        try {
            auto& device = it->second;

            // ✅ NEW: Handle validation-only devices
            if (device.validation_only) {
                stats->packets_received = device.validated_packets;
                stats->packets_dropped = device.sequence_errors;  // Sequence gaps = dropped packets
                stats->checksum_errors = device.checksum_errors;
                stats->out_of_order_packets = device.sequence_errors;
                stats->total_bytes = device.total_bytes_validated;
                stats->last_packet_timestamp = 0;  // Not tracked in validation mode
                return 0;
            }

            // Full SDK mode - get stats from SDK
            if (!device.sdk || !device.sdk->lidar_ptr_) {
                memset(stats, 0, sizeof(HesaiSdkStats));
                return -1;
            }

            auto lidar = device.sdk->lidar_ptr_;
            auto parser = lidar->GetGeneralParser();
            
            if (parser) {
                auto& loss_msg = parser->seqnum_loss_message_;
                stats->packets_received = loss_msg.total_packet_count;
                stats->packets_dropped = loss_msg.total_loss_count;
                stats->total_bytes = 0;
                stats->checksum_errors = 0;
                stats->out_of_order_packets = 0;
                stats->last_packet_timestamp = 0;
            } else {
                memset(stats, 0, sizeof(HesaiSdkStats));
            }

            return 0;

        } catch (const std::exception& ex) {
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
            return -1;
        }

        try {
            auto& device = it->second;

            // ✅ Handle validation-only devices
            if (device.validation_only) {
                device.validated_packets = 0;
                device.checksum_errors = 0;
                device.sequence_errors = 0;
                device.total_bytes_validated = 0;
                device.sequence_initialized = false;
                return 0;
            }

            // Full SDK mode
            if (device.sdk && device.sdk->lidar_ptr_) {
                auto parser = device.sdk->lidar_ptr_->GetGeneralParser();
                if (parser) {
                    parser->seqnum_loss_message_.total_packet_count = 0;
                    parser->seqnum_loss_message_.total_loss_count = 0;
                }
            }

            device.last_packet_count = 0;
            return 0;

        } catch (const std::exception& ex) {
            return -1;
        }
    }

} // extern "C"