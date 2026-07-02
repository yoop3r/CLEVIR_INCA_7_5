// Test decoder to find correct byte offsets by brute force
// This will try different combinations and print results

#include <stdint.h>
#include <stdio.h>
#include <math.h>

#ifndef M_PI
#define M_PI 3.1415926535897932384626433832795
#endif

#define RAD2DEG (180.0 / M_PI)

// Sign-extend 24-bit integer to 32-bit
static inline int32_t sign_extend_24(uint32_t value) {
    if (value & 0x800000) {
        return (int32_t)(value | 0xFF000000);
    }
    return (int32_t)value;
}

int main() {
    // Your actual packet bytes (from hex dump)
    uint8_t packet[] = {
        0xE7, 0xFC, 0xB2, 0x81, 0x02, 0x00, 0x40, 0x01, 0x00, 0xCC, 0x80, 0xFE, 0x44, 0x00, 0x00, 0x0D,
        0x00, 0x00, 0x44, 0x00, 0x00, 0x04, 0x55, 0x37, 0xE3, 0x6E, 0x58, 0xF0, 0xFE, 0xE7, 0x3F, 0xF1,
        0x46, 0x17, 0x07, 0x80, 0x57, 0xF7, 0xBF, 0x0C, 0xE4, 0x5B, 0x43, 0x30, 0x00, 0x00, 0x2E, 0x00,
        0x00, 0xE8, 0xFF, 0xFF, 0x6C, 0x2B, 0x00, 0xCC, 0x1A, 0x00, 0xFA, 0xF3, 0xFF, 0xBB, 0x01, 0x01,
        0x01, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x7A
    };
    
    printf("=== BRUTE FORCE BYTE OFFSET SEARCH ===\n");
    printf("Target Altitude: 220.088 m\n");
    printf("Target Heading: 0.334 deg\n");
    printf("Target Pitch: 0.410 deg\n");
    printf("Target Roll: -0.175 deg\n\n");
    
    // Try to find altitude - check ALL 24-bit combinations
    printf("--- Searching for Altitude (any value) ---\n");
    for (int i = 0; i <= 60; i++) {
        int32_t raw = sign_extend_24(packet[i] | (packet[i+1] << 8) | (packet[i+2] << 16));
        double meters = raw * 0.001;
        printf("  [%d-%d]: raw=%d -> %.3f m\n", i, i+2, raw, meters);
    }
    
    printf("\n--- Searching for Heading (all 16-bit values with 1e-6 scale) ---\n");
    for (int i = 0; i <= 66; i++) {
        uint16_t raw = packet[i] | (packet[i+1] << 8);
        double deg = (raw * 1e-6) * RAD2DEG;
        printf("  [%d-%d]: raw=%u -> %.4f deg\n", i, i+1, raw, deg);
    }
    
    return 0;
}
