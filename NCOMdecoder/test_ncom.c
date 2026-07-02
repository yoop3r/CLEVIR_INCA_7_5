// Quick test to verify NCOM byte positions
#include <stdio.h>
#include <stdint.h>

int main() {
    // Sample NCOM packet from your logs (you had working data before)
    // This should show lat=42.96441455, lon=-83.58662616, etc.
    
    uint8_t test_packet[72] = {0xE7}; // Start with sync byte
    
    printf("NCOM Byte Position Reference:\n");
    printf("================================\n");
    printf("Byte 0: Sync (0xE7)\n");
    printf("Bytes 1-2: Time (ms)\n");
    printf("Bytes 3-6: Accelerations\n");
    printf("Bytes 7-9: Latitude (1e-7 deg)\n");
    printf("Bytes 10-12: Longitude (1e-7 deg)\n");
    printf("Bytes 13-15: Altitude (mm)\n");
    printf("Bytes 16-18: North velocity (0.1 mm/s)\n");
    printf("Bytes 19-21: East velocity (0.1 mm/s)\n");
    printf("Bytes 22-24: Down velocity (0.1 mm/s)\n");
    printf("Bytes 25-26: Reserved\n");
    printf("Byte 27: Pitch (0.01 rad)\n");
    printf("Byte 28: Roll (0.01 rad)\n");
    printf("Bytes 29-31: Reserved\n");
    printf("Bytes 32-33: Roll rate (0.01 mrad/s)\n");
    printf("Bytes 34-35: Pitch rate (0.01 mrad/s)\n");
    printf("Bytes 36-37: Yaw rate (0.01 mrad/s)\n");
    printf("Bytes 37-38: Heading (0.1 mrad)\n");
    
    return 0;
}
