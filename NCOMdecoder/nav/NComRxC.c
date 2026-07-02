//============================================================================================================
//!
//! The software is protected by copyright of Oxford Technical Solutions at oxts.com.
//! © 2008 - 2017, Oxford Technical Solutions Ltd.
//! Unauthorised use, copying or distribution is not permitted.
//! 
//! Permission is hereby granted, free of charge, to any person obtaining a copy of this software and 
//! associated documentation files (the "Software"), to deal in the Software without restriction, including 
//! without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell 
//! copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the 
//! following conditions:
//!
//! All copies or substantial portions of the software must reproduce the above copyright notices, this list 
//! of conditions and the following disclaimer in the software documentation and/or other materials provided
//! with the distribution.
//!
//! The software is provided by the copyright holders "as is" without any warranty of any kind, express or 
//! implied, including, but not limited to, warranties of merchantability or fitness for a particular purpose.
//! In no event shall the copyright holders be liable for any direct, indirect, incidental, special,
//! exemplary, or consequential damages however caused and on any liability, whether in contract, strict
//! liability, or tort (including negligence or otherwise) arising in any way out of the use of this software.
//!
//!
//! \file NComRxC.c
//!
//! \brief NCom C decoder.
//!
//============================================================================================================




//############################################################################################################
//##                                                                                                        ##
//##  Includes and Definitions                                                                              ##
//##                                                                                                        ##
//############################################################################################################


//============================================================================================================
// Includes.

#include <math.h>
#include <stdlib.h>
#include <stdio.h>
#include <string.h>
#include <stdarg.h>
#include "NComRxC.h"


//============================================================================================================
// Definitions.

// General constants.
#define NOUTPUT_PACKET_LENGTH  (72)               //!< NCom packet length.
#define NCOM_SYNC           (0xE7)                //!< NCom sync byte.
#define PKT_PERIOD          (0.01)                //!< 10ms updates.
#define TIME2SEC            (1e-3)                //!< Units of 1 ms.
#define FINETIME2SEC        (4e-6)                //!< Units of 4 us.
#define TIMECYCLE           (60000)               //!< Units of TIME2SEC (i.e. 60 seconds).
#define WEEK2CYCLES         (10080)               //!< Time cycles in a week.
#define ACC2MPS2            (1e-4)                //!< Units of 0.1 mm/s^2.
#define RATE2RPS            (1e-5)                //!< Units of 0.01 mrad/s.
#define VEL2MPS             (1e-4)                //!< Units of 0.1 mm/s.
#define ANG2RAD             (1e-6)                //!< Units of 0.001 mrad.
#define INNFACTOR           (0.1)                 //!< Resolution of 0.1.
#define POSA2M              (1e-3)                //!< Units of 1 mm.
#define VELA2MPS            (1e-3)                //!< Units of 1 mm/s.
#define ANGA2RAD            (1e-5)                //!< Units of 0.01 mrad.
#define GB2RPS              (5e-6)                //!< Units of 0.005 mrad/s.
#define AB2MPS2             (1e-4)                //!< Units of 0.1 mm/s^2.
#define GSFACTOR            (1e-6)                //!< Units of 1 ppm.
#define ASFACTOR            (1e-6)                //!< Units of 1 ppm.
#define GBA2RPS             (1e-6)                //!< Units of 0.001 mrad/s.
#define ABA2MPS2            (1e-5)                //!< Units of 0.01 mm/s^2.
#define GSAFACTOR           (1e-6)                //!< Units of 1 ppm.
#define ASAFACTOR           (1e-6)                //!< Units of 1 ppm.
#define GPSPOS2M            (1e-3)                //!< Units of 1 mm.
#define GPSATT2RAD          (1e-4)                //!< Units of 0.1 mrad.
#define GPSPOSA2M           (1e-4)                //!< Units of 0.1 mm.
#define GPSATTA2RAD         (1e-5)                //!< Units of 0.01 mrad.
#define INNFACTOR           (0.1)                 //!< Resolution of 0.1.
#define DIFFAGE2SEC         (1e-2)                //!< Units of 0.01 s.
#define REFPOS2M            (0.0012)              //!< Units of 1.2 mm.
#define REFANG2RAD          (1e-4)                //!< Units of 0.1 mrad.
#define OUTPOS2M            (1e-3)                //!< Units of 1 mm.
#define ZVPOS2M             (1e-3)                //!< Units of 1 mm.
#define ZVPOSA2M            (1e-4)                //!< Units of 0.1 mm.
#define NSPOS2M             (1e-3)                //!< Units of 1 mm.
#define NSPOSA2M            (1e-4)                //!< Units of 0.1 mm.
#define ALIGN2RAD           (1e-4)                //!< Units of 0.1 mrad.
#define ALIGNA2RAD          (1e-5)                //!< Units of 0.01 mrad.
#define SZVDELAY2S          (1.0)                 //!< Units of 1.0 s.
#define SZVPERIOD2S         (0.1)                 //!< Units of 0.1 s.
#define TOPSPEED2MPS        (0.5)                 //!< Units of 0.5 m/s.
#define NSDELAY2S           (0.1)                 //!< Units of 0.1 s.
#define NSPERIOD2S          (0.02)                //!< Units of 0.02 s.
#define NSACCEL2MPS2        (0.04)                //!< Units of 0.04 m/s^2.
#define NSSPEED2MPS         (0.1)                 //!< Units of 0.1 m/s.
#define NSRADIUS2M          (0.5)                 //!< Units of 0.5 m.
#define INITSPEED2MPS       (0.1)                 //!< Units of 0.1 m/s.
#define HLDELAY2S           (1.0)                 //!< Units of 1.0 s.
#define HLPERIOD2S          (0.1)                 //!< Units of 0.1 s.
#define STATDELAY2S         (1.0)                 //!< Units of 1.0 s.
#define STATSPEED2MPS       (0.01)                //!< Units of 1.0 cm/s.
#define WSPOS2M             (1e-3)                //!< Units of 1 mm.
#define WSPOSA2M            (1e-4)                //!< Units of 0.1 mm.
#define WSSF2PPM            (0.1)                 //!< Units of 0.1 pulse per metre (ppm).
#define WSSFA2PC            (0.002)               //!< Units of 0.002% of scale factor.
#define WSDELAY2S           (0.1)                 //!< Units of 0.1 s.
#define WSNOISE2CNT         (0.1)                 //!< Units of 0.1 count for wheel speed noise.
#define UNDUL2M             (0.005)               //!< Units of 5 mm.
#define DOPFACTOR           (0.1)                 //!< Resolution of 0.1.
#define OMNISTAR_MIN_FREQ   (1.52e9)              //!< (Hz) i.e. 1520.0 MHz.
#define OMNIFREQ2HZ         (1000.0)              //!< Resolution of 1 kHz.
#define SNR2DB              (0.2)                 //!< Resolution of 0.2 dB.
#define LTIME2SEC           (1.0)                 //!< Resolution of 1.0 s.
#define TEMPK_OFFSET        (203.15)              //!< Temperature offset in degrees K.
#define ABSZERO_TEMPC       (-273.15)             //!< Absolute zero (i.e. 0 deg K) in deg C.
#define FINEANG2RAD         (1.74532925199433e-9) //!< Units of 0.1 udeg.
#define ALT2M               (1e-3)                //!< Units of 1 mm.
#define SUPPLYV2V           (0.1)                 //!< Units of 0.1 V.

// Mathematical constant definitions
#ifndef M_PI
#define M_PI (3.1415926535897932384626433832795)  //!< Pi.
#endif
#ifndef M_PI_2
#define M_PI_2 (1.5707963267948966192313216916398)  //!< Pi/2.
#endif
#define DEG2RAD             (M_PI/180.0)  //!< Convert degrees to radians.
#define RAD2DEG             (180.0/M_PI)  //!< Convert radians to degrees.
#define POS_INT_24          (8388607)     //!< Maximum value of a two's complement 24 bit integer.
#define NEG_INT_24          (-8388607)    //!< Minimum value of a two's complement 24 bit integer.
#define INV_INT_24          (-8388608)    //!< Represents an invalid two's complement 24 bit integer.

#define NCOM_COUNT_TOO_OLD  (150)         //!< Cycle counter for data too old.
#define NCOM_STDCNT_MAX     (0xFF)        //!< Definition for the RTBNS accuracy counter.
#define MIN_HORZ_SPEED      (0.07)        //!< 0.07 m/s hold distance.
#define MIN_VERT_SPEED      (0.07)        //!< 0.07 m/s hold distance.
#define SPEED_HOLD_FACTOR   (2.0)         //!< Hold distance when speed within 2 sigma of 0.
#define MINUTES_IN_WEEK     (10080)       //!< Number of minutes in a week.

// OmniStar status definitions
#define NCOM_OMNI_STATUS_UNKNOWN      (0xFF)
#define NCOM_OMNI_STATUS_VBSEXPIRED   (0x01)
#define NCOM_OMNI_STATUS_VBSREGION    (0x02)
#define NCOM_OMNI_STATUS_VBSNOBASE    (0x04)
#define NCOM_OMNI_STATUS_HPEXPIRED    (0x08)
#define NCOM_OMNI_STATUS_HPREGION     (0x10)
#define NCOM_OMNI_STATUS_HPNOBASE     (0x20)
#define NCOM_OMNI_STATUS_HPNOCONVERGE (0x40)
#define NCOM_OMNI_STATUS_HPKEYINVALID (0x80)

// GPS hardware status definitions
#define NCOM_GPS_ANT_STATUS_BITMASK   (0x03)
#define NCOM_GPS_ANT_STATUS_DONTKNOW  (0x03)
#define NCOM_GPS_ANT_STATUS_BITSHIFT  (0)
#define NCOM_GPS_ANT_POWER_BITMASK    (0x0C)
#define NCOM_GPS_ANT_POWER_DONTKNOW   (0x0C)
#define NCOM_GPS_ANT_POWER_BITSHIFT   (2)

// GPS feature set 1 definitions
#define NCOM_GPS_FEATURE_PSRDIFF      (0x01)
#define NCOM_GPS_FEATURE_SBAS         (0x02)
#define NCOM_GPS_FEATURE_OMNIVBS      (0x08)
#define NCOM_GPS_FEATURE_OMNIHP       (0x10)
#define NCOM_GPS_FEATURE_L1DIFF       (0x20)
#define NCOM_GPS_FEATURE_L1L2DIFF     (0x40)

// GPS feature set 2 definitions
#define NCOM_GPS_FEATURE_GLONASS      (0x01)
#define NCOM_GPS_FEATURE_GALILEO      (0x02)
#define NCOM_GPS_FEATURE_RAWRNG       (0x04)
#define NCOM_GPS_FEATURE_RAWDOP       (0x08)
#define NCOM_GPS_FEATURE_RAWL1        (0x10)
#define NCOM_GPS_FEATURE_RAWL2        (0x20)
#define NCOM_GPS_FEATURE_RAWL5        (0x40)

// GPS feature valid definition
#define NCOM_GPS_FEATURE_VALID        (0x80)

// The start of GPS time in a time_t style. In this version it is a constant, but this constant assumes that
// the local machine uses 00:00:00 01/01/1970 as its Epoch time. If your machine is different then you need to
// convert 00:00:00 06/01/1980 in to the local machine's time_t time.
#define GPS_TIME_START_TIME_T         (315964800)

// Second order filter class
#define INPUT_JITTER_TOLERANCE     (0.01)  // i.e. 1%

// Array range checks
#define COM_UNKNOWN(y)       ( ((int)(sizeof(y)/sizeof(y[0]))) - 1 )
#define COM_CK_VALIDITY(x,y) ( (((x)<0) || ((x)>COM_UNKNOWN(y))) ? COM_UNKNOWN(y) : (x) )




//############################################################################################################
//##                                                                                                        ##
//##  Static declarations                                                                                   ##
//##                                                                                                        ##
//############################################################################################################


//============================================================================================================
// Types.

//! Various packet parsing packet states.
typedef enum
{
	PARSED_PACKET_VALID,      //!< Parsed packet in good shape.
	PARSED_PACKET_INCOMPLETE, //!< Parsed packet is incomplete.
	PARSED_PACKET_CORRUPT     //!< Parsed packet is corrupted.
} ParsedPacketType;


//============================================================================================================
// Functions.

