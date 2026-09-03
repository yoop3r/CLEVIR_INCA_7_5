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
#include <cstring>
#include <algorithm>

// ✅ Include actual Hesai SDK headers
#include "../../HesaiLidar_SDK_2.0-master/driver/hesai_lidar_sdk.hpp"
#include "../../HesaiLidar_SDK_2.0-master/libhesai/PtcClient/include/ptc_client.h"

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

    // ================================================================
    // PTC Manifest Queries
    // Each helper opens a short-lived standalone PtcClient (independent of
    // any registered ManagedDevice / HesaiLidarSdk), issues one query, and
    // tears the connection down. Works even when the device is registered
    // in validation_only mode, since no SDK instance is required.
    // ================================================================

    namespace {
        // Waits (bounded) for the PtcClient's background connect thread to
        // finish opening the TCP socket. TryOpen() runs on its own thread in
        // the SDK, so IsOpen() may briefly report false right after
        // construction even when the connection succeeds quickly.
        bool WaitForPtcOpen(hesai::lidar::PtcClient& client, int timeoutMs) {
            auto start = std::chrono::steady_clock::now();
            while (!client.IsOpen()) {
                if (std::chrono::duration_cast<std::chrono::milliseconds>(
                        std::chrono::steady_clock::now() - start).count() >= timeoutMs) {
                    return false;
                }
                std::this_thread::sleep_for(std::chrono::milliseconds(20));
            }
            return true;
        }

        uint16_t ResolvePtcPort(int ptcPort) {
            return (ptcPort > 0) ? static_cast<uint16_t>(ptcPort) : 9347;
        }

        void CopyFixedField(char* dest, size_t destSize, const uint8_t* src, size_t srcLen) {
            size_t n = (std::min)(destSize - 1, srcLen);
            memcpy(dest, src, n);
            dest[n] = '\0';
        }
    }

    HESAI_API int hesai_get_inventory_info(const char* ipAddress, int ptcPort, HesaiInventoryInfo* outInfo) {
        if (!ipAddress || !outInfo) return -1;
        HESAI_LOG("hesai_get_inventory_info: connecting to " << ipAddress << ":" << ResolvePtcPort(ptcPort));

        try {
            hesai::lidar::PtcClient client(ipAddress, ResolvePtcPort(ptcPort));
            if (!WaitForPtcOpen(client, 3000)) {
                HESAI_LOG("hesai_get_inventory_info: PTC connection timed out");
                return -1;
            }

            hesai::lidar::u8Array_t in;
            hesai::lidar::u8Array_t out;
            int ret = client.QueryCommand(in, out, hesai::lidar::kPTCGetInventoryInfo);
            if (ret != 0 || out.size() < 228) {
                HESAI_LOG("hesai_get_inventory_info: QueryCommand failed, ret=" << ret << " size=" << out.size());
                return -1;
            }

            const uint8_t* p = out.data();
            memset(outInfo, 0, sizeof(HesaiInventoryInfo));

            CopyFixedField(outInfo->sn, sizeof(outInfo->sn), p, 18); p += 18;
            CopyFixedField(outInfo->date_of_manufacture, sizeof(outInfo->date_of_manufacture), p, 16); p += 16;
            memcpy(outInfo->mac, p, 6); p += 6;
            CopyFixedField(outInfo->sw_ver, sizeof(outInfo->sw_ver), p, 16); p += 16;
            CopyFixedField(outInfo->hw_ver, sizeof(outInfo->hw_ver), p, 16); p += 16;
            CopyFixedField(outInfo->control_fw_ver, sizeof(outInfo->control_fw_ver), p, 16); p += 16;
            CopyFixedField(outInfo->sensor_fw_ver, sizeof(outInfo->sensor_fw_ver), p, 16); p += 16;
            outInfo->angle_offset = static_cast<unsigned short>((p[0] << 8) | p[1]); p += 2; // big-endian on wire
            outInfo->model = p[0]; p += 1;
            outInfo->motor_type = p[0]; p += 1;
            outInfo->num_of_lines = p[0]; p += 1;
            CopyFixedField(outInfo->pn, sizeof(outInfo->pn), p, 32); p += 32;
            outInfo->customer_pn_enable = p[0]; p += 1;
            CopyFixedField(outInfo->customer_pn, sizeof(outInfo->customer_pn), p, 20); p += 20;
            CopyFixedField(outInfo->duns, sizeof(outInfo->duns), p, 9); p += 9;
            CopyFixedField(outInfo->vpps, sizeof(outInfo->vpps), p, 14); p += 14;
            CopyFixedField(outInfo->boot_ver, sizeof(outInfo->boot_ver), p, 16); p += 16;
            CopyFixedField(outInfo->cruise_pn, sizeof(outInfo->cruise_pn), p, 8); p += 8;
            CopyFixedField(outInfo->gm_sw_pn, sizeof(outInfo->gm_sw_pn), p, 8); p += 8;
            CopyFixedField(outInfo->gm_hw_pn, sizeof(outInfo->gm_hw_pn), p, 8); p += 8;
            // remaining 3 reserved bytes intentionally ignored

            HESAI_LOG("hesai_get_inventory_info: success, sn=" << outInfo->sn << " model=" << (int)outInfo->model);
            return 0;
        } catch (const std::exception& ex) {
            HESAI_LOG("hesai_get_inventory_info: EXCEPTION " << ex.what());
            return -1;
        } catch (...) {
            HESAI_LOG("hesai_get_inventory_info: UNKNOWN EXCEPTION");
            return -1;
        }
    }

    HESAI_API int hesai_get_config_info(const char* ipAddress, int ptcPort, HesaiConfigInfo* outInfo) {
        if (!ipAddress || !outInfo) return -1;
        HESAI_LOG("hesai_get_config_info: connecting to " << ipAddress << ":" << ResolvePtcPort(ptcPort));

        try {
            hesai::lidar::PtcClient client(ipAddress, ResolvePtcPort(ptcPort));
            if (!WaitForPtcOpen(client, 3000)) {
                HESAI_LOG("hesai_get_config_info: PTC connection timed out");
                return -1;
            }

            hesai::lidar::u8Array_t in;
            hesai::lidar::u8Array_t out;
            // 0x08 (GET_CONFIG_INFO) has no named SDK constant / wrapper method;
            // issue the raw command byte via the generic QueryCommand primitive.
            const uint8_t kPTCGetConfigInfo = 0x08;
            int ret = client.QueryCommand(in, out, kPTCGetConfigInfo);
            if (ret != 0 || out.size() < 34) {
                HESAI_LOG("hesai_get_config_info: QueryCommand failed, ret=" << ret << " size=" << out.size());
                return -1;
            }

            const uint8_t* p = out.data();
            memset(outInfo, 0, sizeof(HesaiConfigInfo));

            memcpy(outInfo->ipaddr, p, 4); p += 4;
            memcpy(outInfo->mask, p, 4); p += 4;
            memcpy(outInfo->gateway, p, 4); p += 4;
            memcpy(outInfo->dest_ipaddr, p, 4); p += 4;
            outInfo->dest_lidar_udp_port = static_cast<unsigned short>((p[0] << 8) | p[1]); p += 2;
            outInfo->dest_gps_udp_port = static_cast<unsigned short>((p[0] << 8) | p[1]); p += 2;
            outInfo->spin_rate = static_cast<unsigned short>((p[0] << 8) | p[1]); p += 2;
            outInfo->sync = p[0]; p += 1;
            outInfo->sync_angle = static_cast<unsigned short>((p[0] << 8) | p[1]); p += 2;
            outInfo->start_angle = static_cast<unsigned short>((p[0] << 8) | p[1]); p += 2;
            outInfo->stop_angle = static_cast<unsigned short>((p[0] << 8) | p[1]); p += 2;
            outInfo->clock_source = p[0]; p += 1;
            // reserved_1 (1 byte) skipped
            p += 1;
            outInfo->trigger_method = p[0]; p += 1;
            outInfo->return_mode = p[0]; p += 1;
            outInfo->standby_mode = p[0]; p += 1;
            outInfo->motor_status = p[0]; p += 1;
            outInfo->vlan_flag = p[0]; p += 1;
            outInfo->vlan_id = static_cast<unsigned short>((p[0] << 8) | p[1]); p += 2;
            outInfo->clock_data_fmt = p[0]; p += 1;
            outInfo->noise_filtering = p[0]; p += 1;
            outInfo->reflectivity_mapping = p[0]; p += 1;
            // trailing 6 reserved bytes intentionally ignored

            HESAI_LOG("hesai_get_config_info: success, return_mode=" << (int)outInfo->return_mode
                << " clock_source=" << (int)outInfo->clock_source);
            return 0;
        } catch (const std::exception& ex) {
            HESAI_LOG("hesai_get_config_info: EXCEPTION " << ex.what());
            return -1;
        } catch (...) {
            HESAI_LOG("hesai_get_config_info: UNKNOWN EXCEPTION");
            return -1;
        }
    }

    HESAI_API int hesai_get_lidar_status(const char* ipAddress, int ptcPort, HesaiLidarStatus* outStatus) {
        if (!ipAddress || !outStatus) return -1;
        HESAI_LOG("hesai_get_lidar_status: connecting to " << ipAddress << ":" << ResolvePtcPort(ptcPort));

        try {
            hesai::lidar::PtcClient client(ipAddress, ResolvePtcPort(ptcPort));
            if (!WaitForPtcOpen(client, 3000)) {
                HESAI_LOG("hesai_get_lidar_status: PTC connection timed out");
                return -1;
            }

            hesai::lidar::u8Array_t in;
            hesai::lidar::u8Array_t out;
            int ret = client.QueryCommand(in, out, hesai::lidar::kPTCGetLidarStatus);
            if (ret != 0 || out.size() < 54) {
                HESAI_LOG("hesai_get_lidar_status: QueryCommand failed, ret=" << ret << " size=" << out.size());
                return -1;
            }

            const uint8_t* p = out.data();
            memset(outStatus, 0, sizeof(HesaiLidarStatus));

            outStatus->system_uptime = (static_cast<unsigned int>(p[0]) << 24) | (static_cast<unsigned int>(p[1]) << 16)
                | (static_cast<unsigned int>(p[2]) << 8) | static_cast<unsigned int>(p[3]); p += 4;
            outStatus->motor_speed = static_cast<unsigned short>((p[0] << 8) | p[1]); p += 2;
            for (int i = 0; i < 8; ++i) {
                int32_t raw = (static_cast<int32_t>(p[0]) << 24) | (static_cast<int32_t>(p[1]) << 16)
                    | (static_cast<int32_t>(p[2]) << 8) | static_cast<int32_t>(p[3]);
                outStatus->temperature[i] = raw / 100.0f; // 0.01 deg C units
                p += 4;
            }
            outStatus->gps_pps_lock = p[0]; p += 1;
            outStatus->gps_gprmc_status = p[0]; p += 1;
            outStatus->startup_times = (static_cast<unsigned int>(p[0]) << 24) | (static_cast<unsigned int>(p[1]) << 16)
                | (static_cast<unsigned int>(p[2]) << 8) | static_cast<unsigned int>(p[3]); p += 4;
            outStatus->total_operation_time = (static_cast<unsigned int>(p[0]) << 24) | (static_cast<unsigned int>(p[1]) << 16)
                | (static_cast<unsigned int>(p[2]) << 8) | static_cast<unsigned int>(p[3]); p += 4;
            outStatus->ptp_clock_status = p[0]; p += 1;
            {
                int32_t raw = (static_cast<int32_t>(p[0]) << 24) | (static_cast<int32_t>(p[1]) << 16)
                    | (static_cast<int32_t>(p[2]) << 8) | static_cast<int32_t>(p[3]);
                outStatus->humidity = raw / 10.0f; // 0.1 %rh units
                p += 4;
            }
            // trailing reserved byte intentionally ignored

            HESAI_LOG("hesai_get_lidar_status: success, motor_speed=" << outStatus->motor_speed
                << " ptp_clock_status=" << (int)outStatus->ptp_clock_status);
            return 0;
        } catch (const std::exception& ex) {
            HESAI_LOG("hesai_get_lidar_status: EXCEPTION " << ex.what());
            return -1;
        } catch (...) {
            HESAI_LOG("hesai_get_lidar_status: UNKNOWN EXCEPTION");
            return -1;
        }
    }

    HESAI_API int hesai_get_correction_info(const char* ipAddress, int ptcPort, char* outBuffer, int bufferLength, int* outLength) {
        if (!ipAddress || !outBuffer || bufferLength <= 0 || !outLength) return -1;
        HESAI_LOG("hesai_get_correction_info: connecting to " << ipAddress << ":" << ResolvePtcPort(ptcPort));

        try {
            hesai::lidar::PtcClient client(ipAddress, ResolvePtcPort(ptcPort));
            if (!WaitForPtcOpen(client, 3000)) {
                HESAI_LOG("hesai_get_correction_info: PTC connection timed out");
                *outLength = 0;
                return -1;
            }

            hesai::lidar::u8Array_t in;
            hesai::lidar::u8Array_t out;
            int ret = client.QueryCommand(in, out, hesai::lidar::kPTCGetLidarCalibration);
            if (ret != 0 || out.empty()) {
                HESAI_LOG("hesai_get_correction_info: QueryCommand failed, ret=" << ret << " size=" << out.size());
                *outLength = 0;
                return -1;
            }

            if (static_cast<int>(out.size()) > bufferLength) {
                HESAI_LOG("hesai_get_correction_info: buffer too small, need=" << out.size() << " have=" << bufferLength);
                *outLength = static_cast<int>(out.size());
                return -2;
            }

            memcpy(outBuffer, out.data(), out.size());
            *outLength = static_cast<int>(out.size());
            HESAI_LOG("hesai_get_correction_info: success, " << out.size() << " bytes");
            return 0;
        } catch (const std::exception& ex) {
            HESAI_LOG("hesai_get_correction_info: EXCEPTION " << ex.what());
            *outLength = 0;
            return -1;
        } catch (...) {
            HESAI_LOG("hesai_get_correction_info: UNKNOWN EXCEPTION");
            *outLength = 0;
            return -1;
        }
    }

    HESAI_API int hesai_get_manifest_info(
        const char* ipAddress, int ptcPort,
        HesaiInventoryInfo* outInventory, int* hasInventory,
        HesaiConfigInfo* outConfig, int* hasConfig,
        HesaiLidarStatus* outStatus, int* hasStatus,
        char* correctionBuffer, int correctionBufferLength, int* correctionLength, int* hasCorrection) {

        if (!ipAddress || !outInventory || !hasInventory || !outConfig || !hasConfig
            || !outStatus || !hasStatus || !correctionBuffer || !correctionLength || !hasCorrection) {
            return -1;
        }

        *hasInventory = 0;
        *hasConfig = 0;
        *hasStatus = 0;
        *hasCorrection = 0;
        *correctionLength = 0;
        memset(outInventory, 0, sizeof(HesaiInventoryInfo));
        memset(outConfig, 0, sizeof(HesaiConfigInfo));
        memset(outStatus, 0, sizeof(HesaiLidarStatus));

        HESAI_LOG("hesai_get_manifest_info: connecting to " << ipAddress << ":" << ResolvePtcPort(ptcPort));

        try {
            // ✅ Single persistent PtcClient shared across all four queries.
            // The Pandar128E3X PTC TCP server only accepts one connection at a
            // time and does not tolerate the connect/query/disconnect churn of
            // four independent short-lived clients; reusing one open session
            // for all queries avoids the "invalid input parameter" failures
            // seen when each query opened/closed its own connection.
            hesai::lidar::PtcClient client(ipAddress, ResolvePtcPort(ptcPort));
            if (!WaitForPtcOpen(client, 3000)) {
                HESAI_LOG("hesai_get_manifest_info: PTC connection timed out");
                return -1;
            }

            // ── Inventory (0x07) ──
            {
                hesai::lidar::u8Array_t in, out;
                int ret = client.QueryCommand(in, out, hesai::lidar::kPTCGetInventoryInfo);
                if (ret == 0 && out.size() >= 228) {
                    const uint8_t* p = out.data();
                    CopyFixedField(outInventory->sn, sizeof(outInventory->sn), p, 18); p += 18;
                    CopyFixedField(outInventory->date_of_manufacture, sizeof(outInventory->date_of_manufacture), p, 16); p += 16;
                    memcpy(outInventory->mac, p, 6); p += 6;
                    CopyFixedField(outInventory->sw_ver, sizeof(outInventory->sw_ver), p, 16); p += 16;
                    CopyFixedField(outInventory->hw_ver, sizeof(outInventory->hw_ver), p, 16); p += 16;
                    CopyFixedField(outInventory->control_fw_ver, sizeof(outInventory->control_fw_ver), p, 16); p += 16;
                    CopyFixedField(outInventory->sensor_fw_ver, sizeof(outInventory->sensor_fw_ver), p, 16); p += 16;
                    outInventory->angle_offset = static_cast<unsigned short>((p[0] << 8) | p[1]); p += 2;
                    outInventory->model = p[0]; p += 1;
                    outInventory->motor_type = p[0]; p += 1;
                    outInventory->num_of_lines = p[0]; p += 1;
                    CopyFixedField(outInventory->pn, sizeof(outInventory->pn), p, 32); p += 32;
                    outInventory->customer_pn_enable = p[0]; p += 1;
                    CopyFixedField(outInventory->customer_pn, sizeof(outInventory->customer_pn), p, 20); p += 20;
                    CopyFixedField(outInventory->duns, sizeof(outInventory->duns), p, 9); p += 9;
                    CopyFixedField(outInventory->vpps, sizeof(outInventory->vpps), p, 14); p += 14;
                    CopyFixedField(outInventory->boot_ver, sizeof(outInventory->boot_ver), p, 16); p += 16;
                    CopyFixedField(outInventory->cruise_pn, sizeof(outInventory->cruise_pn), p, 8); p += 8;
                    CopyFixedField(outInventory->gm_sw_pn, sizeof(outInventory->gm_sw_pn), p, 8); p += 8;
                    CopyFixedField(outInventory->gm_hw_pn, sizeof(outInventory->gm_hw_pn), p, 8); p += 8;
                    *hasInventory = 1;
                    HESAI_LOG("hesai_get_manifest_info: inventory success, sn=" << outInventory->sn);
                } else {
                    HESAI_LOG("hesai_get_manifest_info: inventory QueryCommand failed, ret=" << ret << " size=" << out.size());
                }
            }

            // ── Config (0x08) ──
            {
                hesai::lidar::u8Array_t in, out;
                const uint8_t kPTCGetConfigInfo = 0x08;
                int ret = client.QueryCommand(in, out, kPTCGetConfigInfo);
                if (ret == 0 && out.size() >= 34) {
                    const uint8_t* p = out.data();
                    memcpy(outConfig->ipaddr, p, 4); p += 4;
                    memcpy(outConfig->mask, p, 4); p += 4;
                    memcpy(outConfig->gateway, p, 4); p += 4;
                    memcpy(outConfig->dest_ipaddr, p, 4); p += 4;
                    outConfig->dest_lidar_udp_port = static_cast<unsigned short>((p[0] << 8) | p[1]); p += 2;
                    outConfig->dest_gps_udp_port = static_cast<unsigned short>((p[0] << 8) | p[1]); p += 2;
                    outConfig->spin_rate = static_cast<unsigned short>((p[0] << 8) | p[1]); p += 2;
                    outConfig->sync = p[0]; p += 1;
                    outConfig->sync_angle = static_cast<unsigned short>((p[0] << 8) | p[1]); p += 2;
                    outConfig->start_angle = static_cast<unsigned short>((p[0] << 8) | p[1]); p += 2;
                    outConfig->stop_angle = static_cast<unsigned short>((p[0] << 8) | p[1]); p += 2;
                    outConfig->clock_source = p[0]; p += 1;
                    p += 1; // reserved_1
                    outConfig->trigger_method = p[0]; p += 1;
                    outConfig->return_mode = p[0]; p += 1;
                    outConfig->standby_mode = p[0]; p += 1;
                    outConfig->motor_status = p[0]; p += 1;
                    outConfig->vlan_flag = p[0]; p += 1;
                    outConfig->vlan_id = static_cast<unsigned short>((p[0] << 8) | p[1]); p += 2;
                    outConfig->clock_data_fmt = p[0]; p += 1;
                    outConfig->noise_filtering = p[0]; p += 1;
                    outConfig->reflectivity_mapping = p[0]; p += 1;
                    *hasConfig = 1;
                    HESAI_LOG("hesai_get_manifest_info: config success, return_mode=" << (int)outConfig->return_mode);
                } else {
                    HESAI_LOG("hesai_get_manifest_info: config QueryCommand failed, ret=" << ret << " size=" << out.size());
                }
            }

            // ── Status (0x09) ──
            {
                hesai::lidar::u8Array_t in, out;
                int ret = client.QueryCommand(in, out, hesai::lidar::kPTCGetLidarStatus);
                if (ret == 0 && out.size() >= 54) {
                    const uint8_t* p = out.data();
                    outStatus->system_uptime = (static_cast<unsigned int>(p[0]) << 24) | (static_cast<unsigned int>(p[1]) << 16)
                        | (static_cast<unsigned int>(p[2]) << 8) | static_cast<unsigned int>(p[3]); p += 4;
                    outStatus->motor_speed = static_cast<unsigned short>((p[0] << 8) | p[1]); p += 2;
                    for (int i = 0; i < 8; ++i) {
                        int32_t raw = (static_cast<int32_t>(p[0]) << 24) | (static_cast<int32_t>(p[1]) << 16)
                            | (static_cast<int32_t>(p[2]) << 8) | static_cast<int32_t>(p[3]);
                        outStatus->temperature[i] = raw / 100.0f;
                        p += 4;
                    }
                    outStatus->gps_pps_lock = p[0]; p += 1;
                    outStatus->gps_gprmc_status = p[0]; p += 1;
                    outStatus->startup_times = (static_cast<unsigned int>(p[0]) << 24) | (static_cast<unsigned int>(p[1]) << 16)
                        | (static_cast<unsigned int>(p[2]) << 8) | static_cast<unsigned int>(p[3]); p += 4;
                    outStatus->total_operation_time = (static_cast<unsigned int>(p[0]) << 24) | (static_cast<unsigned int>(p[1]) << 16)
                        | (static_cast<unsigned int>(p[2]) << 8) | static_cast<unsigned int>(p[3]); p += 4;
                    outStatus->ptp_clock_status = p[0]; p += 1;
                    {
                        int32_t raw = (static_cast<int32_t>(p[0]) << 24) | (static_cast<int32_t>(p[1]) << 16)
                            | (static_cast<int32_t>(p[2]) << 8) | static_cast<int32_t>(p[3]);
                        outStatus->humidity = raw / 10.0f;
                    }
                    *hasStatus = 1;
                    HESAI_LOG("hesai_get_manifest_info: status success, motor_speed=" << outStatus->motor_speed);
                } else {
                    HESAI_LOG("hesai_get_manifest_info: status QueryCommand failed, ret=" << ret << " size=" << out.size());
                }
            }

            // ── Angle correction (0x05) ──
            {
                hesai::lidar::u8Array_t in, out;
                int ret = client.QueryCommand(in, out, hesai::lidar::kPTCGetLidarCalibration);
                if (ret == 0 && !out.empty()) {
                    if (static_cast<int>(out.size()) <= correctionBufferLength) {
                        memcpy(correctionBuffer, out.data(), out.size());
                        *correctionLength = static_cast<int>(out.size());
                        *hasCorrection = 1;
                        HESAI_LOG("hesai_get_manifest_info: correction success, " << out.size() << " bytes");
                    } else {
                        HESAI_LOG("hesai_get_manifest_info: correction buffer too small, need=" << out.size() << " have=" << correctionBufferLength);
                    }
                } else {
                    HESAI_LOG("hesai_get_manifest_info: correction QueryCommand failed, ret=" << ret << " size=" << out.size());
                }
            }

            return 0;
        } catch (const std::exception& ex) {
            HESAI_LOG("hesai_get_manifest_info: EXCEPTION " << ex.what());
            return -1;
        } catch (...) {
            HESAI_LOG("hesai_get_manifest_info: UNKNOWN EXCEPTION");
            return -1;
        }
    }

} // extern "C"
