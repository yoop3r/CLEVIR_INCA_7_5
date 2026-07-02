// Simple NCOM decoder for VB.NET P/Invoke
// Based on OXTS NCOM Manual Rev 250811, Table 6 (Batch B definition)
// Reference: OXTS RT3000 NCOM Manual

#include <stdint.h>
#include <math.h>
#include <string.h>
#include <stdio.h>

#ifndef M_PI
#define M_PI 3.1415926535897932384626433832795
#endif

#define RAD2DEG (180.0 / M_PI)

// Windows DLL export
#ifdef _WIN32
    #ifdef NCOM_DECODE_DLL_EXPORT
        #define NCOM_API __declspec(dllexport)
    #else
        #define NCOM_API __declspec(dllimport)
    #endif
#else
    #define NCOM_API
#endif

// NCOM data structure
typedef struct {
    double latitude;          // degrees
    double longitude;         // degrees
    double altitude;          // meters
    double heading;           // degrees (0-360)
    double pitch;             // degrees (-90 to +90)
    double roll;              // degrees (-180 to +180)
    
    // GPS Time
    int gps_week;             // GPS week number (from status channel)
    double gps_time_of_week;  // seconds within minute (0-59.999)
    
    // Velocities (m/s, NED frame)
    double velocity_north;
    double velocity_east;
    double velocity_down;
    
    // Angular rates (rad/s, body frame)
    double roll_rate;         // Wx (about longitudinal axis)
    double pitch_rate;        // Wy (about lateral axis)
    double yaw_rate;          // Wz (about vertical axis)
    
    int navigation_status;    // Status byte
    int is_valid;             // 1 if decode successful
} NcomData;

// Sign-extend 24-bit integer to 32-bit
static inline int32_t sign_extend_24(uint32_t value) {
    if (value & 0x800000) {
        return (int32_t)(value | 0xFF000000);
    }
    return (int32_t)value;
}

// Helper function to cast 8 bytes to double (little-endian)
static inline double cast_8_byte_LE_to_double(const uint8_t *b) {
    union {
        uint64_t i;
        double d;
    } u;
    u.i = ((uint64_t)b[0]) | ((uint64_t)b[1] << 8) | ((uint64_t)b[2] << 16) | ((uint64_t)b[3] << 24) |
          ((uint64_t)b[4] << 32) | ((uint64_t)b[5] << 40) | ((uint64_t)b[6] << 48) | ((uint64_t)b[7] << 56);
    return u.d;
}

// Helper function to cast 4 bytes to float (little-endian)
static inline float cast_4_byte_LE_to_float(const uint8_t *b) {
    union {
        uint32_t i;
        float f;
    } u;
    u.i = ((uint32_t)b[0]) | ((uint32_t)b[1] << 8) | ((uint32_t)b[2] << 16) | ((uint32_t)b[3] << 24);
    return u.f;
}

// Main decoding function - OFFICIAL OXTS NCOM STRUCTURE-A
NCOM_API int NcomDecodePacket(const uint8_t *packet, int length, NcomData *output) {
    if (!packet || !output || length < 72) {
        return 0; // Invalid input
    }
    
    // Check sync byte
    if (packet[0] != 0xE7) {
        return 0; // Not a valid NCOM packet
    }
    
    // Check navigation status to ensure Structure-A
    uint8_t nav_status = packet[21];
    if (nav_status == 11) {
        return 0; // Structure-B packet, ignore
    }
    
    memset(output, 0, sizeof(NcomData));
    
    // === BATCH A: INERTIAL OUTPUT (Bytes 1-20) ===
    // Time (bytes 1-2): milliseconds into current GPS minute
    uint16_t ms_in_minute = packet[1] | (packet[2] << 8);
    output->gps_time_of_week = ms_in_minute * 0.001; // Convert to seconds
    
    // Accelerations (bytes 3-11) - skip for now, not needed
    
    // Angular rates (bytes 12-20) - OFFICIAL: signed Word (24-bit), scale 1×10?? rad/s
    int32_t wx_raw = sign_extend_24(packet[12] | (packet[13] << 8) | (packet[14] << 16));
    output->roll_rate = wx_raw * 1e-5;
    
    int32_t wy_raw = sign_extend_24(packet[15] | (packet[16] << 8) | (packet[17] << 16));
    output->pitch_rate = wy_raw * 1e-5;
    
    int32_t wz_raw = sign_extend_24(packet[18] | (packet[19] << 8) | (packet[20] << 16));
    output->yaw_rate = wz_raw * 1e-5;
    
    // === NAVIGATION STATUS (Byte 21) ===
    output->navigation_status = nav_status;
    
    // === BATCH B: POSITION, VELOCITY, ORIENTATION (Bytes 23-60) ===
    // Per OXTS Manual Table 6:
    
    // Latitude (bytes 23-30): Double in radians
    double lat_rad = cast_8_byte_LE_to_double(&packet[23]);
    output->latitude = lat_rad * RAD2DEG; // Convert to degrees
    
    // Longitude (bytes 31-38): Double in radians
    double lon_rad = cast_8_byte_LE_to_double(&packet[31]);
    output->longitude = lon_rad * RAD2DEG; // Convert to degrees
    
    // Altitude (bytes 39-42): Float in meters
    output->altitude = cast_4_byte_LE_to_float(&packet[39]);
    
    // North Velocity (bytes 43-45): signed Word (24-bit), scale 1×10?? m/s
    int32_t vn_raw = sign_extend_24(packet[43] | (packet[44] << 8) | (packet[45] << 16));
    output->velocity_north = vn_raw * 1e-4;
    
    // East Velocity (bytes 46-48): signed Word (24-bit), scale 1×10?? m/s
    int32_t ve_raw = sign_extend_24(packet[46] | (packet[47] << 8) | (packet[48] << 16));
    output->velocity_east = ve_raw * 1e-4;
    
    // Down Velocity (bytes 49-51): signed Word (24-bit), scale 1×10?? m/s
    int32_t vd_raw = sign_extend_24(packet[49] | (packet[50] << 8) | (packet[51] << 16));
    output->velocity_down = vd_raw * 1e-4;
    
    // Heading (bytes 52-54): signed Word (24-bit), scale 1×10?? radians
    int32_t hdg_raw = sign_extend_24(packet[52] | (packet[53] << 8) | (packet[54] << 16));
    double heading_rad = hdg_raw * 1e-6;
    output->heading = heading_rad * RAD2DEG;
    
    // Normalize heading to [0, 360)
    while (output->heading < 0.0) output->heading += 360.0;
    while (output->heading >= 360.0) output->heading -= 360.0;
    
    // Pitch (bytes 55-57): signed Word (24-bit), scale 1×10?? radians
    int32_t pitch_raw = sign_extend_24(packet[55] | (packet[56] << 8) | (packet[57] << 16));
    double pitch_rad = pitch_raw * 1e-6;
    output->pitch = pitch_rad * RAD2DEG;
    
    // Roll (bytes 58-60): signed Word (24-bit), scale 1×10?? radians
    int32_t roll_raw = sign_extend_24(packet[58] | (packet[59] << 8) | (packet[60] << 16));
    double roll_rad = roll_raw * 1e-6;
    output->roll = roll_rad * RAD2DEG;
    
    output->is_valid = 1;
    return 1; // Success
}