static ParsedPacketType PktStatus(size_t Len, const unsigned char *p);
static void RemoveFromBuffer(NComRxCInternal *Com, int n);
static void UpdateNavInvalidate(NComRxC *Com);
static void UpdateNav(NComRxC *Com);
static void FilteredOutputsInvalidate(NComRxC *Com);
static void FilteredOutputsCompute(NComRxC *Com);
static void RotateOutputsInvalidate(NComRxC *Com);
static void RotateOutputsCompute(NComRxC *Com);
static void Iso8855OutputsCompute(NComRxC *Com);
static void SpeedSlipInvalidate(NComRxC *Com);
static void SpeedSlipCompute(NComRxC *Com);
static void DistanceInvalidate(NComRxC *Com);
static void DistanceCompute(NComRxC *Com, char trig);
static void NComSetLinAccFiltFreq(NComRxC *Com, double freq);
static void NComSetLinAccFiltZeta(NComRxC *Com, double zeta);
static void NComSetAngAccFiltFreq(NComRxC *Com, double freq);
static void NComSetAngAccFiltZeta(NComRxC *Com, double zeta);
static void SetRefFrame(NComRxC *Com, double lat, double lon, double alt, double heading);

// Decode functions
static void DecodeStatusMsg(NComRxC *Com);
static void DecodeExtra0(NComRxC *Com);
static void DecodeExtra1(NComRxC *Com);
static void DecodeExtra2(NComRxC *Com);
static void DecodeExtra3(NComRxC *Com);
static void DecodeExtra4(NComRxC *Com);
static void DecodeExtra5(NComRxC *Com);
static void DecodeExtra6(NComRxC *Com);
static void DecodeExtra7(NComRxC *Com);
static void DecodeExtra8(NComRxC *Com);
static void DecodeExtra9(NComRxC *Com);
static void DecodeExtra10(NComRxC *Com);
static void DecodeExtra11(NComRxC *Com);
static void DecodeExtra12(NComRxC *Com);
static void DecodeExtra13(NComRxC *Com);
static void DecodeExtra14(NComRxC *Com);
static void DecodeExtra15(NComRxC *Com);
static void DecodeExtra16(NComRxC *Com);
static void DecodeExtra17(NComRxC *Com);
static void DecodeExtra18(NComRxC *Com);
static void DecodeExtra19(NComRxC *Com);
static void DecodeExtra20(NComRxC *Com);
static void DecodeExtra21(NComRxC *Com);
static void DecodeExtra22(NComRxC *Com);
static void DecodeExtra23(NComRxC *Com);
static void DecodeExtra24(NComRxC *Com);
static void DecodeExtra25(NComRxC *Com);
static void DecodeExtra26(NComRxC *Com);
static void DecodeExtra27(NComRxC *Com);
static void DecodeExtra28(NComRxC *Com);
static void DecodeExtra29(NComRxC *Com);
static void DecodeExtra30(NComRxC *Com);
static void DecodeExtra31(NComRxC *Com);
static void DecodeExtra32(NComRxC *Com);
static void DecodeExtra33(NComRxC *Com);
static void DecodeExtra34(NComRxC *Com);
static void DecodeExtra35(NComRxC *Com);
static void DecodeExtra36(NComRxC *Com);
static void DecodeExtra37(NComRxC *Com);
static void DecodeExtra38(NComRxC *Com);
static void DecodeExtra39(NComRxC *Com);
static void DecodeExtra41(NComRxC *Com);
static void DecodeExtra42(NComRxC *Com);
static void DecodeExtra43(NComRxC *Com);
static void DecodeExtra44(NComRxC *Com);
static void DecodeExtra45(NComRxC *Com);
static void DecodeExtra46(NComRxC *Com);
static void DecodeExtra47(NComRxC *Com);
static void DecodeExtra48(NComRxC *Com);
static void DecodeExtra49(NComRxC *Com);
static void DecodeExtra50(NComRxC *Com);
static void DecodeExtra55(NComRxC *Com);
static void DecodeExtra56(NComRxC *Com);
static void DecodeExtra57(NComRxC *Com);
static void DecodeExtra59(NComRxC *Com);
static void DecodeExtra60(NComRxC *Com);
static void DecodeExtra61(NComRxC *Com);
static void DecodeExtra62(NComRxC *Com);
static void DecodeExtra63(NComRxC *Com);
static void DecodeExtra64(NComRxC *Com);
static void DecodeExtra65(NComRxC *Com);
static void DecodeExtra66(NComRxC *Com);
static void DecodeExtra67(NComRxC *Com);
static void DecodeExtra72(NComRxC *Com);
static void DecodeExtra73(NComRxC *Com);


// Filter functions
static Filt2ndOrder *Filt2ndOrderCreate();
static void Filt2ndOrderDestroy(Filt2ndOrder *Filt);
static void Filt2ndOrderReset(Filt2ndOrder *Filt);
static void Filt2ndOrderSetCharacteristics(Filt2ndOrder *Filt, double freq, double zeta);
static void Filt2ndOrderNewInput(Filt2ndOrder *Filt, double t, double x);
static void Filt2ndOrderInitialise(Filt2ndOrder *Filt);

// Utilities
static void strgrab(char *destination, int destination_length, const char *source, int source_length);

// Byte casts
static double   cast_8_byte_LE_to_real64(const uint8_t *b);
static float    cast_4_byte_LE_to_real32(const uint8_t *b);
static  int32_t cast_4_byte_LE_to_int32 (const uint8_t *b);
static uint32_t cast_4_byte_LE_to_uint32(const uint8_t *b);
static  int32_t cast_3_byte_LE_to_int32 (const uint8_t *b);
static uint32_t cast_3_byte_LE_to_uint32(const uint8_t *b);
static  int16_t cast_2_byte_LE_to_int16 (const uint8_t *b);
static uint16_t cast_2_byte_LE_to_uint16(const uint8_t *b);
static uint32_t incr_2_byte_LE_to_uint32(const uint8_t *b, uint32_t z);
static uint32_t incr_1_byte_LE_to_uint32(const uint8_t *b, uint32_t z);

// Matrix library
static int MatAllocR(Mat *R, long r, long c);
static int MatFillR(Mat *R, long r, long c, ...);
static int MatFree(Mat *A);
static int MatMultRAB(Mat *R, Mat *A, Mat *B);
static int MatMultRAtB(Mat *r, Mat *a, Mat *b);
static int MatScaleRsA(Mat *R, MatElement s, const Mat *A);
static int MatAddRAB(Mat *R, const Mat *A, const Mat *B);
static int MatSubRAB(Mat *R, const Mat *A, const Mat *B);
static int MatCrossProduct(Mat *r, const Mat *u, const Mat *v);
static int Euler2DirCos(Mat *C, const Mat *E);
static int Euler2DirCos2(Mat *C, Mat *E);
static int Euler2DirCosH(Mat *C, Mat *E);
static int DirCos2Euler(Mat *E, const Mat *C);

// ECEF library
static int Ecef2NedRotation(Mat *C_en, double lat, double lon);
static int Lib__compute_local_gravity_magnitude(double *g_mag, double lat, double depth);
static int Lib__compute_local_gravity(Mat *g_n, double lat, double depth);
static int Lib__compute_earth_curvature(double *rho_e, double *rho_n, double lat);
static int Lib__compute_transport_rate(Mat *w_enn, double lat, double de, const Mat *v_en, double rho_e, double rho_n);
static int Lib__compute_imu_b2n_corrections(Mat *w_inb, Mat *g_n, Mat *cora_n, double lat, double lon, double de, const Mat *v_en, Mat *C_bn);
static int Lib__apply_imu_b2n_corrections_a(Mat *a_nbn, Mat *a_rawb, const Mat *g_n, const Mat *cora_n, Mat *C_bn);
static int Lib__apply_imu_b2n_corrections_w(Mat *w_nbb, const Mat *w_ibb, const Mat *w_inb);


//============================================================================================================
// Enumerated types.

static const char *NAV_OUTPUT_PACKET_TYPE_NAME[9] =
{
	"Invalid",
	"Empty",
	"Regular",
	"Status",
	"In 1 Down",
	"In 1 Up",
	"Out 1",
	"Interpolated",
	"Unknown"
};

static const char *NAVIGATION_STATUS_NAME[24] =
{
	"Invalid",
	"Raw Inertial Data",
	"Ready to Initialise",
	"Locking On",
	"Real Time",
	"Unlocked",
	"Firmware Expired",
	"Reserved",
	"Reserved",
	"Reserved",
	"Status Only",
	"Reserved",
	"Reserved",
	"Reserved",
	"Reserved",
	"Reserved",
	"Reserved",
	"Reserved",
	"Reserved",
	"Reserved",
	"Trigger (Initialise)",
	"Trigger (Locking On)",
	"Trigger (Real Time)",
	"Unknown"
};

static const char *COM_UMAC_STATUS_NAME[11] =
{
	"Error",
	"Time Valid",
	"Speed Threshold",
	"Output Lag",
	"Aligning Axis",
	"Bad Position",
	"Poor Position",
	"SPS Position",
	"Differential Position",
	"RTK Position",
	"Unknown"
};

static const char *COM_OPTION_VEHICLE_LEVEL_NAME[4] =
{
	"Initially not level",
	"Initially level",
	"Initial attitude specified",
	"Unknown"
};

static const char *COM_OPTION_VIBRATION_NAME[4] =
{
	"Normal",
	"High",
	"Very High",
	"Unknown"
};

static const char *COM_OPTION_GPS_ACCURACY_NAME[4] =
{
	"Some Obstructions",
	"Open Sky",
	"Frequent Obstructions",
	"Unknown"
};

static const char *COM_OPTION_OUTPUT_NAME[16] =
{
	"NCOM",
	"TCOM",
	"ABD",
	"TSS1",
	"THALES",
	"NMEA",
	"ACOM",
	"MCOM",
	"EM3000",
	"EM1000",
	"TSS HHRP",
	"PASHR",
	"PRDID",
	"MGCOM1",
	"Javad I+RTK",
	"Unknown"
};

static const char *COM_OPTION_HEADING_NAME[5] =
{
	"Never",
	"No Search",
	"After Initialisation",
	"Always",
	"Unknown"
};

static const char *COM_HEADING_QUALITY_NAME[5] =
{
	"None",
	"Poor",
	"RTK Float",
	"RTK Integer",
	"Unknown"
};

static const char *COM_HEADING_SEARCH_TYPE_NAME[5] =
{
	"Idle",
	"L1",
	"L2",
	"L1/L2",
	"Unknown"
};

static const char *COM_HEADING_SEARCH_STATUS_NAME[20] =
{
	"OK",
	"No Spare CPU",
	"No Seed",
	"No Master",
	"No Slave1",
	"No Slave2",
	"No Slave3",
	"Bad Length",
	"No matching ambiguities",
	"Too many ambiguities",
	"Lost Master",
	"Lost Slave1",
	"Lost Slave2",
	"Lost Slave3",
	"Sat Constellation Too Poor",
	"Covariance Error",
	"Ambiguous Ambiguities",
	"Lost Lock",
	"Disabled",
	"Unknown"
};

static const char *COM_HEADING_SEARCH_READY_NAME[3] =
{
	"Waiting",
	"Processing",
	"Unknown"
};

static const char *IMU_TYPE_NAME[8]=
{
	"SiIMU-A",
	"R&D IMU",
	"IMU2",
	"IMU2X",
	"IMU3",
	"IMU3X",
	"IMU5",
	"Unknown"
};

static const char *INTER_PCB_TYPE_NAME[5]=
{
	"14P0008A",
	"14P0008B",
	"14P0008C",
	"14P0008D",
	"Unknown"
};

static const char *FRONT_PCB_TYPE_NAME[8]=
{
	"14P0007A",
	"14P0009A",
	"14P0009B",
	"14P0009C",
	"14P0019C",
	"14P0019D",
	"14P0034C",
	"Unknown"
};

static const char *INTER_SW_ID_NAME[14]=
{
	"None",
	"030528.14an",
	"030724.14an",
	"030731.14an",
	"031023.14an",
	"031107.14an",
	"040131.14an",
	"050110.14an",
	"060105.14an",
	"080102.14an",
	"080204.14an",
	"081215.14bj",
	"120228.14an",
	"Unknown"
};

static const char *HARDWARE_CONFIG_TYPE_NAME[6]=
{
	"Integral Small Box",
	"Integral Standard Box",
	"Pod and Rack",
	"Extruded Standard Box",
	"Pod and Extrusion",
	"Unknown"
};

static const char *CPU_PCB_TYPE_NAME[3]=
{
	"TP400B",
	"TP500",
	"Unknown"
};

static const char *COM_DUAL_PORT_RAM_STATUS_NAME[12] =
{
	"Not Fitted",
	"Failed To Initialise",
	"Dead",
	"Down",
	"Overloaded",
	"Sporadic",
	"Slow",
	"Acceptable",
	"OK",
	"Good",
	"Excellent",
	"Unknown"
};

static const char *SERIAL_BAUD_NAME[16] =
{
	"Disabled",
	"300",
	"600",
	"1200",
	"2400",
	"4800",
	"9600",
	"19200",
	"38400",
	"57600",
	"76800",
	"115200",
	"230400",
	"460800",
	"921600",
	"Unknown"
};

