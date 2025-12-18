// Modified ptc_tool to dump LiDAR configuration
// Compile this to dump current settings from Hesai Pandar128E3X

#include <cstdio>
#include <cstdlib>
#include <iostream>
#include <fstream>
#include <vector>
#include "hs_com.h"
#include "logger.h"
#include "ptc_client.h"
using namespace hesai::lidar;

int main(int argc, char **argv) {
    if (argc != 3) {
        std::cerr << "Usage: " << argv[0] << " <device_ip_address> <ptc_port>" << std::endl;
        std::cerr << "Example: " << argv[0] << " 10.5.55.14 9347" << std::endl;
        return -1;
    }

    // Initialize logger
    Logger::GetInstance().setLogTargetRule(LOGTARGET::HESAI_LOG_TARGET_CONSOLE);
    Logger::GetInstance().setLogLevelRule(LOGLEVEL::HESAI_LOG_INFO | LOGLEVEL::HESAI_LOG_WARNING | LOGLEVEL::HESAI_LOG_ERROR | LOGLEVEL::HESAI_LOG_FATAL);

    std::string device_ip_address = argv[1];
    int ptc_port = atoi(argv[2]);

    LogInfo("Connecting to LiDAR at %s:%d", device_ip_address.c_str(), ptc_port);

    PtcClient *ptc_client_ = new PtcClient(device_ip_address, ptc_port);

    // Wait for client to be ready
    LogInfo("Waiting for the client to be ready");
    while(ptc_client_->IsOpen() == false) {
        std::this_thread::sleep_for(std::chrono::milliseconds(100));
    }
    LogInfo("Client is ready");

    // ?????????????????????????????????????????????????????????
    // DUMP CONFIGURATION
    // ?????????????????????????????????????????????????????????
    
    std::cout << "\n???????????????????????????????????????????" << std::endl;
    std::cout << " Hesai LiDAR Configuration Dump" << std::endl;
    std::cout << "???????????????????????????????????????????\n" << std::endl;

    // Get LiDAR Calibration (PTC command 0x05)
    {
        u8Array_t dataIn;
        u8Array_t dataOut;
        uint8_t ptc_cmd = 0x05;
        
        LogInfo("Querying LiDAR calibration...");
        int ret = ptc_client_->QueryCommand(dataIn, dataOut, ptc_cmd);
        if (ret == 0) {
            std::cout << "? Calibration File Size: " << dataOut.size() << " bytes" << std::endl;
            
            // Save to file
            std::ofstream calFile("lidar_calibration.bin", std::ios::binary);
            calFile.write((char*)dataOut.data(), dataOut.size());
            calFile.close();
            std::cout << "   Saved to: lidar_calibration.bin" << std::endl;
        } else {
            LogWarning("Failed to get calibration (error: %d)", ptc_client_->ret_code_);
        }
    }

    // Get Device Info (PTC command 0x07)
    {
        u8Array_t dataIn;
        u8Array_t dataOut;
        uint8_t ptc_cmd = 0x07;
        
        LogInfo("Querying device information...");
        int ret = ptc_client_->QueryCommand(dataIn, dataOut, ptc_cmd);
        if (ret == 0 && dataOut.size() > 0) {
            std::cout << "\n?? Device Information:" << std::endl;
            std::cout << "   Raw data size: " << dataOut.size() << " bytes" << std::endl;
            
            // Try to parse as string (some Hesai devices return ASCII)
            std::string devInfo((char*)dataOut.data(), dataOut.size());
            if (devInfo.length() > 0 && std::isprint(devInfo[0])) {
                std::cout << "   " << devInfo << std::endl;
            } else {
                // Dump hex if not printable
                std::cout << "   Hex: ";
                for (size_t i = 0; i < std::min<size_t>(64, dataOut.size()); i++) {
                    printf("%02X ", dataOut[i]);
                }
                std::cout << std::endl;
            }
        } else {
            LogWarning("Failed to get device info (error: %d)", ptc_client_->ret_code_);
        }
    }

    // Get Network Configuration (PTC command varies by model)
    // Try common commands
    std::vector<uint8_t> config_commands = {0x13, 0x15, 0x20, 0x25};
    
    for (auto cmd : config_commands) {
        u8Array_t dataIn;
        u8Array_t dataOut;
        
        LogInfo("Trying config command 0x%02X...", cmd);
        int ret = ptc_client_->QueryCommand(dataIn, dataOut, cmd);
        if (ret == 0 && dataOut.size() > 0) {
            std::cout << "\n??  Configuration (Cmd 0x" << std::hex << (int)cmd << std::dec << "):" << std::endl;
            std::cout << "   Size: " << dataOut.size() << " bytes" << std::endl;
            
            // Dump first 256 bytes as hex
            std::cout << "   Hex dump:" << std::endl;
            for (size_t i = 0; i < std::min<size_t>(256, dataOut.size()); i++) {
                if (i % 16 == 0) std::cout << "   " << std::setw(4) << std::setfill('0') << i << ": ";
                printf("%02X ", dataOut[i]);
                if ((i + 1) % 16 == 0) std::cout << std::endl;
            }
            std::cout << std::endl;
            
            // Save to file
            std::string filename = "lidar_config_0x" + std::to_string(cmd) + ".bin";
            std::ofstream configFile(filename, std::ios::binary);
            configFile.write((char*)dataOut.data(), dataOut.size());
            configFile.close();
            std::cout << "   Saved to: " << filename << std::endl;
        }
    }

    std::cout << "\n???????????????????????????????????????????" << std::endl;
    std::cout << " Configuration dump complete!" << std::endl;
    std::cout << "???????????????????????????????????????????\n" << std::endl;
    
    std::cout << "Next steps:" << std::endl;
    std::cout << "1. Check saved files for configuration details" << std::endl;
    std::cout << "2. Look for PTP settings in binary dumps" << std::endl;
    std::cout << "3. Use Hesai's HesaiLidarTool or web interface for PTP config" << std::endl;

    delete ptc_client_;
    return 0;
}