static const char *CAN_BUS_BAUD_NAME[8] =
{
	"Disabled",
	"100000",
	"125000",
	"200000",
	"250000",
	"500000",
	"1000000",
	"Unknown"
};

static const char *GPS_TYPE_NAME[16]=
{
	"BeeLine",
	"OEM4",
	"None",
	"OEMV",
	"LEA4",
	"Generic",
	"Trimble 5700/5800",
	"Trimble AgGPS 132",
	"Topcon GB-500",
	"NavCom Sapphire",
	"LEA6",
	"Trimble BD920",
	"Leica GX1200",
	"Topcon B110",
	"OEM6",
	"Unknown"
};

static const char *GPS_FORMAT_NAME[10]=
{
	"OEM3 Binary",
	"OEM4 Binary",
	"UBX",
	"NMEA",
	"GSOF",
	"TSIP",
	"GRIL",
	"Debug",
	"NCT Binary",
	"Unknown"
};

static const char *COM_GPS_RATE_TYPE_NAME[8] =
{
	"Disabled",
	"1",
	"2",
	"4",
	"5",
	"10",
	"20",
	"Unknown"
};

static const char *COM_GPS_ANTENNA_STATUS_NAME[4] =
{
	"OK",
	"Open",
	"Short",
	"Unknown"
};

static const char *COM_GPS_ANTENNA_POWER_NAME[3] =
{
	"On",
	"Off",
	"Unknown"
};

static const char *COM_GPS_XMODE_TYPE_NAME[31] =
{
	"None",
	"Search",
	"Doppler",
	"SPS",
	"Differential",
	"RTK Float",
	"RTK Integer",
	"WAAS",
	"Omnistar",
	"Omnistar HP",
	"No Data",
	"Blanked",
	"Doppler (PP)",
	"SPS (PP)",
	"Differential (PP)",
	"RTK Float (PP)",
	"RTK Integer (PP)",
	"Omnistar XP",
	"CDGPS",
	"Not Recognised",
	"gxDoppler",
	"gxSPS",
	"gxDifferential",
	"gxFloat",
	"gxInteger",
	"ixDoppler",
	"ixSPS",
	"ixDifferential",
	"ixFloat",
	"ixInteger",
	"Unknown"
};




//############################################################################################################
//##                                                                                                        ##
//##  NComRxCInteral                                                                                        ##
//##                                                                                                        ##
//############################################################################################################


//============================================================================================================
//! \brief Invalidation of internally used data space of the decoder.

static void NComInternalInvalidate(NComRxCInternal *Com)
{
	Com->mCurChannel = -1;
	Com->mCurLen = 0;
	Com->mPktProcessed = 0;

	// Timing
	Com->mMilliSecs = -1;
	Com->mMinutes = -1;

	// Rotations
	Com->C_on_valid = 0;
	Com->C_oh_valid = 0;
	Com->C_hn_valid = 0;

	// Triggers
	Com->mTrigCount = 0;
	Com->mTrig2Count = 0;
	Com->mDigitalOutCount = 0;

	// Clear the parameters for calculating distance travelled
	Com->mPrevDist2dValid = 0;
	Com->mPrevDist2dTime = 0.0;
	Com->mPrevDist2dSpeed = 0.0;
	Com->mPrevDist2d = 0.0;
	Com->mPrevDist3dValid = 0;
	Com->mPrevDist3dTime = 0.0;
	Com->mPrevDist3dSpeed = 0.0;
	Com->mPrevDist3d = 0.0;

	// Clear the parameters for calculating wheel-speed tacho frequency
	Com->mIsOldWSpeedTimeValid = 0; Com->mOldWSpeedTime = 0.0;
	Com->mIsOldWSpeedCountValid = 0; Com->mOldWSpeedCount = 0.0;

	// Reference frame parameters
	Com->mIsAccurateRefLatValid = 0; Com->mAccurateRefLat = 0.0;
	Com->mIsAccurateRefLonValid = 0; Com->mAccurateRefLon = 0.0;
	Com->mIsAccurateRefAltValid = 0; Com->mAccurateRefAlt = 0.0;
	Com->mIsAccurateRefHeadingValid = 0; Com->mAccurateRefHeading = 0.0;

	// Linear acceleration filter
	Com->mIsLinAccFiltFixed    = 0;
	Com->mHasLinAccFiltChanged = 0;
	Com->mIsLinAccFiltOff      = 0;
	Filt2ndOrderReset(&Com->FiltForAx);
	Filt2ndOrderReset(&Com->FiltForAy);
	Filt2ndOrderReset(&Com->FiltForAz);

	// Reset the angular rate differentiation variables
	Com->mPrevWx = 0.0;
	Com->mPrevWy = 0.0;
	Com->mPrevWz = 0.0;
	Com->mPrevWbTime = 0.0;

	// Angular acceleration filter
	Com->mIsAngAccFiltFixed    = 0;
	Com->mHasAngAccFiltChanged = 0;
	Com->mIsAngAccFiltOff      = 0;
	Filt2ndOrderReset(&Com->FiltForYx);
	Filt2ndOrderReset(&Com->FiltForYy);
	Filt2ndOrderReset(&Com->FiltForYz);
}


//============================================================================================================
//! \brief Constructor for internally used data space of the decoder.
//!
//! If any of the work space matrices did not allocate, they are all freed and a flag instructs the decoder
//! that they are not available. This mean any quantity requiring these matrices will not be decoded.

static NComRxCInternal *NComInternalCreate()
{
	NComRxCInternal *Com = (NComRxCInternal *)calloc(1, sizeof(NComRxCInternal));

	if (Com)
	{
		Com->mCurStatus   = Com->mCurPkt + PI_CHANNEL_STATUS;

		Com->mMatrixHold  = MatAllocR(&Com->E,  3, 1);
		Com->mMatrixHold |= MatAllocR(&Com->Ab, 3, 1);
		Com->mMatrixHold |= MatAllocR(&Com->Al, 3, 1);
		Com->mMatrixHold |= MatAllocR(&Com->Wb, 3, 1);
		Com->mMatrixHold |= MatAllocR(&Com->Wl, 3, 1);
		Com->mMatrixHold |= MatAllocR(&Com->Vn, 3, 1);
		Com->mMatrixHold |= MatAllocR(&Com->Vl, 3, 1);
		Com->mMatrixHold |= MatAllocR(&Com->Yb, 3, 1);
		Com->mMatrixHold |= MatAllocR(&Com->Yl, 3, 1);
		Com->mMatrixHold |= MatAllocR(&Com->C_on, 3, 3);
		Com->mMatrixHold |= MatAllocR(&Com->C_oh, 3, 3);
		Com->mMatrixHold |= MatAllocR(&Com->C_hn, 3, 3);
		Com->mMatrixHold |= MatAllocR(&Com->C_sn, 3, 3);
		Com->mMatrixHold |= MatAllocR(&Com->C_os, 3, 3);

		if (Com->mMatrixHold)
		{
			MatFree(&Com->E);
			MatFree(&Com->Ab);
			MatFree(&Com->Al);
			MatFree(&Com->Wb);
			MatFree(&Com->Wl);
			MatFree(&Com->Vn);
			MatFree(&Com->Vl);
			MatFree(&Com->Yb);
			MatFree(&Com->Yl);
			MatFree(&Com->C_on);
			MatFree(&Com->C_oh);
			MatFree(&Com->C_hn);
			MatFree(&Com->C_sn);
			MatFree(&Com->C_os);
		}

		Com->mNumChars         = 0;
		Com->mSkippedChars     = 0;
		Com->mNumPackets       = 0;
		Com->mHoldDistWhenSlow = 0;
	}

	// Reset the linear acceleration filters
	Filt2ndOrderReset(&Com->FiltForAx);
	Filt2ndOrderReset(&Com->FiltForAy);
	Filt2ndOrderReset(&Com->FiltForAz);

	// Reset the angular acceleration filters
	Filt2ndOrderReset(&Com->FiltForYx);
	Filt2ndOrderReset(&Com->FiltForYy);
	Filt2ndOrderReset(&Com->FiltForYz);

	return Com;
}


//============================================================================================================
//! \brief Destructor for internally used data space of the decoder.

static void NComInternalDestroy(NComRxCInternal *Com)
{
	if (Com != NULL)
	{
		if (!Com->mMatrixHold)
		{
			MatFree(&Com->E );
			MatFree(&Com->Ab);
			MatFree(&Com->Al);
			MatFree(&Com->Wb);
			MatFree(&Com->Wl);
			MatFree(&Com->Vn);
			MatFree(&Com->Vl);
			MatFree(&Com->Yb);
			MatFree(&Com->Yl);
			MatFree(&Com->C_on);
			MatFree(&Com->C_oh);
			MatFree(&Com->C_hn);
			MatFree(&Com->C_sn);
			MatFree(&Com->C_os);
		}

		free(Com);
	}
}


//============================================================================================================
//! \brief Copy for internally used data space of the decoder.
//!
//! \warning mMatrixHold property: Destination->mMatrixHold = Destination->mMatrixHold or Source->mMatrixHold.
//! That is, if either destination and/or source have no matrices before copy, then the destination will have
//! no matrices after the copy.

void NComInternalCopy(NComRxCInternal *ComDestination, const NComRxCInternal *ComSource)
{
	if (ComSource->mMatrixHold)
	{
		if (ComDestination->mMatrixHold)
		{
			// No dynamic memory anywhere so do nothing here.
		}
		else
		{
			// Clear out the destination matrix memory as ComDestination->mMatrixHold will become true.

			MatFree(&ComDestination->E );
			MatFree(&ComDestination->Ab);
			MatFree(&ComDestination->Al);
			MatFree(&ComDestination->Wb);
			MatFree(&ComDestination->Wl);
			MatFree(&ComDestination->Vn);
			MatFree(&ComDestination->Vl);
			MatFree(&ComDestination->Yb);
			MatFree(&ComDestination->Yl);
			MatFree(&ComDestination->C_on);
			MatFree(&ComDestination->C_oh);
			MatFree(&ComDestination->C_hn);
			MatFree(&ComDestination->C_sn);
			MatFree(&ComDestination->C_os);
		}

		// So now should have no dynamic memory and any pointers set null so low level shallow copy ok.

		memcpy(ComDestination, ComSource, sizeof(NComRxCInternal));
	}
	else
	{
		// Do a low level shallow copy and revert any clobbered pointers.

		MatElement *E    = ComDestination->E .m;
		MatElement *Ab   = ComDestination->Ab.m;
		MatElement *Al   = ComDestination->Al.m;
		MatElement *Wb   = ComDestination->Wb.m;
		MatElement *Wl   = ComDestination->Wl.m;
		MatElement *Vn   = ComDestination->Vn.m;
		MatElement *Vl   = ComDestination->Vl.m;
		MatElement *Yb   = ComDestination->Yb.m;
		MatElement *Yl   = ComDestination->Yl.m;
		MatElement *C_on = ComDestination->C_on.m;
		MatElement *C_oh = ComDestination->C_oh.m;
		MatElement *C_hn = ComDestination->C_hn.m;
		MatElement *C_sn = ComDestination->C_sn.m;
		MatElement *C_os = ComDestination->C_os.m;

		memcpy(ComDestination, ComSource, sizeof(NComRxCInternal));

		ComDestination->E .m   = E;
		ComDestination->Ab.m   = Ab;
		ComDestination->Al.m   = Al;
		ComDestination->Wb.m   = Wb;
		ComDestination->Wl.m   = Wl;
		ComDestination->Vn.m   = Vn;
		ComDestination->Vl.m   = Vl;
		ComDestination->Yb.m   = Yb;
		ComDestination->Yl.m   = Yl;
		ComDestination->C_on.m = C_on;
		ComDestination->C_oh.m = C_oh;
		ComDestination->C_hn.m = C_hn;
		ComDestination->C_sn.m = C_sn;
		ComDestination->C_os.m = C_os;

		if (ComDestination->mMatrixHold)
		{
			// Even though the source used matrices, the destination does not so nothing to do.
		}
		else
		{
			// Now copy over the matrix data. ** Assumption: matrices have not been resized. **

			memcpy(ComDestination->E   .m, ComSource->E   .m, ComSource->E   .tr * ComSource->E   .tc * sizeof(MatElement));
			memcpy(ComDestination->Ab  .m, ComSource->Ab  .m, ComSource->Ab  .tr * ComSource->Ab  .tc * sizeof(MatElement));
			memcpy(ComDestination->Al  .m, ComSource->Al  .m, ComSource->Al  .tr * ComSource->Al  .tc * sizeof(MatElement));
			memcpy(ComDestination->Wb  .m, ComSource->Wb  .m, ComSource->Wb  .tr * ComSource->Wb  .tc * sizeof(MatElement));
			memcpy(ComDestination->Wl  .m, ComSource->Wl  .m, ComSource->Wl  .tr * ComSource->Wl  .tc * sizeof(MatElement));
			memcpy(ComDestination->Vn  .m, ComSource->Vn  .m, ComSource->Vn  .tr * ComSource->Vn  .tc * sizeof(MatElement));
			memcpy(ComDestination->Vl  .m, ComSource->Vl  .m, ComSource->Vl  .tr * ComSource->Vl  .tc * sizeof(MatElement));
			memcpy(ComDestination->Yb  .m, ComSource->Yb  .m, ComSource->Yb  .tr * ComSource->Yb  .tc * sizeof(MatElement));
			memcpy(ComDestination->Yl  .m, ComSource->Yl  .m, ComSource->Yl  .tr * ComSource->Yl  .tc * sizeof(MatElement));
			memcpy(ComDestination->C_on.m, ComSource->C_on.m, ComSource->C_on.tr * ComSource->C_on.tc * sizeof(MatElement));
			memcpy(ComDestination->C_oh.m, ComSource->C_oh.m, ComSource->C_oh.tr * ComSource->C_oh.tc * sizeof(MatElement));
			memcpy(ComDestination->C_hn.m, ComSource->C_hn.m, ComSource->C_hn.tr * ComSource->C_hn.tc * sizeof(MatElement));
			memcpy(ComDestination->C_sn.m, ComSource->C_sn.m, ComSource->C_sn.tr * ComSource->C_sn.tc * sizeof(MatElement));
			memcpy(ComDestination->C_os.m, ComSource->C_os.m, ComSource->C_os.tr * ComSource->C_os.tc * sizeof(MatElement));
		}
	}

	// Ensure we do not clobber current status pointer.

	ComDestination->mCurStatus = ComDestination->mCurPkt + PI_CHANNEL_STATUS;
}



//############################################################################################################
//##                                                                                                        ##
//##  NComRxCGps                                                                                            ##
//##                                                                                                        ##
//############################################################################################################


//============================================================================================================
// Access functions.

// *** Code Generation Begin - NComRxCGps Access Functions ***

//------------------------------------------------------------------------------------------------------------
// GPS Information

// System information

const char *NComGpsGetTypeString(const NComRxCGps *Com) { return GPS_TYPE_NAME[Com->mType]; }
static void NComGpsSetType(NComRxCGps *Com, int v) { Com->mType = COM_CK_VALIDITY(v, GPS_TYPE_NAME); Com->mIsTypeValid = 1; }
const char *NComGpsGetFormatString(const NComRxCGps *Com) { return GPS_FORMAT_NAME[Com->mFormat]; }
static void NComGpsSetFormat(NComRxCGps *Com, int v) { Com->mFormat = COM_CK_VALIDITY(v, GPS_FORMAT_NAME); Com->mIsFormatValid = 1; }

const char *NComGpsGetRawRateString(const NComRxCGps *Com) { return COM_GPS_RATE_TYPE_NAME[Com->mRawRate]; }
static void NComGpsSetRawRate(NComRxCGps *Com, int v) { Com->mRawRate = COM_CK_VALIDITY(v, COM_GPS_RATE_TYPE_NAME); Com->mIsRawRateValid = 1; }
const char *NComGpsGetPosRateString(const NComRxCGps *Com) { return COM_GPS_RATE_TYPE_NAME[Com->mPosRate]; }
static void NComGpsSetPosRate(NComRxCGps *Com, int v) { Com->mPosRate = COM_CK_VALIDITY(v, COM_GPS_RATE_TYPE_NAME); Com->mIsPosRateValid = 1; }
const char *NComGpsGetVelRateString(const NComRxCGps *Com) { return COM_GPS_RATE_TYPE_NAME[Com->mVelRate]; }
static void NComGpsSetVelRate(NComRxCGps *Com, int v) { Com->mVelRate = COM_CK_VALIDITY(v, COM_GPS_RATE_TYPE_NAME); Com->mIsVelRateValid = 1; }

const char *NComGpsGetAntStatusString(const NComRxCGps *Com) { return COM_GPS_ANTENNA_STATUS_NAME[Com->mAntStatus]; }
static void NComGpsSetAntStatus(NComRxCGps *Com, int v) { Com->mAntStatus = COM_CK_VALIDITY(v, COM_GPS_ANTENNA_STATUS_NAME); Com->mIsAntStatusValid = 1; }
const char *NComGpsGetAntPowerString(const NComRxCGps *Com) { return COM_GPS_ANTENNA_POWER_NAME[Com->mAntPower]; }
static void NComGpsSetAntPower(NComRxCGps *Com, int v) { Com->mAntPower = COM_CK_VALIDITY(v, COM_GPS_ANTENNA_POWER_NAME); Com->mIsAntPowerValid = 1; }
const char *NComGpsGetPosModeString(const NComRxCGps *Com) { return COM_GPS_XMODE_TYPE_NAME[Com->mPosMode]; }
static void NComGpsSetPosMode(NComRxCGps *Com, int v) { Com->mPosMode = COM_CK_VALIDITY(v, COM_GPS_XMODE_TYPE_NAME); Com->mIsPosModeValid = 1; }

const char *NComGpsGetSerBaudString(const NComRxCGps *Com) { return SERIAL_BAUD_NAME[Com->mSerBaud]; }
static void NComGpsSetSerBaud(NComRxCGps *Com, int v) { Com->mSerBaud = COM_CK_VALIDITY(v, SERIAL_BAUD_NAME); Com->mIsSerBaudValid = 1; }

// Status

static void NComGpsSetNumSats(NComRxCGps *Com, int v) { Com->mNumSats = v; Com->mIsNumSatsValid = 1; }

static void NComGpsSetCpuUsed(NComRxCGps *Com, double v) { Com->mCpuUsed = v; Com->mIsCpuUsedValid = 1; }
static void NComGpsSetCoreNoise(NComRxCGps *Com, double v) { Com->mCoreNoise = v; Com->mIsCoreNoiseValid = 1; }
static void NComGpsSetCoreTemp(NComRxCGps *Com, double v) { Com->mCoreTemp = v; Com->mIsCoreTempValid = 1; }
static void NComGpsSetSupplyVolt(NComRxCGps *Com, double v) { Com->mSupplyVolt = v; Com->mIsSupplyVoltValid = 1; }

// Received data statistics

static void NComGpsSetChars(NComRxCGps *Com, uint32_t v) { Com->mChars = v; Com->mIsCharsValid = 1; }
static void NComGpsSetCharsSkipped(NComRxCGps *Com, uint32_t v) { Com->mCharsSkipped = v; Com->mIsCharsSkippedValid = 1; }
static void NComGpsSetPkts(NComRxCGps *Com, uint32_t v) { Com->mPkts = v; Com->mIsPktsValid = 1; }
static void NComGpsSetOldPkts(NComRxCGps *Com, uint32_t v) { Com->mOldPkts = v; Com->mIsOldPktsValid = 1; }

// *** Code Generation End - NComRxCGps Access Functions ***


//============================================================================================================
//! \brief Invalidation.

void NComGpsInvalidate(NComRxCGps *Com)
{
	// *** Code Generation Begin - NComRxCGps Invalidate ***

	//--------------------------------------------------------------------------------------------------------
	// GPS Information

	// System information

	Com->mIsTypeValid = 0; Com->mType = 0;
	Com->mIsFormatValid = 0; Com->mFormat = 0;

	Com->mIsRawRateValid = 0; Com->mRawRate = 0;
	Com->mIsPosRateValid = 0; Com->mPosRate = 0;
	Com->mIsVelRateValid = 0; Com->mVelRate = 0;

	Com->mIsAntStatusValid = 0; Com->mAntStatus = 0;
	Com->mIsAntPowerValid = 0; Com->mAntPower = 0;
	Com->mIsPosModeValid = 0; Com->mPosMode = 0;

	Com->mIsSerBaudValid = 0; Com->mSerBaud = 0;

	// Status

	Com->mIsNumSatsValid = 0; Com->mNumSats = 0;

	Com->mIsCpuUsedValid = 0; Com->mCpuUsed = 0.0;
	Com->mIsCoreNoiseValid = 0; Com->mCoreNoise = 0.0;
	Com->mIsCoreTempValid = 0; Com->mCoreTemp = 0.0;
	Com->mIsSupplyVoltValid = 0; Com->mSupplyVolt = 0.0;

	// Received data statistics

	Com->mIsCharsValid = 0; Com->mChars = 0;
	Com->mIsCharsSkippedValid = 0; Com->mCharsSkipped = 0;
	Com->mIsPktsValid = 0; Com->mPkts = 0;
	Com->mIsOldPktsValid = 0; Com->mOldPkts = 0;

	// *** Code Generation End - NComRxCGps Invalidate ***
}


//============================================================================================================
//! \brief Constructor for GPS information structure.

NComRxCGps *NComGpsCreate()
{
	return (NComRxCGps *)calloc(1, sizeof(NComRxCGps));
}


//============================================================================================================
//! \brief Destructor for GPS information structure.

void NComGpsDestroy(NComRxCGps *Com)
{
	if (Com != NULL)
	{
		free(Com);
	}
}


//============================================================================================================
//! \brief Copy for GPS information structure.

void NComGpsCopy(NComRxCGps *ComDestination, const NComRxCGps *ComSource)
{
	memcpy(ComDestination, ComSource, sizeof(NComRxCGps));
}




//############################################################################################################
//##                                                                                                        ##
//##  NComRxC                                                                                               ##
//##                                                                                                        ##
//############################################################################################################


//============================================================================================================
// Access functions.

// *** Code Generation Begin - NComRxC Access Functions ***

//------------------------------------------------------------------------------------------------------------
// General information

// Status

const char *NComGetOutputPacketTypeString(const NComRxC *Com) { return NAV_OUTPUT_PACKET_TYPE_NAME[Com->mOutputPacketType]; }
static void NComSetOutputPacketType(NComRxC *Com, int v) { Com->mOutputPacketType = COM_CK_VALIDITY(v, NAV_OUTPUT_PACKET_TYPE_NAME); Com->mIsOutputPacketTypeValid = 1; }
const char *NComGetInsNavModeString(const NComRxC *Com) { return NAVIGATION_STATUS_NAME[Com->mInsNavMode]; }
static void NComSetInsNavMode(NComRxC *Com, int v) { Com->mInsNavMode = COM_CK_VALIDITY(v, NAVIGATION_STATUS_NAME); Com->mIsInsNavModeValid = 1; }

// System information

static void NComSetSerialNumber(NComRxC *Com, int v) { Com->mSerialNumber = v; Com->mIsSerialNumberValid = 1; }
static void NComSetDevId(NComRxC *Com, const char *v, int n) { strgrab(Com->mDevId, DEV_ID_STRLEN, v, n); Com->mIsDevIdValid = 1; }

static void NComSetOsVersion1(NComRxC *Com, int v) { Com->mOsVersion1 = v; Com->mIsOsVersion1Valid = 1; }
static void NComSetOsVersion2(NComRxC *Com, int v) { Com->mOsVersion2 = v; Com->mIsOsVersion2Valid = 1; }
static void NComSetOsVersion3(NComRxC *Com, int v) { Com->mOsVersion3 = v; Com->mIsOsVersion3Valid = 1; }
static void NComSetOsScriptId(NComRxC *Com, const char *v, int n) { strgrab(Com->mOsScriptId, OS_SCRIPT_ID_STRLEN, v, n); Com->mIsOsScriptIdValid = 1; }

const char *NComGetImuTypeString(const NComRxC *Com) { return IMU_TYPE_NAME[Com->mImuType]; }
static void NComSetImuType(NComRxC *Com, int v) { Com->mImuType = COM_CK_VALIDITY(v, IMU_TYPE_NAME); Com->mIsImuTypeValid = 1; }
const char *NComGetCpuPcbTypeString(const NComRxC *Com) { return CPU_PCB_TYPE_NAME[Com->mCpuPcbType]; }
static void NComSetCpuPcbType(NComRxC *Com, int v) { Com->mCpuPcbType = COM_CK_VALIDITY(v, CPU_PCB_TYPE_NAME); Com->mIsCpuPcbTypeValid = 1; }
const char *NComGetInterPcbTypeString(const NComRxC *Com) { return INTER_PCB_TYPE_NAME[Com->mInterPcbType]; }
static void NComSetInterPcbType(NComRxC *Com, int v) { Com->mInterPcbType = COM_CK_VALIDITY(v, FRONT_PCB_TYPE_NAME); Com->mIsInterPcbTypeValid = 1; }
const char *NComGetFrontPcbTypeString(const NComRxC *Com) { return FRONT_PCB_TYPE_NAME[Com->mFrontPcbType]; }
static void NComSetFrontPcbType(NComRxC *Com, int v) { Com->mFrontPcbType = COM_CK_VALIDITY(v, FRONT_PCB_TYPE_NAME); Com->mIsFrontPcbTypeValid = 1; }
const char *NComGetInterSwIdString(const NComRxC *Com) { return INTER_SW_ID_NAME[Com->mInterSwId]; }
static void NComSetInterSwId(NComRxC *Com, int v) { Com->mInterSwId = COM_CK_VALIDITY(v, INTER_SW_ID_NAME); Com->mIsInterSwIdValid = 1; }
const char *NComGetHwConfigString(const NComRxC *Com) { return HARDWARE_CONFIG_TYPE_NAME[Com->mHwConfig]; }
static void NComSetHwConfig(NComRxC *Com, int v) { Com->mHwConfig = COM_CK_VALIDITY(v, HARDWARE_CONFIG_TYPE_NAME); Com->mIsHwConfigValid = 1; }

static void NComSetDiskSpace(NComRxC *Com, uint64_t v) { Com->mDiskSpace = v; Com->mIsDiskSpaceValid = 1; }
static void NComSetFileSize(NComRxC *Com, uint64_t v) { Com->mFileSize = v; Com->mIsFileSizeValid = 1; }
static void NComSetUpTime(NComRxC *Com, uint32_t v) { Com->mUpTime = v; Com->mIsUpTimeValid = 1; }
const char *NComGetDualPortRamStatusString(const NComRxC *Com) { return COM_DUAL_PORT_RAM_STATUS_NAME[Com->mDualPortRamStatus]; }
static void NComSetDualPortRamStatus(NComRxC *Com, int v) { Com->mDualPortRamStatus = COM_CK_VALIDITY(v, COM_DUAL_PORT_RAM_STATUS_NAME); Com->mIsDualPortRamStatusValid = 1; }

// IMU information

const char *NComGetUmacStatusString(const NComRxC *Com) { return COM_UMAC_STATUS_NAME[Com->mUmacStatus]; }
static void NComSetUmacStatus(NComRxC *Com, int v) { Com->mUmacStatus = COM_CK_VALIDITY(v, COM_UMAC_STATUS_NAME); Com->mIsUmacStatusValid = 1; }

// Global Navigation Satellite System (GNSS) information

static void NComSetGnssGpsEnabled(NComRxC *Com, int v) { Com->mGnssGpsEnabled = v; Com->mIsGnssGpsEnabledValid = 1; }
static void NComSetGnssGlonassEnabled(NComRxC *Com, int v) { Com->mGnssGlonassEnabled = v; Com->mIsGnssGlonassEnabledValid = 1; }
static void NComSetGnssGalileoEnabled(NComRxC *Com, int v) { Com->mGnssGalileoEnabled = v; Com->mIsGnssGalileoEnabledValid = 1; }
static void NComSetGnssBeiDouEnabled(NComRxC *Com, int v) { Com->mGnssBeiDouEnabled = v; Com->mIsGnssBeiDouEnabledValid = 1; }

static void NComSetPsrDiffEnabled(NComRxC *Com, int v) { Com->mPsrDiffEnabled = v; Com->mIsPsrDiffEnabledValid = 1; }
static void NComSetSBASEnabled(NComRxC *Com, int v) { Com->mSBASEnabled = v; Com->mIsSBASEnabledValid = 1; }
static void NComSetOmniVBSEnabled(NComRxC *Com, int v) { Com->mOmniVBSEnabled = v; Com->mIsOmniVBSEnabledValid = 1; }
static void NComSetOmniHPEnabled(NComRxC *Com, int v) { Com->mOmniHPEnabled = v; Com->mIsOmniHPEnabledValid = 1; }
static void NComSetL1DiffEnabled(NComRxC *Com, int v) { Com->mL1DiffEnabled = v; Com->mIsL1DiffEnabledValid = 1; }
static void NComSetL1L2DiffEnabled(NComRxC *Com, int v) { Com->mL1L2DiffEnabled = v; Com->mIsL1L2DiffEnabledValid = 1; }

static void NComSetRawRngEnabled(NComRxC *Com, int v) { Com->mRawRngEnabled = v; Com->mIsRawRngEnabledValid = 1; }
static void NComSetRawDopEnabled(NComRxC *Com, int v) { Com->mRawDopEnabled = v; Com->mIsRawDopEnabledValid = 1; }
static void NComSetRawL1Enabled (NComRxC *Com, int v) { Com->mRawL1Enabled = v; Com->mIsRawL1EnabledValid = 1; }
static void NComSetRawL2Enabled (NComRxC *Com, int v) { Com->mRawL2Enabled = v; Com->mIsRawL2EnabledValid = 1; }
static void NComSetRawL5Enabled (NComRxC *Com, int v) { Com->mRawL5Enabled = v; Com->mIsRawL5EnabledValid = 1; }

static void NComSetGPSPosMode(NComRxC *Com, int v) { Com->mGpsPosMode = COM_CK_VALIDITY(v, COM_GPS_XMODE_TYPE_NAME); Com->mIsGpsPosModeValid = 1; }
static void NComSetGPSVelMode(NComRxC *Com, int v) { Com->mGpsVelMode = COM_CK_VALIDITY(v, COM_GPS_XMODE_TYPE_NAME); Com->mIsGpsVelModeValid = 1; }
static void NComSetGPSAttMode(NComRxC *Com, int v) { Com->mGpsAttMode = COM_CK_VALIDITY(v, COM_GPS_XMODE_TYPE_NAME); Com->mIsGpsAttModeValid = 1; }

static void NComSetPDOP(NComRxC *Com, double v) { Com->mPDOP = v; Com->mIsPDOPValid = 1; }
static void NComSetHDOP(NComRxC *Com, double v) { Com->mHDOP = v; Com->mIsHDOPValid = 1; }
static void NComSetVDOP(NComRxC *Com, double v) { Com->mVDOP = v; Com->mIsVDOPValid = 1; }

static void NComSetGpsNumObs(NComRxC *Com, int v) { Com->mGpsNumObs = v; Com->mIsGpsNumObsValid = 1; }
static void NComSetUndulation(NComRxC *Com, double v) { Com->mUndulation = v; Com->mIsUndulationValid = 1; }

static void NComSetBaseStationId(NComRxC *Com, const char *v, int n) { strgrab(Com->mBaseStationId, BASE_STATION_ID_STRLEN, v, n); Com->mIsBaseStationIdValid = 1; }
static void NComSetGpsDiffAge(NComRxC *Com, double v) { Com->mGpsDiffAge = v; Com->mIsGpsDiffAgeValid = 1; }

// Heading computation status

const char *NComGetHeadQualityString(const NComRxC *Com) { return COM_HEADING_QUALITY_NAME[Com->mHeadQuality]; }
static void NComSetHeadQuality(NComRxC *Com, int v) { Com->mHeadQuality = COM_CK_VALIDITY(v, COM_HEADING_QUALITY_NAME); Com->mIsHeadQualityValid = 1; }
const char *NComGetHeadSearchTypeString(const NComRxC *Com) { return COM_HEADING_SEARCH_TYPE_NAME[Com->mHeadSearchType]; }
static void NComSetHeadSearchType(NComRxC *Com, int v) { Com->mHeadSearchType = COM_CK_VALIDITY(v, COM_HEADING_SEARCH_TYPE_NAME); Com->mIsHeadSearchTypeValid = 1; }
const char *NComGetHeadSearchStatusString(const NComRxC *Com) { return COM_HEADING_SEARCH_STATUS_NAME[Com->mHeadSearchStatus]; }
static void NComSetHeadSearchStatus(NComRxC *Com, int v) { Com->mHeadSearchStatus = COM_CK_VALIDITY(v, COM_HEADING_SEARCH_STATUS_NAME); Com->mIsHeadSearchStatusValid = 1; }
const char *NComGetHeadSearchReadyString(const NComRxC *Com) { return COM_HEADING_SEARCH_READY_NAME[Com->mHeadSearchReady]; }
static void NComSetHeadSearchReady(NComRxC *Com, int v) { Com->mHeadSearchReady = COM_CK_VALIDITY(v, COM_HEADING_SEARCH_READY_NAME); Com->mIsHeadSearchReadyValid = 1; }

static void NComSetHeadSearchInit(NComRxC *Com, int v) { Com->mHeadSearchInit = v; Com->mIsHeadSearchInitValid = 1; }
static void NComSetHeadSearchNum(NComRxC *Com, int v) { Com->mHeadSearchNum = v; Com->mIsHeadSearchNumValid = 1; }
static void NComSetHeadSearchTime(NComRxC *Com, int v) { Com->mHeadSearchTime = v; Com->mIsHeadSearchTimeValid = 1; }
static void NComSetHeadSearchConstr(NComRxC *Com, int v) { Com->mHeadSearchConstr = v; Com->mIsHeadSearchConstrValid = 1; }

static void NComSetHeadSearchMaster(NComRxC *Com, int v) { Com->mHeadSearchMaster = v; Com->mIsHeadSearchMasterValid = 1; }
static void NComSetHeadSearchSlave1(NComRxC *Com, int v) { Com->mHeadSearchSlave1 = v; Com->mIsHeadSearchSlave1Valid = 1; }
static void NComSetHeadSearchSlave2(NComRxC *Com, int v) { Com->mHeadSearchSlave2 = v; Com->mIsHeadSearchSlave2Valid = 1; }
static void NComSetHeadSearchSlave3(NComRxC *Com, int v) { Com->mHeadSearchSlave3 = v; Com->mIsHeadSearchSlave3Valid = 1; }

// OmniSTAR information

static void NComSetOmniStarSerial(NComRxC *Com, const char *v, int n) { strgrab(Com->mOmniStarSerial, OMNISTAR_SERIAL_STRLEN, v, n); Com->mIsOmniStarSerialValid = 1; }
static void NComSetOmniStarFreq(NComRxC *Com, double v) { Com->mOmniStarFreq = v; Com->mIsOmniStarFreqValid = 1; }
static void NComSetOmniStarSNR(NComRxC *Com, double v) { Com->mOmniStarSNR = v; Com->mIsOmniStarSNRValid = 1; }
static void NComSetOmniStarLockTime(NComRxC *Com, double v) { Com->mOmniStarLockTime = v; Com->mIsOmniStarLockTimeValid = 1; }

static void NComSetOmniStatusVbsExpired      (NComRxC *Com, int v) { Com->mOmniStatusVbsExpired = v; Com->mIsOmniStatusVbsExpiredValid = 1; }
static void NComSetOmniStatusVbsOutOfRegion  (NComRxC *Com, int v) { Com->mOmniStatusVbsOutOfRegion = v; Com->mIsOmniStatusVbsOutOfRegionValid = 1; }
static void NComSetOmniStatusVbsNoRemoteSites(NComRxC *Com, int v) { Com->mOmniStatusVbsNoRemoteSites = v; Com->mIsOmniStatusVbsNoRemoteSitesValid = 1; }

static void NComSetOmniStatusHpExpired      (NComRxC *Com, int v) { Com->mOmniStatusHpExpired = v; Com->mIsOmniStatusHpExpiredValid = 1; }
static void NComSetOmniStatusHpOutOfRegion  (NComRxC *Com, int v) { Com->mOmniStatusHpOutOfRegion = v; Com->mIsOmniStatusHpOutOfRegionValid = 1; }
static void NComSetOmniStatusHpNoRemoteSites(NComRxC *Com, int v) { Com->mOmniStatusHpNoRemoteSites = v; Com->mIsOmniStatusHpNoRemoteSitesValid = 1; }
static void NComSetOmniStatusHpNotConverged (NComRxC *Com, int v) { Com->mOmniStatusHpNotConverged = v; Com->mIsOmniStatusHpNotConvergedValid = 1; }
static void NComSetOmniStatusHpKeyInvalid   (NComRxC *Com, int v) { Com->mOmniStatusHpKeyInvalid = v; Com->mIsOmniStatusHpKeyInvalidValid = 1; }

// GPS successive rejected aiding updates

static void NComSetGPSPosReject(NComRxC *Com, uint32_t v) { Com->mGPSPosReject = v; Com->mIsGPSPosRejectValid = 1; }
static void NComSetGPSVelReject(NComRxC *Com, uint32_t v) { Com->mGPSVelReject = v; Com->mIsGPSVelRejectValid = 1; }
static void NComSetGPSAttReject(NComRxC *Com, uint32_t v) { Com->mGPSAttReject = v; Com->mIsGPSAttRejectValid = 1; }
static void NComSetGPSPosReject(NComRxC *Com, uint32_t v) { Com->mGPSPosReject = v; Com->mIsGPSPosRejectValid = 1; }
static void NComSetGPSVelReject(NComRxC *Com, uint32_t v) { Com->mGPSVelReject = v; Com->mIsGPSVelRejectValid = 1; }
static void NComSetGPSAttReject(NComRxC *Com, uint32_t v) { Com->mGPSAttReject = v; Com->mIsGPSAttRejectValid = 1; }

// *** Code Generation End - NComRxC Access Functions ***


//============================================================================================================
//! \brief Invalidation.

void NComInvalidate(NComRxC *Com)
{
	NComInternalInvalidate(Com->mInternal);

	//--------------------------------------------------------------------------------------------------------
	// Other structures.

	NComGpsInvalidate(Com->mGpsPrimary);
	NComGpsInvalidate(Com->mGpsSecondary);
	NComGpsInvalidate(Com->mGpsExternal);

	// *** Code Generation Begin - NComRxC Invalidate ***

	//--------------------------------------------------------------------------------------------------------
	// General information

	// Status

	Com->mIsOutputPacketTypeValid = 0; Com->mOutputPacketType = 0;
	Com->mIsInsNavModeValid = 0; Com->mInsNavMode = 0;

	// System information

	Com->mIsSerialNumberValid = 0; Com->mSerialNumber = 0;
	Com->mIsDevIdValid = 0; Com->mDevId[0] = '\0';

	Com->mIsOsVersion1Valid = 0; Com->mOsVersion1 = 0;
	Com->mIsOsVersion2Valid = 0; Com->mOsVersion2 = 0;
	Com->mIsOsVersion3Valid = 0; Com->mOsVersion3 = 0;
	Com->mIsOsScriptIdValid = 0; Com->mOsScriptId[0] = '\0';

	Com->mIsImuTypeValid = 0; Com->mImuType = 0;
	Com->mIsCpuPcbTypeValid = 0; Com->mCpuPcbType = 0;
	Com->mIsInterPcbTypeValid = 0; Com->mInterPcbType = 0;
	Com->mIsFrontPcbTypeValid = 0; Com->mFrontPcbType = 0;
	Com->mIsInterSwIdValid = 0; Com->mInterSwId = 0;
	Com->mIsHwConfigValid = 0; Com->mHwConfig = 0;

	Com->mIsDiskSpaceValid = 0; Com->mDiskSpace = 0;
	Com->mIsFileSizeValid = 0; Com->mFileSize = 0;
	Com->mIsUpTimeValid = 0; Com->mUpTime = 0;
	Com->mIsDualPortRamStatusValid = 0; Com->mDualPortRamStatus = 0;

	// IMU information

	Com->mIsUmacStatusValid = 0; Com->mUmacStatus = 0;

	// Global Navigation Satellite System (GNSS) information

	Com->mIsGnssGpsEnabledValid = 0; Com->mGnssGpsEnabled = 0;
	Com->mIsGnssGlonassEnabledValid = 0; Com->mGnssGlonassEnabled = 0;
	Com->mIsGnssGalileoEnabledValid = 0; Com->mGnssGalileoEnabled = 0;
	Com->mIsGnssBeiDouEnabledValid = 0; Com->mGnssBeiDouEnabled = 0;

	Com->mIsPsrDiffEnabledValid = 0; Com->mPsrDiffEnabled = 0;
	Com->mIsSBASEnabledValid = 0; Com->mSBASEnabled = 0;
	Com->mIsOmniVBSEnabledValid = 0; Com->mOmniVBSEnabled = 0;
	Com->mIsOmniHPEnabledValid = 0; Com->mOmniHPEnabled = 0;
	Com->mIsL1DiffEnabledValid = 0; Com->mL1DiffEnabled = 0;
	Com->mIsL1L2DiffEnabledValid = 0; Com->mL1L2DiffEnabled = 0;
	}
	else
	{
		Com->mIsGnssGpsEnabledValid = 0;
		Com->mIsGnssGlonassEnabledValid = 0;
		Com->mIsGnssGalileoEnabledValid = 0;
		Com->mIsGnssBeiDouEnabledValid = 0;

		Com->mIsPsrDiffEnabledValid = 0;
		Com->mIsSBASEnabledValid = 0;
		Com->mIsOmniVBSEnabledValid = 0;
		Com->mIsOmniHPEnabledValid = 0;
		Com->mIsL1DiffEnabledValid = 0;
		Com->mIsL1L2DiffEnabledValid = 0;
	}
}


//============================================================================================================
//! \brief Constructor.

NComRxC *NComCreateNComRxC()
{
	NComRxC *Com = (NComRxC *)calloc(1, sizeof(NComRxC));

	if (Com == NULL) return NULL;

	Com->mInternal     = NComInternalCreate();
	Com->mGpsPrimary   = NComGpsCreate();
	Com->mGpsSecondary = NComGpsCreate();
	Com->mGpsExternal  = NComGpsCreate();

	if (Com->mInternal == NULL || Com->mGpsPrimary == NULL || Com->mGpsSecondary == NULL || Com->mGpsExternal == NULL)
	{
		NComDestroyNComRxC(Com);
		return NULL;
	};

	NComInvalidate(Com);
	return Com;
}


//============================================================================================================
//! \brief Destructor.

void NComDestroyNComRxC(NComRxC *Com)
{
	if (Com != NULL)
	{
		// Free the the internal space
		NComInternalDestroy(Com->mInternal);

		// Free the the GPS information
		NComGpsDestroy(Com->mGpsPrimary);
		NComGpsDestroy(Com->mGpsSecondary);
		NComGpsDestroy(Com->mGpsExternal);

		// Free the Com
		free(Com);
	}
}


//============================================================================================================
//! \brief Copy for NCom structure.

void NComCopy(NComRxC *ComDestination, const NComRxC *ComSource)
{
	// Keep track of pointers before mem copy.

	NComRxCGps *xGpsPrimary   = ComDestination->mGpsPrimary;
	NComRxCGps *xGpsSecondary = ComDestination->mGpsSecondary;
	NComRxCGps *xGpsExternal  = ComDestination->mGpsExternal;

	NComRxCInternal *xInternal = ComDestination->mInternal;

	// Copy this structure

	memcpy(ComDestination, ComSource, sizeof(NComRxC));

	// Recover pointers

	ComDestination->mGpsPrimary   = xGpsPrimary;
	ComDestination->mGpsSecondary = xGpsSecondary;
	ComDestination->mGpsExternal  = xGpsExternal;

	ComDestination->mInternal = xInternal;

	// Copy pointed to structures

	NComGpsCopy(ComDestination->mGpsPrimary,   ComSource->mGpsPrimary);
	NComGpsCopy(ComDestination->mGpsSecondary, ComSource->mGpsSecondary);
	NComGpsCopy(ComDestination->mGpsExternal,  ComSource->mGpsExternal);

	NComInternalCopy(ComDestination->mInternal, ComSource->mInternal);
}


 //============================================================================================================
//! \brief Parse the incoming character for the packet.

ComResponse NComNewChar(NComRxC *Com, unsigned char c)
{
	return NComNewChars(Com, &c, 1);
}


//============================================================================================================
//! \brief Parse the incoming characters for the packet.

ComResponse NComNewChars(NComRxC *Com, const unsigned char *data, int num)
{
	int extra;
	ParsedPacketType pktStatus = PARSED_PACKET_INCOMPLETE;
	int offset = 0;

	NComRxCInternal *ComI = Com->mInternal;

	// If a processed packet is still in the buffer, remove it
	if (ComI->mPktProcessed)
	{
		RemoveFromBuffer(ComI, NOUTPUT_PACKET_LENGTH);
	}

	// If we have new data to add, and the buffer is full, get rid of
	// enough characters to accommodate the new characters
	// This is expensive, but should not happen often!
	if ((extra = ComI->mCurLen + num - NCOMRX_BUFFER_SIZE) > 0)
	{
		ComI->mCurLen = NCOMRX_BUFFER_SIZE - num;

		// Check that the amount of data passed is not beyond acceptable limits
		if (ComI->mCurLen > 0)
		{
			memmove(ComI->mCurPkt, ComI->mCurPkt + extra, ComI->mCurLen);
		}
		else if (ComI->mCurLen < 0) // (num > NCOMRX_BUFFER_SIZE) - i.e. too much data for the buffer
		{
			num += ComI->mCurLen; // Only accept as much data as the buffer size
			ComI->mCurLen = 0; // All previous data is lost
		}

		ComI->mSkippedChars += extra;
	}

	// Add the new characters (if available)
	if (num > 0)
	{
		memcpy(ComI->mCurPkt + ComI->mCurLen, data, num);
		ComI->mCurLen   += num;
		ComI->mNumChars += num;
	}

	// Check to see if we have a complete packet
	while (offset < ComI->mCurLen)
	{
		// Is there a valid packet at current offset?
		pktStatus = PktStatus(ComI->mCurLen - offset, ComI->mCurPkt + offset);

		// If we have found valid data
		if (pktStatus != PARSED_PACKET_CORRUPT)
			break; // Stop searching
		else
			offset++;
	}

	// If some corrupt data was found, realign the data
	if (offset > 0)
	{
		RemoveFromBuffer(ComI, offset);
	}

	// If a complete packet has been detected
	if (pktStatus == PARSED_PACKET_VALID)
	{
		// Extract data from the packet and update the navigation quantities
		UpdateNav(Com);

		// No packets so far then ignore the errors
		if (ComI->mNumPackets == 0)
			ComI->mSkippedChars = 0;

		// We've done a packet
		ComI->mNumPackets++;
		ComI->mPktProcessed = 1;

		// Indicate that a complete packet has been received
		return COM_NEW_UPDATE;
	}
	else
	{
		// Indicate that no complete packet has been received
		return COM_NO_UPDATE;
	}
}


//============================================================================================================
//! \brief Number of characters.

uint64_t NComNumChars(const NComRxC *Com)
{
	return (Com != NULL && Com->mInternal != NULL) ? Com->mInternal->mNumChars : 0;
}


//============================================================================================================
//! \brief Skipped characters.

uint64_t NComSkippedChars(const NComRxC *Com)
{
	return (Com != NULL && Com->mInternal != NULL) ? Com->mInternal->mSkippedChars : 0;
}


//============================================================================================================
//! \brief Number of packets.

uint64_t NComNumPackets(const NComRxC *Com)
{
	return (Com != NULL && Com->mInternal != NULL) ? Com->mInternal->mNumPackets : 0;
}


//============================================================================================================
//! \brief Get the size of the current NCOM packet.

unsigned int NComGetCurrentPacketSize(const NComRxC *Com)
{
	return (unsigned int)(NCOM_PACKET_LENGTH);
}


 //============================================================================================================
//! \brief Get the data of the current NCOM packet.

const unsigned char *NComGetCurrentPacketData(const NComRxC *Com)
{
	if (Com && Com->mInternal)
	{
		return Com->mInternal->mCurPkt;
	}

	return NULL;
}


//============================================================================================================
//! \brief Get the current status channel number.

int NComGetCurrentStatusChannel(const NComRxC *Com)
{
	if (Com && Com->mInternal)
	{
		return Com->mInternal->mCurChannel;
	}

	return -1;
}


//============================================================================================================
//! \brief Get the size of the NCOM status portion of the current packet.

unsigned int NComGetCurrentStatusPacketSize(const NComRxC *Com)
{
	return (unsigned int)(NCOM_STATUS_PACKET_LENGTH);
}

//============================================================================================================
//! \brief Get the data of the NCOM status portion of the current packet.

const unsigned char *NComGetCurrentStatusPacketData(const NComRxC *Com)
{
	if (Com && Com->mInternal)
	{
		return Com->mInternal->mCurStatus;
	}

	return NULL;
}


//============================================================================================================
//! \brief Update innovation age.

void NComUpdateInnAge(NComRxC *Com)
{
	if (Com->mInnPosXAge     < MAX_INN_AGE) Com->mInnPosXAge++;
	if (Com->mInnPosYAge     < MAX_INN_AGE) Com->mInnPosYAge++;
	if (Com->mInnPosZAge     < MAX_INN_AGE) Com->mInnPosZAge++;

	if (Com->mInnVelXAge     < MAX_INN_AGE) Com->mInnVelXAge++;
	if (Com->mInnVelYAge     < MAX_INN_AGE) Com->mInnVelYAge++;
	if (Com->mInnVelZAge     < MAX_INN_AGE) Com->mInnVelZAge++;

	if (Com->mInnHeadingAge  < MAX_INN_AGE) Com->mInnHeadingAge++;
	if (Com->mInnPitchAge    < MAX_INN_AGE) Com->mInnPitchAge++;

	if (Com->mInnZeroVelXAge < MAX_INN_AGE) Com->mInnZeroVelXAge++;
	if (Com->mInnZeroVelYAge < MAX_INN_AGE) Com->mInnZeroVelYAge++;
	if (Com->mInnZeroVelZAge < MAX_INN_AGE) Com->mInnZeroVelZAge++;

	if (Com->mInnNoSlipHAge  < MAX_INN_AGE) Com->mInnNoSlipHAge++;
	if (Com->mInnHeadingHAge < MAX_INN_AGE) Com->mInnHeadingHAge++;
	if (Com->mInnWSpeedAge   < MAX_INN_AGE) Com->mInnWSpeedAge++;
}


//============================================================================================================
//! \brief Interpolate between packets.
//!
//! \todo This needs to be reviewed in light of all available data members.

void NComInterpolate(NComRxC *Com, double a, const NComRxC *A, double b, const NComRxC *B)
{
	NComInvalidate(Com);

	if (A->mIsTimeValid && B->mIsTimeValid) NComSetTime(Com, a * A->mTime + b * B->mTime);
	if (A->mIsLatValid  && B->mIsLatValid ) NComSetLat (Com, a * A->mLat  + b * B->mLat );
	if (A->mIsLonValid  && B->mIsLonValid ) NComSetLon (Com, a * A->mLon  + b * B->mLon );
	if (A->mIsAltValid  && B->mIsAltValid ) NComSetAlt (Com, a * A->mAlt  + b * B->mAlt );
	if (A->mIsVnValid   && B->mIsVnValid  ) NComSetVn  (Com, a * A->mVn   + b * B->mVn  );
	if (A->mIsVeValid   && B->mIsVeValid  ) NComSetVe  (Com, a * A->mVe   + b * B->mVe  );
	if (A->mIsVdValid   && B->mIsVdValid  ) NComSetVd  (Com, a * A->mVd   + b * B->mVd  );
	if (A->mIsHeadingValid && B->mIsHeadingValid)
	{
		double d = A->mHeading - B->mHeading;
		if (d > 180.0)
			d -= 360.0;
		else if (d < -180.0)
			d += 360.0;
		d = A->mHeading + b * d;
		if (d < 0.0)
			d += 360.0;
		else if (d > 360.0)
			d -= 360.0;
		NComSetHeading(Com, d);
	}

	if (A->mIsPitchValid   && B->mIsPitchValid  ) NComSetPitch  (Com, a * A->mPitch   + b * B->mPitch  );
	if (A->mIsRollValid    && B->mIsRollValid   ) NComSetRoll   (Com, a * A->mRoll    + b * B->mRoll   );
	if (A->mIsDist2dValid  && B->mIsDist2dValid ) NComSetDist2d (Com, a * A->mDist2d  + b * B->mDist2d );
	if (A->mIsDist3dValid  && B->mIsDist3dValid ) NComSetDist3d (Com, a * A->mDist3d  + b * B->mDist3d );
	if (A->mIsVfValid      && B->mIsVfValid     ) NComSetVf     (Com, a * A->mVf      + b * B->mVf     );
	if (A->mIsVlValid      && B->mIsVlValid     ) NComSetVl     (Com, a * A->mVl      + b * B->mVl     );
	if (A->mIsSpeed2dValid && B->mIsSpeed2dValid) NComSetSpeed2d(Com, a * A->mSpeed2d + b * B->mSpeed2d);
	if (A->mIsSpeed3dValid && B->mIsSpeed3dValid) NComSetSpeed3d(Com, a * A->mSpeed3d + b * B->mSpeed3d);
	if (A->mIsAxValid      && B->mIsAxValid     ) NComSetAx     (Com, a * A->mAx      + b * B->mAx     );
	if (A->mIsAyValid      && B->mIsAyValid     ) NComSetAy     (Com, a * A->mAy      + b * B->mAy     );
	if (A->mIsAzValid      && B->mIsAzValid     ) NComSetAz     (Com, a * A->mAz      + b * B->mAz     );
	if (A->mIsAfValid      && B->mIsAfValid     ) NComSetAf     (Com, a * A->mAf      + b * B->mAf     );
	if (A->mIsAlValid      && B->mIsAlValid     ) NComSetAl     (Com, a * A->mAl      + b * B->mAl     );
	if (A->mIsAdValid      && B->mIsAdValid     ) NComSetAd     (Com, a * A->mAd      + b * B->mAd     );
	if (A->mIsWxValid      && B->mIsWxValid     ) NComSetWx     (Com, a * A->mWx      + b * B->mWx     );
	if (A->mIsWyValid      && B->mIsWyValid     ) NComSetWy     (Com, a * A->mWy      + b * B->mWy     );
	if (A->mIsWzValid      && B->mIsWzValid     ) NComSetWz     (Com, a * A->mWz      + b * B->mWz     );
	if (A->mIsWfValid      && B->mIsWfValid     ) NComSetWf     (Com, a * A->mWf      + b * B->mWf     );
	if (A->mIsWlValid      && B->mIsWlValid     ) NComSetWl     (Com, a * A->mWl      + b * B->mWl     );
	if (A->mIsWdValid      && B->mIsWdValid     ) NComSetWd     (Com, a * A->mWd      + b * B->mWd     );
	if (A->mIsYxValid      && B->mIsYxValid     ) NComSetYx     (Com, a * A->mYx      + b * B->mYx     );
	if (A->mIsYyValid      && B->mIsYyValid     ) NComSetYy     (Com, a * A->mYy      + b * B->mYy     );
	if (A->mIsYzValid      && B->mIsYzValid     ) NComSetYz     (Com, a * A->mYz      + b * B->mYz     );
	if (A->mIsYfValid      && B->mIsYfValid     ) NComSetYf     (Com, a * A->mYf      + b * B->mYf     );
	if (A->mIsYlValid      && B->mIsYlValid     ) NComSetYl     (Com, a * A->mYl      + b * B->mYl     );
	if (A->mIsYdValid      && B->mIsYdValid     ) NComSetYd     (Com, a * A->mYd      + b * B->mYd     );

	if (A->mIsSlipValid && B->mIsSlipValid)
	{
		double d = A->mSlip - B->mSlip;
		if (d > 180.0)
			d -= 360.0;
		else if (d < -180.0)
			d += 360.0;
		d = A->mSlip + b * d;
		if (d < 0.0)
			d += 360.0;
		else if (d > 360.0)
			d -= 360.0;
		NComSetSlip(Com, d);
	}

	if (A->mIsFiltAxValid  && B->mIsFiltAxValid )
	{
		NComSetFiltAx(Com, a * A->mFiltAx  + b * B->mFiltAx );
		NComSetFiltAy(Com, a * A->mFiltAy  + b * B->mFiltAy );
		NComSetFiltAz(Com, a * A->mFiltAz  + b * B->mFiltAz );
	}
	else
	{
		FilteredOutputsCompute(Com);
	}

	if (Com->mInsNavMode > NAVIGATION_STATUS_INIT)
	{
		if (!ComI->mMatrixHold)
		{
			RotateOutputsCompute(Com);    // Rotated quantities
			Iso8855OutputsCompute(Com);   // ISO quantities
		}

		SpeedSlipCompute(Com);            // Compute speed and slip
		DistanceCompute(Com, trig);       // Compute distance travelled
	}
}


//============================================================================================================
//! \brief Invalidate all variables that may be computed by rotate outputs.

static void RotateOutputsInvalidate(NComRxC *Com)
{
	NComRxCInternal *ComI = Com->mInternal;

	Com->mIsAfValid    = Com->mIsAlValid    = Com->mIsAdValid    = 0;  // Acceleration.
	Com->mIsWfValid    = Com->mIsWlValid    = Com->mIsWdValid    = 0;  // Angular rate.
	Com->mIsVfValid    = Com->mIsVlValid                         = 0;  // Velocity.
	Com->mIsYfValid    = Com->mIsYlValid    = Com->mIsYdValid    = 0;  // Angular acceleration.

	Com->mIsFiltAfValid = Com->mIsFiltAlValid = Com->mIsFiltAdValid = 0;  // Filtered acceleration.
	Com->mIsFiltYfValid = Com->mIsFiltYlValid = Com->mIsFiltYdValid = 0;  // Filtered angular acceleration.

	ComI->C_on_valid = 0;  // Rotation from output-frame to navigation-frame (all angles).
	ComI->C_oh_valid = 0;  // Rotation from output-frame to horizontal-frame (roll and pitch angles).
	ComI->C_hn_valid = 0;  // Rotation from horizontal-frame to navigation-frame (heading angle).
}


//============================================================================================================
//! \brief Rotate outputs.

static void RotateOutputsCompute(NComRxC *Com)
{
	if (Com->mIsHeadingValid && Com->mIsPitchValid && Com->mIsRollValid)
	{
		NComRxCInternal *ComI = Com->mInternal;

		MatFillR(&ComI->E,  3, 1, Com->mHeading * DEG2RAD, Com->mPitch * DEG2RAD, Com->mRoll * DEG2RAD);

		Euler2DirCos (&ComI->C_on, &ComI->E); ComI->C_on_valid = 1;  // Rotation from output-frame to navigation-frame (all angles).
		Euler2DirCos2(&ComI->C_oh, &ComI->E); ComI->C_oh_valid = 1;  // Rotation from output-frame to horizontal-frame (roll and pitch angles).
		Euler2DirCosH(&ComI->C_hn, &ComI->E); ComI->C_hn_valid = 1;  // Rotation from horizontal-frame to navigation-frame (heading angle).

		// Acceleration in FLD frame.
		if (Com->mIsAxValid && Com->mIsAyValid && Com->mIsAzValid)
		{
			MatFillR(&ComI->Ab, 3, 1, Com->mAx, Com->mAy, Com->mAz);

			Lib__apply_imu_b2n_corrections_a(&tmp_nbn, &ComI->Ab, &g_n, &cora_n, &ComI->C_on);

			//ISO earth-fixed system velocity.

			// A_ni = C_noni A_no
			NComSetIsoAnX(Com, e(&tmp_nbn, 1, 0));
			NComSetIsoAnY(Com, e(&tmp_nbn, 0, 0));
			NComSetIsoAnZ(Com, -e(&tmp_nbn, 2, 0));


			// // ISO intermediate system velocity.
			if (ComI->C_hn_valid)
			{
				// A_hi = C_hohi A_ho, A_ho = C_noho A_no
				MatMultRAtB(&tmp_nbh, &ComI->C_hn, &tmp_nbn);
				NComSetIsoAhX(Com, e(&tmp_nbh, 0, 0));
				NComSetIsoAhY(Com, -e(&tmp_nbh, 1, 0));
				NComSetIsoAhZ(Com, -e(&tmp_nbh, 2, 0));
			}

			// ISO vehicle system velocity.
			if (ComI->C_oh_valid)
			{
				// A_oi = C_oooi A_oo, A_oo = C_hooo A_ho
				MatMultRAtB(&tmp_nbb, &ComI->C_oh, &tmp_nbh);
				NComSetIsoAoX(Com, e(&tmp_nbb, 0, 0));
				NComSetIsoAoY(Com, -e(&tmp_nbb, 1, 0));
				NComSetIsoAoZ(Com, -e(&tmp_nbb, 2, 0));
			}
		}

		// === Angular Velocity (W) ===

		if (Com->mIsWxValid && Com->mIsWyValid && Com->mIsWzValid)
		{
			MatFillR(&tmp_nbb, 3, 1, Com->mWx, Com->mWy, Com->mWz);

			// ISO vehicle system angular velocity.
				// W_oi = C_oooi W_oo
				NComSetIsoWoX(Com, e(&tmp_nbb, 0, 0));
				NComSetIsoWoY(Com, -e(&tmp_nbb, 1, 0));
				NComSetIsoWoZ(Com, -e(&tmp_nbb, 2, 0));


			// ISO intermediate system angular velocity.
			if (ComI->C_oh_valid)
			{
				// W_hi = C_hohi W_ho, W_ho = C_ooho W_oo
				MatMultRAB(&tmp_nbh, &ComI->C_oh, &tmp_nbb);
				NComSetIsoWhX(Com, e(&tmp_nbh, 0, 0));
				NComSetIsoWhY(Com, -e(&tmp_nbh, 1, 0));
				NComSetIsoWhZ(Com, -e(&tmp_nbh, 2, 0));
			}

			// ISO earth-fixed system angular velocity.
			if (ComI->C_hn_valid)
			{
				// W_ni = C_noni W_no, W_no = C_hono W_ho
				MatMultRAB(&tmp_nbn, &ComI->C_hn, &tmp_nbh);
				NComSetIsoWnX(Com, e(&tmp_nbn, 1, 0));
				NComSetIsoWnY(Com, e(&tmp_nbn, 0, 0));
				NComSetIsoWnZ(Com, -e(&tmp_nbn, 2, 0));
			}
		}

		// === Angular Acceleration (Y) ===

		if (Com->mIsYxValid && Com->mIsYyValid && Com->mIsYzValid)
		{
			MatFillR(&tmp_nbb, 3, 1, Com->mYx, Com->mYy, Com->mYz);

			// ISO vehicle system angular velocity.

				// Y_oi = C_oooi Y_oo
				NComSetIsoYoX(Com, e(&tmp_nbb, 0, 0));
				NComSetIsoYoY(Com, -e(&tmp_nbb, 1, 0));
				NComSetIsoYoZ(Com, -e(&tmp_nbb, 2, 0));


				// ISO intermediate system angular velocity.
				if (ComI->C_oh_valid)
				{
					// Y_hi = C_hohi Y_ho, Y_ho = C_ooho Y_oo
					MatMultRAB(&tmp_nbh, &ComI->C_oh, &tmp_nbb);
					NComSetIsoYhX(Com, e(&tmp_nbh, 0, 0));
					NComSetIsoYhY(Com, -e(&tmp_nbh, 1, 0));
					NComSetIsoYhZ(Com, -e(&tmp_nbh, 2, 0));
				}

				// ISO earth-fixed system angular velocity.
				if (ComI->C_hn_valid)
				{
					// Y_ni = C_noni Y_no, Y_no = C_hono Y_ho
					MatMultRAB(&tmp_nbn, &ComI->C_hn, &tmp_nbh);
					NComSetIsoYnX(Com, e(&tmp_nbn, 1, 0));
					NComSetIsoYnY(Com, e(&tmp_nbn, 0, 0));
					NComSetIsoYnZ(Com, -e(&tmp_nbn, 2, 0));
				}
		}


		if (Com->mIsFiltYxValid && Com->mIsFiltYyValid && Com->mIsFiltYzValid)
		{
			MatFillR(&tmp_nbb, 3, 1, Com->mFiltYx, Com->mFiltYy,Com->mFiltYz);

			// ISO vehicle system angular velocity.
			// Y_oi = C_oooi Y_oo
			NComSetFiltIsoYoX(Com, e(&tmp_nbb, 0, 0));
			NComSetFiltIsoYoY(Com, -e(&tmp_nbb, 1, 0));
			NComSetFiltIsoYoZ(Com, -e(&tmp_nbb, 2, 0));


			// ISO intermediate system angular velocity.
			if (ComI->C_oh_valid)
			{
				// Y_hi = C_hohi Y_ho, Y_ho = C_ooho Y_oo
				MatMultRAB(&tmp_nbh, &ComI->C_oh, &tmp_nbb);
				NComSetFiltIsoYhX(Com, e(&tmp_nbh, 0, 0));
				NComSetFiltIsoYhY(Com, -e(&tmp_nbh, 1, 0));
				NComSetFiltIsoYhZ(Com, -e(&tmp_nbh, 2, 0));
			}

			// ISO earth-fixed system angular velocity.
			if (ComI->C_hn_valid)
			{
				// Y_ni = C_noni Y_no, Y_no = C_hono Y_ho
				MatMultRAB(&tmp_nbn, &ComI->C_hn, &tmp_nbh);
				NComSetFiltIsoYnX(Com, e(&tmp_nbn, 1, 0));
				NComSetFiltIsoYnY(Com, e(&tmp_nbn, 0, 0));
				NComSetFiltIsoYnZ(Com, -e(&tmp_nbn, 2, 0));
			}
		}
	}
}


//============================================================================================================
//! \brief 50. Information sent to the command decoder.

static void DecodeExtra50(NComRxC *Com)
{
	const unsigned char *mCurStatus = Com->mInternal->mCurStatus;

	NComSetCmdChars       (Com, incr_2_byte_LE_to_uint32(mCurStatus+0, Com->mCmdChars));
	NComSetCmdPkts        (Com, incr_2_byte_LE_to_uint32(mCurStatus+2, Com->mCmdPkts));
	NComSetCmdCharsSkipped(Com, incr_2_byte_LE_to_uint32(mCurStatus+4, Com->mCmdCharsSkipped));
	NComSetCmdErrors      (Com, incr_2_byte_LE_to_uint32(mCurStatus+6, Com->mCmdErrors));
}


//============================================================================================================


//============================================================================================================


//============================================================================================================


//============================================================================================================
//! \brief 55. Information about the primary GPS receiver.

static void DecodeExtra55(NComRxC *Com)
{
	DecodeExtraGpsStatus(Com->mInternal->mCurStatus, Com->mGpsPrimary);
}


//============================================================================================================
//! \brief 56. Information about the secondary GPS receiver.

static void DecodeExtra56(NComRxC *Com)
{
	DecodeExtraGpsStatus(Com->mInternal->mCurStatus, Com->mGpsSecondary);
}


//============================================================================================================
//! \brief 57. Position estimate of the primary GPS antenna (extended range).

static void DecodeExtra57(NComRxC *Com)
{
	const unsigned char *mCurStatus = Com->mInternal->mCurStatus;

	if ((mCurStatus[6] < NCOM_COUNT_TOO_OLD) && (mCurStatus[7] != 0x00))
	{
		double sf;

		if (mCurStatus[7] == 0xFF) // saturation condition
			sf = 1.0;              // to match standard saturation values
		else
			sf = mCurStatus[7];

		NComSetGAPx(Com, ((double) cast_2_byte_LE_to_int16(mCurStatus+0)) * sf * GPSPOS2M);
		NComSetGAPy(Com, ((double) cast_2_byte_LE_to_int16(mCurStatus+2)) * sf * GPSPOS2M);
		NComSetGAPz(Com, ((double) cast_2_byte_LE_to_int16(mCurStatus+4)) * sf * GPSPOS2M);
	}
	else
	{
		Com->mIsGAPxValid = 0;
		Com->mIsGAPyValid = 0;
		Com->mIsGAPzValid = 0;
	}
}


//============================================================================================================


//============================================================================================================
//! \brief 59. IMU decoding status.

static void DecodeExtra59(NComRxC *Com)
{
	const unsigned char *mCurStatus = Com->mInternal->mCurStatus;

	NComSetImuMissedPkts(Com, incr_2_byte_LE_to_uint32(mCurStatus+0, Com->mImuMissedPkts));
	NComSetImuResetCount(Com, incr_1_byte_LE_to_uint32(mCurStatus+2, Com->mImuResetCount));
	NComSetImuErrorCount(Com, incr_1_byte_LE_to_uint32(mCurStatus+3, Com->mImuErrorCount));
}


//============================================================================================================
//! \brief 60. Definition of the surface angles.

static void DecodeExtra60(NComRxC *Com)
{
	const unsigned char *mCurStatus = Com->mInternal->mCurStatus;

	if (mCurStatus[6] == 0x00)
	{
		NComSetNed2SurfHeading(Com, ((double) cast_2_byte_LE_to_int16(mCurStatus+0)) * GPSATT2RAD * RAD2DEG);
		NComSetNed2SurfPitch  (Com, ((double) cast_2_byte_LE_to_int16(mCurStatus+2)) * GPSATT2RAD * RAD2DEG);
		NComSetNed2SurfRoll   (Com, ((double) cast_2_byte_LE_to_int16(mCurStatus+4)) * GPSATT2RAD * RAD2DEG);
	}
	else
	{
		Com->mIsNed2SurfHeadingValid = 0;
		Com->mIsNed2SurfPitchValid   = 0;
		Com->mIsNed2SurfRollValid    = 0;
	}
}


//============================================================================================================
//! \brief 61. Received information about external GPS receiver.

static void DecodeExtra61(NComRxC *Com)
{
	DecodeExtraGpsReceived(Com->mInternal->mCurStatus, Com->mGpsExternal);
}


//============================================================================================================
//! \brief 62. Measurement age.

static void DecodeExtra62(NComRxC *Com)
{
	const unsigned char *mCurStatus = Com->mInternal->mCurStatus;

	if (mCurStatus[6] < NCOM_COUNT_TOO_OLD)
	{
		// Position
		NComSetInnPosXAge(Com, cast_1_byte_LE_to_int8 (mCurStatus+0));
		NComSetInnPosYAge(Com, cast_1_byte_LE_to_int8 (mCurStatus+1));
		NComSetInnPosZAge(Com, cast_1_byte_LE_to_int8 (mCurStatus+2));

		// Velocity
		NComSetInnVelXAge(Com, cast_1_byte_LE_to_int8 (mCurStatus+3));
		NComSetInnVelYAge(Com, cast_1_byte_LE_to_int8 (mCurStatus+4));
		NComSetInnVelZAge(Com, cast_1_byte_LE_to_int8 (mCurStatus+5));

		// Heading
		NComSetInnHeadingAge(Com, cast_1_byte_LE_to_int8 (mCurStatus+6));
		NComSetInnPitchAge(Com,   cast_1_byte_LE_to_int8 (mCurStatus+7));
		NComSetInnRollAge(Com,    cast_1_byte_LE_to_int8 (mCurStatus+8));

		// Zero velocity
		NComSetInnZeroVelXAge(Com, cast_1_byte_LE_to_int8 (mCurStatus+9));
		NComSetInnZeroVelYAge(Com, cast_1_byte_LE_to_int8 (mCurStatus+10));
		NComSetInnZeroVelZAge(Com, cast_1_byte_LE_to_int8 (mCurStatus+11));

		// No slip heading
		NComSetInnNoSlipHAge(Com, cast_1_byte_LE_to_int8 (mCurStatus+12));
		NComSetInnHeadingHAge(Com, cast_1_byte_LE_to_int8 (mCurStatus+13));
		NComSetInnWSpeedAge(Com,  cast_1_byte_LE_to_int8 (mCurStatus+14));
	}
	else
	{
		Com->mInnPosXAge    = MAX_INN_AGE;
		Com->mInnPosYAge    = MAX_INN_AGE;
		Com->mInnPosZAge    = MAX_INN_AGE;
		Com->mInnVelXAge    = MAX_INN_AGE;
		Com->mInnVelYAge    = MAX_INN_AGE;
		Com->mInnVelZAge    = MAX_INN_AGE;
		Com->mInnHeadingAge = MAX_INN_AGE;
		Com->mInnPitchAge    = MAX_INN_AGE;
		Com->mInnRollAge     = MAX_INN_AGE;
		Com->mInnZeroVelXAge = MAX_INN_AGE;
		Com->mInnZeroVelYAge = MAX_INN_AGE;
		Com->mInnZeroVelZAge = MAX_INN_AGE;
		Com->mInnNoSlipHAge  = MAX_INN_AGE;
		Com->mInnHeadingHAge = MAX_INN_AGE;
		Com->mInnWSpeedAge   = MAX_INN_AGE;
	}
}


//============================================================================================================
//! \brief 63. Time since last IMU self-test.

static void DecodeExtra63(NComRxC *Com)
{
	const unsigned char *mCurStatus = Com->mInternal->mCurStatus;

	if (mCurStatus[6] < NCOM_COUNT_TOO_OLD)
	{
		NComSetImuSelfTestTime(Com, cast_2_byte_LE_to_real32(mCurStatus+0) * TIME2SEC);

	}
	else
	{
		Com->mIsImuSelfTestTimeValid = 0;
	}
}


//============================================================================================================
//! \brief 72. Accelerometer scale factor.

static void DecodeExtra72(NComRxC *Com)
{
	const unsigned char *mCurStatus = Com->mInternal->mCurStatus;

	if (mCurStatus[6] < NCOM_COUNT_TOO_OLD)
	{
		NComSetAxSf(Com, cast_2_byte_LE_to_int16(mCurStatus+0) * ASFACTOR);
		NComSetAySf(Com, cast_2_byte_LE_to_int16(mCurStatus+2) * ASFACTOR);
		NComSetAzSf(Com, cast_2_byte_LE_to_int16(mCurStatus+4) * ASFACTOR);
	}
	else
	{
		Com->mIsAxSfValid = 0;
		Com->mIsAySfValid = 0;
		Com->mIsAzSfValid = 0;
	}
}


//============================================================================================================
//! \brief 73. Accelerometer scale factor accuracy.

static void DecodeExtra73(NComRxC *Com)
{
	const unsigned char *mCurStatus = Com->mInternal->mCurStatus;

	if (mCurStatus[6] < NCOM_COUNT_TOO_OLD)
	{
		NComSetAxSfAcc(Com, cast_2_byte_LE_to_uint16(mCurStatus+0) * ASAFACTOR);
		NComSetAySfAcc(Com, cast_2_byte_LE_to_uint16(mCurStatus+2) * ASAFACTOR);
		NComSetAzSfAcc(Com, cast_2_byte_LE_to_uint16(mCurStatus+4) * ASAFACTOR);
	}
	else
	{
		Com->mIsAxSfAccValid = 0;
		Com->mIsAySfAccValid = 0;
		Com->mIsAzSfAccValid = 0;
	}
}

