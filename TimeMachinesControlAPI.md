> **Locator Protocol API version 2.3**
>
> July 2025

#  1 Locator Data Query

+-----------------------------------+-----------------------------------+
| > **Product**                     | **Firmware Supporting**           |
+===================================+===================================+
| > TM1000                          | 2.6                               |
+-----------------------------------+-----------------------------------+
| > TM2000                          | 0.3.3                             |
+-----------------------------------+-----------------------------------+
| > POE Display (B series)          | 4.9 (unreleased)                  |
+-----------------------------------+-----------------------------------+
| > POE Display (C series),         | 5.4                               |
| > Supporting 9 digit display      |                                   |
+-----------------------------------+-----------------------------------+
| > WiFi Display (B series)         | 2.6 (unreleased)                  |
+-----------------------------------+-----------------------------------+
| > WiFi Display (C series)         | 3.4                               |
+-----------------------------------+-----------------------------------+

\*The series letter is determined from the first letter of the serial
number of the device

This API was original designed primarily for use by TM-Manager, but is
available for management and some remote control of the devices. This
later release only supports the mo above. See the 1.1 version of this
document for models prior to the version above.

##  1.1 Device Query

The Locator Data Service is a simple UDP/IP protocol that can be used by
other network applications to extract status and location information
from TM products.

###  1.1.1 POE, WiFi, and DotMatrix Device Query Format (Clocks)

Requesting information from the clocks is done by sending a 3 byte
message to the Clock, using UDP/IP, to port 7372. The three bytes, in
hexadecimal, are: 0xA1 0x04 0xB2 The clock/timers will also respond to a
broadcast to the same port.

The response packet is 40 bytes and will be formatted as follows:

+-----------------------+----------------------------------------------+
| > **Bytes**           | > **Description**                            |
+:=====================:+==============================================+
| > 0                   | Device Type: 0x01=POE, 0x02=WiFi,            |
|                       | 0x03=DotMatrix                               |
+-----------------------+----------------------------------------------+
| > 1 to 4              | client IP address                            |
+-----------------------+----------------------------------------------+
| > 5 to 10             | MAC address                                  |
+-----------------------+----------------------------------------------+
| > 11 and 12           | firmware version Major:Minor                 |
+-----------------------+----------------------------------------------+
| > 13 and 14           | NTP Sync Count                               |
+-----------------------+----------------------------------------------+
| > 15 to 17            | Displayed Time / Timer Value: HH MM SS in    |
|                       | each byte                                    |
+-----------------------+----------------------------------------------+
| > 18                  | Tenths of a second                           |
+-----------------------+----------------------------------------------+
| > 19                  | Display mode: bits 2-0: 000=time 001=Up      |
|                       | Timer 010=Down Timer                         |
|                       |                                              |
|                       | 011=Interval Count Up 100=Interval Count     |
|                       | Down bit5 (0x20): display mode 1=D:H:M mode  |
|                       | (bit7 cleared) bit6 (0x40): start/stop       |
|                       | 0=stopped 1=running bit7 (0x80): display     |
|                       | mode 0=H:M:S 1=M:S:Tenths                    |
+-----------------------+----------------------------------------------+
| > 20                  | Downtimer Alarm Set                          |
|                       |                                              |
|                       | Bit0-6: Alarm Duration                       |
|                       |                                              |
|                       | Bit7: Down timer Alarm Checked 0=unchecked   |
|                       | 1=checked                                    |
+-----------------------+----------------------------------------------+
| > 21                  | Days currently displayed, most significant 8 |
|                       | bits                                         |
+-----------------------+----------------------------------------------+
| > 22                  | Digits value. 0=4/6 Digits, 1=(D):H:M:S,     |
|                       | 2=(H):M:S.Tenths                             |
|                       |                                              |
|                       | Options 1 and 2 require 9 digit display for  |
|                       | () value to show Top 3 bits are the most     |
|                       | significant bits of Day value                |
+-----------------------+----------------------------------------------+
| > 23                  | WiFi Signal Strength. 0 for wired, otherwise |
|                       | negative of value in dBm                     |
+-----------------------+----------------------------------------------+
| > 24 to 39            | Device Name as null terminated ASCII string  |
+-----------------------+----------------------------------------------+

TM-Manager / TM-Timer uses this protocol to find and monitor clocks on
the network. A Wireshark capture of that software can be used to see an
example of the data transfer.

###  1.1.2 TM1000/TM2000 Device Query

Requesting information from the TM1000A/TM2000A is done by sending a 3
byte message to the TMX000A, using UDP/IP, to port 7372. The three
bytes, in hexadecimal, are: 0xA1 0x04 0xB2 The TMX000A will also respond
to a broadcast to the same port.

The response packet is 80 bytes and will be formatted as follows:

+-----------------------+----------------------------------------------+
| > **Bytes**           | > **Description**                            |
+:=====================:+==============================================+
| > 0                   | TM1000A response value = 0x04, TM2000A=0x05  |
+-----------------------+----------------------------------------------+
| > 1 to 4              | client IP address                            |
+-----------------------+----------------------------------------------+
| > 5 to 10             | MAC address                                  |
+-----------------------+----------------------------------------------+
| > 11 and 12           | firmware version Major:Minor                 |
+-----------------------+----------------------------------------------+
| > 13                  | Lock status 0=No Lock, 1=2D Lock, or 2=3D    |
|                       | Lock                                         |
+-----------------------+----------------------------------------------+
| > 14 to 17            | NTP Sync count, 32 bits, MSB to LSB          |
+-----------------------+----------------------------------------------+
| > 18 to 20            | Current Time, H:M:S, UTC                     |
+-----------------------+----------------------------------------------+
| > 21 to 45            | Location of unit 25 bytes, Latitude,         |
|                       | Longitude, null terminated                   |
+-----------------------+----------------------------------------------+
| > 46 to 79            | Name of Time Server, null terminated         |
+-----------------------+----------------------------------------------+

TM-Manager / TM-Timer uses this protocol to find and monitor
TM1000A/TM2000A\'s on the network. A Wireshark capture of that software
can be used to see an example of the data transfer.

##  1.2 Timer Control Sequences

The Up/Down counter timers can be controlled use the same UDP/IP API.
Those sequences are documented in the following sections.

###  1.2.1 Use UpTimer

This command puts the clock into the UpTimer Mode. A single character
\'A\' Acknowledge is sent back.

+----------------+-----------------------------------------------------+
| > **Bytes**    | > **Description**                                   |
+:==============:+=====================================================+
| > 0            | Command Byte: 0xA2                                  |
+----------------+-----------------------------------------------------+
| > 1            | Timer Display Mode: 0x00=MIN:SEC.Tenths,            |
|                | 0x01=HH:MM:SS                                       |
+----------------+-----------------------------------------------------+
| > 2            | 0x00                                                |
+----------------+-----------------------------------------------------+

###  1.2.2 UpTimer Start/Pause

Toggles the UpTimer between running and paused. A single character \'A\'
Acknowledge is sent back.

+----------------+-----------------------------------------------------+
| > **Bytes**    | > **Description**                                   |
+:==============:+=====================================================+
| > 0            | Command Byte: 0xA3                                  |
+----------------+-----------------------------------------------------+
| > 1            | 0x00=Pause 0x01=Count Up \*\*New in version 1.1     |
+----------------+-----------------------------------------------------+
| > 2            | 0x00                                                |
+----------------+-----------------------------------------------------+

###  1.2.3 Uptimer Reset

Resets the UpTimer back to zero. A single character \'A\' Acknowledge is
sent back.

+----------------+-----------------------------------------------------+
| > **Bytes**    | > **Description**                                   |
+:==============:+=====================================================+
| > 0            | Command Byte: 0xA4                                  |
+----------------+-----------------------------------------------------+
| > 1            | Timer Display Mode: 0x00=MIN:SEC.Tenths,            |
|                | 0x01=HH:MM:SS                                       |
+----------------+-----------------------------------------------------+
| > 2            | 0x00                                                |
+----------------+-----------------------------------------------------+

###  1.2.4 Use DownTimer

Sets DownTimer Mode on the clock. A single character \'A\' Acknowledge
is sent back.

+----------------+-----------------------------------------------------+
| > **Bytes**    | > **Description**                                   |
+:==============:+=====================================================+
| > 0            | Command Byte: 0xA5                                  |
+----------------+-----------------------------------------------------+
| > 1            | Timer Display Mode: 0x00=MIN:SEC.Tenths,            |
|                | 0x01=HH:MM:SS Bit 7 is bargraph display. 0x80 to    |
|                | display bargraph on matrix clock                    |
+----------------+-----------------------------------------------------+
| > 2            | Starting Hour value for countdown                   |
+----------------+-----------------------------------------------------+
| > 3            | Starting Minute value for countdown                 |
+----------------+-----------------------------------------------------+
| > 4            | Starting Second value for countdown                 |
+----------------+-----------------------------------------------------+
| > 5            | Starting Tenths of a second value for countdown     |
+----------------+-----------------------------------------------------+
| > 6            | End of countdown Alarm Enable. 0=Disabled,          |
|                | 1=Enabled                                           |
+----------------+-----------------------------------------------------+
| > 7            | Alarm duration in seconds                           |
+----------------+-----------------------------------------------------+
| > 8            | Starting Days value for countdown, LSB (optional,   |
|                | zero if omitted)                                    |
+----------------+-----------------------------------------------------+
| > 9            | Starting Days value for countdown, MSB (optional,   |
|                | zero if omitted)                                    |
+----------------+-----------------------------------------------------+

###  1.2.5 DownTimer Start/Pause

Toggles the DownTimer between running and paused. A single character
\'A\' Acknowledge is sent back.

+----------------+-----------------------------------------------------+
| > **Bytes**    | > **Description**                                   |
+:==============:+=====================================================+
| > 0            | Command Byte: 0xA6                                  |
+----------------+-----------------------------------------------------+
| > 1            | 0x00=Pause 0x01=Count Up \*\*New in version 1.1     |
+----------------+-----------------------------------------------------+
| > 2            | 0x00                                                |
+----------------+-----------------------------------------------------+

###  1.2.6 DownTimer Reset

Resets the DownTimer back to starting value. Must already be in
DownTimer Mode. A single character \'A\' Acknowledge is sent back.

+----------------+-----------------------------------------------------+
| > **Bytes**    | > **Description**                                   |
+:==============:+=====================================================+
| > 0            | Command Byte: 0xA7                                  |
+----------------+-----------------------------------------------------+
| > 1            | Timer Display Mode: 0x00=MIN:SEC.Tenths,            |
|                | 0x01=HH:MM:SS Bit 7 is bargraph display. 0x80 to    |
|                | display bargraph on matrix clock                    |
+----------------+-----------------------------------------------------+
| > 2            | Starting Hour value for countdown                   |
+----------------+-----------------------------------------------------+
| > 3            | Starting Minute value for countdown                 |
+----------------+-----------------------------------------------------+
| > 4            | Starting Second value for countdown                 |
+----------------+-----------------------------------------------------+
| > 5            | Starting Tenths of a second value for countdown     |
+----------------+-----------------------------------------------------+
| > 6            | End of countdown Alarm Enable. 0=Disabled,          |
|                | 1=Enabled                                           |
+----------------+-----------------------------------------------------+
| > 7            | Alarm duration in seconds                           |
+----------------+-----------------------------------------------------+
| > 8            | Starting Days value for countdown, LSB (optional,   |
|                | zero if omitted)                                    |
+----------------+-----------------------------------------------------+
| > 9            | Starting Days value for countdown, MSB (optional,   |
|                | zero if omitted)                                    |
+----------------+-----------------------------------------------------+

###  1.2.7 Set Clock to TimeMode

Returns the clock from the Up/Down Timer modes to regular time display.
A single character \'A\' Acknowledge is sent back.

+----------------+----------------------+------------------------------+
| > **Bytes**    |                      | **Description**              |
+:==============:+======================+==============================+
| > 0            | > Command Byte: 0xA8 |                              |
+----------------+----------------------+------------------------------+
| > 1            | > 0x01               |                              |
+----------------+----------------------+------------------------------+
| > 2            | > 0x00               |                              |
+----------------+----------------------+------------------------------+

###  1.2.8 Set DotMatrix Text (TM848 POE dot matrix models)

Sets the text string on the DotMatrix display. Scrolling direction and
Justification are controlled as well. A single character \'A\'
Acknowledge is sent back.

+----------------+-----------------------------------------------------+
| > **Bytes**    | > **Description**                                   |
+:==============:+=====================================================+
| > 0            | Command Byte: 0xA9                                  |
+----------------+-----------------------------------------------------+
| > 1            | ESC Character for Formatting: 0x1B                  |
+----------------+-----------------------------------------------------+
| > 2            | Bits 3:2=Scroll Direction ( 0=No Scroll, 1=Right to |
|                | Left, 2=Bottom to Top Bits 1:0=Justification of     |
|                | Text (1=Left, 2=Center, 3=Right)                    |
+----------------+-----------------------------------------------------+
| > 3            | Scroll Speed                                        |
+----------------+-----------------------------------------------------+
| > 4            | Text to display starts here, null terminated. 250   |
|                | chars maximum with null                             |
+----------------+-----------------------------------------------------+

###  1.2.9 Set DotMatrix Text, 2 Line Clocks, POE 5.4 and WiFi 3.4 and greater

Sets the text string on the DotMatrix display. Scrolling direction and
Justification are controlled as well. A single character \'A\'
Acknowledge is sent back.

+----------------+-----------------------------------------------------+
| > **Bytes**    | > **Description**                                   |
+:==============:+=====================================================+
| > 0            | Command Byte: 0xA9                                  |
+----------------+-----------------------------------------------------+
| > 1            | Display address. 0x00                               |
+----------------+-----------------------------------------------------+
| > 2            | Text justification:                                 |
|                |                                                     |
|                | Bit 4=Blink. 0=No Blink, 1=Blinking text            |
|                |                                                     |
|                | Bits 3:2=Scroll Direction ( 0=No Scroll, 1=Right to |
|                | Left, 2=Bottom to Top                               |
|                |                                                     |
|                | Bits 1:0=Justification of Text (1=Left, 2=Center,   |
|                | 3=Right)                                            |
+----------------+-----------------------------------------------------+
| > 3            | Scroll Speed, typically 60-150                      |
+----------------+-----------------------------------------------------+
| > 4            | Font. 1=standard, 4=bold                            |
+----------------+-----------------------------------------------------+
| > 5            | Color, 0-9, uses color pallet stored on clock       |
+----------------+-----------------------------------------------------+
| > 6            | Beginning of text string to display. Multiple lines |
|                | can be setup by embedding \'\\n\' into string       |
+----------------+-----------------------------------------------------+
| > 7+String     | String termination byte: 0x0                        |
| > length       |                                                     |
+----------------+-----------------------------------------------------+

###  1.2.10 Static Text Display turn off

When the previous command is used to display some static text on the
2^nd^ line dot-matrix display, this command is used to release that mode
and return the display to control of the clock.

+----------------+----------------------+------------------------------+
| > **Bytes**    |                      | **Description**              |
+:==============:+======================+==============================+
| > 0            | > Command Byte: 0xBD |                              |
+----------------+----------------------+------------------------------+
| > 1            | > 0x01               |                              |
+----------------+----------------------+------------------------------+
| > 2            | > 0x00               |                              |
+----------------+----------------------+------------------------------+

###  1.2.11 Set UpTimer Time While Running

Instantaneously changes the time of the UpTimer while it is running. A
single character \'A\' Acknowledge is sent back.

+----------------+-----------------------------------------------------+
| > **Bytes**    | > **Description**                                   |
+:==============:+=====================================================+
| > 0            | Command Byte: 0xAA                                  |
+----------------+-----------------------------------------------------+
| > 1            | New HOUR value                                      |
+----------------+-----------------------------------------------------+
| > 2            | New MINUTE value                                    |
+----------------+-----------------------------------------------------+
| > 3            | New SECOND value                                    |
+----------------+-----------------------------------------------------+
| > 4            | New TENTHS of a second value                        |
+----------------+-----------------------------------------------------+
| > 5            | New HUNDREDTHS of a second value                    |
+----------------+-----------------------------------------------------+
| > 6            | New DAYS value, LSB (optional, if not present zero  |
|                | will be assumed)                                    |
+----------------+-----------------------------------------------------+
| > 7            | New DAYS value, MSB (optional, if not present zero  |
|                | will be assumed)                                    |
+----------------+-----------------------------------------------------+

###  1.2.12 Set DownTimer Time While Running

Instantaneously changes the time of the DownTimer while it is running. A
single character \'A\' Acknowledge is sent back.

+----------------+-----------------------------------------------------+
| > **Bytes**    | > **Description**                                   |
+:==============:+=====================================================+
| > 0            | Command Byte: 0xAB                                  |
+----------------+-----------------------------------------------------+
| > 1            | New HOUR value                                      |
+----------------+-----------------------------------------------------+
| > 2            | New MINUTE value                                    |
+----------------+-----------------------------------------------------+
| > 3            | New SECOND value                                    |
+----------------+-----------------------------------------------------+
| > 4            | New TENTHS of a second value                        |
+----------------+-----------------------------------------------------+
| > 5            | New HUNDREDTHS of a second value                    |
+----------------+-----------------------------------------------------+
| > 6            | New DAYS value, LSB (optional, if not present zero  |
|                | will be assumed)                                    |
+----------------+-----------------------------------------------------+
| > 7            | New DAYS value, MSB (optional, if not present zero  |
|                | will be assumed)                                    |
+----------------+-----------------------------------------------------+

###  1.2.13 Countdown to Date Timer Reset

Resets the Date Timer back to starting value. A single character \'A\'
Acknowledge is sent back.

+----------------+-----------------------------------------------------+
| > **Bytes**    | > **Description**                                   |
+:==============:+=====================================================+
| > 0            | Command Byte: 0xB9                                  |
+----------------+-----------------------------------------------------+
| > 1            | Display Mode: 0x00=(HR):MIN:SEC.Tenths              |
|                |                                                     |
|                | 0x01=(DAY):HH:MM:SS                                 |
|                |                                                     |
|                | 0x02=DD:HH:MM                                       |
+----------------+-----------------------------------------------------+
| > 2, 3         | Ending Year value for count down to date (ie.       |
|                | 2022). LSB, MSB                                     |
+----------------+-----------------------------------------------------+
| > 4            | Ending Month value for count down to date           |
+----------------+-----------------------------------------------------+
| > 5            | Ending Day value for count down to date             |
+----------------+-----------------------------------------------------+
| > 6            | Ending Hour for count down to date                  |
+----------------+-----------------------------------------------------+
| > 7            | Ending Minute for count down to date                |
+----------------+-----------------------------------------------------+
| > 8            | Ending Second for count down to date                |
+----------------+-----------------------------------------------------+
| > 9            | End of countdown Alarm Enable. 0=Disabled,          |
|                | 1=Enabled                                           |
+----------------+-----------------------------------------------------+
| > 10           | Alarm duration in seconds                           |
+----------------+-----------------------------------------------------+

###  1.2.14 Countdown to Date Start/Pause

Toggles the DownTimer between running and paused. A single character
\'A\' Acknowledge is sent back.

+----------------+----------------------+------------------------------+
| > **Bytes**    |                      | **Description**              |
+:==============:+======================+==============================+
| > 0            | > Command Byte: 0xBA |                              |
+----------------+----------------------+------------------------------+
| > 1            | > 0x00=Pause         |                              |
|                | > 0x01=Count Up      |                              |
+----------------+----------------------+------------------------------+
| > 2            | > 0x00               |                              |
+----------------+----------------------+------------------------------+

##  1.3 Timer Programs

###  1.3.1 Timer Internal Data Structure

The below C structure is used internally within the clocks and
TM-Manager to store the timer programs. This data is sent to and from
the clock in a binary format to store and retrieve the programs. It is
likely that using TM-Manager to create the program and then modifying a
few bytes to change timing durations will be a good strategy for using
this information. Further documentation of each of these values appears
in the following.

typedef struct { unsigned char command;

union { struct {

unsigned char incrementInterval; unsigned char gotoLine; unsigned char
repeatCountSet; unsigned char repeatCountAct;

UINT startSeconds;

UINT stopSeconds; unsigned char relayTimeSec; unsigned char digitColor;
unsigned char matrixAttrib; unsigned char rsrvd3; char
matrixStrig\[12\];

} time; struct {

unsigned char red1; unsigned char grn1; unsigned char blu1; unsigned
char red2; unsigned char grn2; unsigned char blu2; unsigned char
colorIndex1; unsigned char colorIndex2;

} color;

};

} TIMER_PROGRAM;

A program will be made up of an array of 10 of the above structures.
This structure must be 32 bytes. Note that three bytes of padding get
added to the command byte before the union. A declaration such as:

TIMER_PROGRAM tp\[10\];

would be typical to store one program.

Below are descriptions of each member of the struct and the purpose they
serve. Unused members should be 0x00 by default.

**command:** Is a byte value (0x00 -- 0x0B) that determines the function
of the program step using the data in the structure below it. The
section below lists the different timer step functions. From version 5.4
POE, and 3.4 WiFi forward, the color section of the structure is no
longer needed (Change Color (0x09) is still supported in the clocks,
just not needed) but maintained for purposes of operating with clocks
with older firmware running in them. Commands have the ability to change
color in addition to starting a new count-up/down sequence.

**incrementInterval:** When in an interval timer mode, this functions as
a boolean that triggers the program to update the interval count on the
display. Unused when not in an interval timer mode.

**gotoLine:** When in Goto Line mode, this signals which line, 0 through
9, to jump to. When in End Program mode, setting Bit 0 to 1 (0x01)
resets the program after it has finished, and setting Bit 1 to 1 (0x02)
simply stops the program after it has finished. Unused when not in Goto
Line or End Program Mode.

**repeatCountSet:** When in Goto Line mode, this signals how many total
times this command should repeat. For infinite repetitions, set this to
0. Otherwise, values 1-255 are valid. Unused when not in Goto Line mode.

**repeatCountAct:** When in Goto Line mode, this signals how many times
this command has actually been executed so far. Once this matches
repeatCountSet, unless repeatCountSet is 0, Goto Line will cease
repeating and move onto the next step. Unused when not in Goto Line
mode.

**startSeconds:** 4 bytes containing the initial amount of seconds for
the timer to be set to (LSB). Unused when in Goto Line, End Program, or
Change Colors mode.

**stopSeconds:** 4 bytes containing the amount of seconds for the timer
to count to and end at (LSB). Unused when in Goto Line, End Program, or
Change Colors mode.

**relayTimeSec:** This signals the amount of seconds for the relay to be
on. Unused when in Goto Line and Change Colors mode.

**digitColor:** Using the drop down color menu, this byte signals the
colors of the leftmost and rightmost digits. For example, in HH:MM:SS
mode, bits 7:4 are for the hour value, and bits 3:0 are for the minutes
and seconds. For a magenta (4) hour value and green (1) minute and
second value, this byte would be 0x41. Unused when in Goto Line and End
Program mode.

**matrixAttrib:** This byte controls the matrix attributes. Bits 3:0 are
for color using the drop down color menu, bit 4 is for Blink, bit 5 is
for Bar Graph, and bit 6 is for Bold. For Blink, Bar Graph, and Bold, 1
is enabled and 0 is disabled. Unused when in Goto Line, End Program, or
Change Colors mode. **rsrvd3:** Reserved byte of 0x00.

**matrixStrig:** Null-terminated matrix text that must be exactly 12
bytes. Unused when in Goto Line, End Program, or Change Colors mode.

Together, the bytes will be sent as shown in the table below:

+----------------+----------------------------+------------------------------+
| > **Bytes**    |                            | **Description**              |
+:==============:+============================+==============================+
| > 0            | > **command**              |                              |
+----------------+----------------------------+------------------------------+
| > 1 to 3       | > Reserved: 0x00 0x00 0x00 |                              |
+----------------+----------------------------+------------------------------+
| > 4            | > **incrementInterval**    |                              |
+----------------+----------------------------+------------------------------+
| > 5            | > **gotoLine**             |                              |
+----------------+----------------------------+------------------------------+
| > 6            | > **repeatCountSet**       |                              |
+----------------+----------------------------+------------------------------+
| > 7            | > **repeatCountAct**       |                              |
+----------------+----------------------------+------------------------------+
| > 8 to 11      | > **startSeconds**         |                              |
+----------------+----------------------------+------------------------------+
| > 12 to 15     | > **stopSeconds**          |                              |
+----------------+----------------------------+------------------------------+
| > 16           | > **relayTimeSec**         |                              |
+----------------+----------------------------+------------------------------+
| > 17           | > **digitColor**           |                              |
+----------------+----------------------------+------------------------------+
| > 18           | **matrixAttrib**                                          |
+----------------+-----------------------------------------------------------+
| > 19           | **rsrvd3**                                                |
+----------------+-----------------------------------------------------------+
| > 20 to 31     | **matrixStrig**                                           |
+----------------+-----------------------------------------------------------+

###  1.3.2 Timer Functions

Each timer program step can have up to 10 functions, or steps, with the
last function always being the End Program function. Listed here are the
functions and corresponding numerical values.

+--------------+----------------------------+----------------------------+
| > **Number** | **Function**               | **Description**            |
+:============:+:==========================:+:==========================:+
| > 0          | > None                     | Zero to show no current    |
|              |                            | function                   |
+--------------+----------------------------+----------------------------+
| > 1          | > Interval Countdown       | Countdown between two set  |
|              |                            | values with the option to  |
|              |                            | increment an interval      |
+--------------+----------------------------+----------------------------+
| > 2          | > Interval Countup         | Countup between two set    |
|              |                            | values with the option to  |
|              |                            | increment an interval      |
+--------------+----------------------------+----------------------------+
| > 3          | CountUp (DDD):HH:MM:SS     | Countup with               |
|              |                            | (DDD):HH:MM:SS format      |
+--------------+----------------------------+----------------------------+
| > 4          | > CountUp (HR):MM:SS:TS    | Countup with (HR):MM:SS:TS |
|              |                            | format                     |
+--------------+----------------------------+----------------------------+
| > 5          | CountDown (DAY):HH:MM:SS   | > Countdown with           |
|              |                            | > (DDD):HH:MM:SS format    |
+--------------+----------------------------+----------------------------+
| > 6          | CountDown (HR):MM:SS:TS    | > Countdown with           |
|              |                            | > (HR):MM:SS:TS format     |
+--------------+----------------------------+----------------------------+
| > 7          | > Goto Line                | Used to repeat sequences,  |
|              |                            | jump to a specific line    |
|              |                            | and choose how many times  |
|              |                            | to repeat                  |
+--------------+----------------------------+----------------------------+
| > 8          | > End Program              | > Ends the program         |
+--------------+----------------------------+----------------------------+
| > 9          | > Change Colors            | For older devices, this is |
|              |                            | how the color of the clock |
|              |                            | can be changed             |
+--------------+----------------------------+----------------------------+
| > 10         | > CountUp DD:HH:MM         | Countup with DD:HH:MM      |
|              |                            | format                     |
+--------------+----------------------------+----------------------------+
| > 11         | > CountDown DD:HH:MM       | Countdown with DD:HH:MM    |
|              |                            | format                     |
+--------------+----------------------------+----------------------------+

###  1.3.3 Get Program from Clock

Has the clock return the requested program from flash memory.

+----------------+----------------------+------------------------------+
| > **Bytes**    |                      | **Description**              |
+:==============:+======================+==============================+
| > 0            | > Command Byte: 0xB2 |                              |
+----------------+----------------------+------------------------------+
| > 1            | > Program Number:    |                              |
|                | > 0-9                |                              |
+----------------+----------------------+------------------------------+

###  1.3.4 Store Program to Clock

Sends a program to the clock to store in flash memory. Any existing
program at that location will be overwritten, and anything not set
should be defaulted to 0x00. Up to 10 programs can be stored this way.

+----------------+----------------------------+------------------------------+
| > **Bytes**    |                            | **Description**              |
+:==============:+============================+==============================+
| > 0            | > Command Byte: 0xB3       |                              |
+----------------+----------------------------+------------------------------+
| > 1            | > Program Number: 0-9      |                              |
+----------------+----------------------------+------------------------------+
| > 2 to 13      | Program Name (must be exactly 12 bytes and null           |
|                | terminated)                                               |
+----------------+-----------------------------------------------------------+
| > 14-333       | 10 copies of the TIMER_PROGRAM structure                  |
+----------------+-----------------------------------------------------------+

###  1.3.5 Get Clock Active Program

Has the clock send back the program that it is currently set to run.

+----------------+----------------------+------------------------------+
| > **Bytes**    |                      | **Description**              |
+:==============:+======================+==============================+
| > 0            | > Command Byte: 0xAF |                              |
+----------------+----------------------+------------------------------+

###  1.3.6 Reset

Gets the clock out of time mode and ready to run a specific program.
Anything not set should be defaulted to 0x00.

+----------------+-----------------------------------------------------+
| > **Bytes**    | > **Description**                                   |
+:==============:+=====================================================+
| > 0            | Command Byte: 0xAC                                  |
+----------------+-----------------------------------------------------+
| > 1            | 0x4F (sum of program size, program name length, and |
|                | 3)                                                  |
+----------------+-----------------------------------------------------+
| > 2            | Program Number: 0-9                                 |
+----------------+-----------------------------------------------------+
| > 3 to 14      | Program Name (must be exactly 12 bytes and null     |
|                | terminated)                                         |
+----------------+-----------------------------------------------------+
| > 15-334       | 10 copies of the TIMER_PROGRAM structure            |
+----------------+-----------------------------------------------------+

###  1.3.7 Program Start/Pause

Starts, pauses, and resumes the clock\'s current program.

+----------------+----------------------+------------------------------+
| > **Bytes**    |                      | **Description**              |
+:==============:+======================+==============================+
| > 0            | > Command Byte: 0xAE |                              |
+----------------+----------------------+------------------------------+
| > 1            | > 0x00=Stop          |                              |
|                | > 0x01=Start/Resume  |                              |
+----------------+----------------------+------------------------------+

###  1.3.8 Execute Stored Program

Tells the clock to load a particular stored timer program from the flash
memory, and begin executing it. This is the same mechanism used by the
Alarm system in the displays to trigger a stored program at a given
time.

+----------------+----------------------+------------------------------+
| > **Bytes**    |                      | **Description**              |
+:==============:+======================+==============================+
| > 0            | > Command Byte: 0xB8 |                              |
+----------------+----------------------+------------------------------+
| > 1            | > Program Number:    |                              |
|                | > 0-9                |                              |
+----------------+----------------------+------------------------------+

###  1.3.9 Using Timer Program API

A simple way to use the Timer Program API is through a command line with
Ncat. Ncat is a free utility that can be downloaded from [[Nmap\'s
website]{.underline},](https://nmap.org/ncat/) and it allows the user to
send UDP packets directly to an IP address. In order to send the
correctly formatted hexadecimal data, it is easiest to use a binary file
and link that within your command. A helpful tool for this is
[[HxD]{.underline},](https://mh-nexus.de/en/hxd/) an easy-to-use hex
editor that is also free to download online. The desired program data
can be written in a binary file on HxD then sent to the clock through a
UDP packet using Ncat in the command prompt. The clock will always have
a response to confirm delivery of the packet. In some cases, such as Get
Program, it will send an entire program back. When information is not
being requested of it, however, it will simply send back the character
\'A\' (0x41). See below for an example of using this process.

![](media/image3.jpg)

Shown above is an example of a five-step program that could be sent with
Ncat in the command prompt using the command:

> ncat \--udp 192.168.1.1 7372 \< \"C:/users/username/Desktop/file.bin\"

The beginning, "ncat \--udp", signals that a udp packet will be sent
using Ncat. After that, the IP address of the clock is entered followed
by the port that the packet will be sent on. 7372 is the standard port
for communication with the clocks. Next, the correct path to the binary
file that has the hex data must be entered, and the command is complete.

Looking at the hex data above, the program being sent can be deduced.
From the first byte, 0xB3, it is shown that this is a command to store a
program to a clock. The next byte, 0x06, signals that this program will
overwrite and be saved as the seventh program. The next 12 bytes are the
null-terminated name of the program.

In this case, as it can be seen in the decoded text section, the title
is ExampleProg, followed by a null character. The 15^th^ byte is the
first byte of the first step within the program. See below for a
breakdown of each step\'s hexadecimal data.

+------------+-------------------+-------------------------+-------------------------+---------------+
|            |                   | > **Function/Step 1**   |                         |               |
+:==========:+:=======:+:=======:+:=========:+:===========:+:=======:+:=============:+:=============:+
| > 0x06     | 0x00    | 0x00    | 0x00      | 0x00        | 0x00    | > 0x00        | > 0x00        |
+------------+---------+---------+-----------+-------------+---------+---------------+---------------+
| CountDown  | Reserved          |           | > No        | No Goto | > No          | > No          |
| MM:SS:TSec |                   |           | >           | Line    | >             | >             |
|            |                   |           | > increment |         | > repetitions | > repetitions |
+------------+---------+---------+-----------+-------------+---------+---------------+---------------+
| > 0x0A     | 0x00    | 0x00    | 0x00      | 0x00        | 0x00    | > 0x00        | > 0x00        |
+------------+---------+---------+-----------+-------------+---------+---------------+---------------+
|            | Start: 10 seconds |           |             | > Stop: 0 seconds       |               |
+------------+---------+---------+-----------+-------------+---------+---------------+---------------+
| > 0x01     | 0x44    | 0x44    | 0x00      | > 0x6D      | 0x61    | > 0x74        | > 0x72        |
+------------+---------+---------+-----------+-------------+---------+---------------+---------------+
| Relay on   | H:      | Matrix: | Reserved  | > \'m\'     | \'a\'   | \'t\'         | > \'r\'       |
| for 1      | Magenta |         |           |             |         |               |               |
| second     | M/S:    | > Bold  |           |             |         |               |               |
|            |         |         |           |             |         |               |               |
|            | Magenta | Magenta |           |             |         |               |               |
+------------+---------+---------+-----------+-------------+---------+---------------+---------------+
| > 0x69     | 0x78    | 0x20    | 0x74      | 0x65        | 0x78    | > 0x74        | > 0x00        |
+------------+---------+---------+-----------+-------------+---------+---------------+---------------+
| \'i\'      | > \'x\' | > \' \' | \'t\'     | \'e\'       | > \'x\' | \'t\'         | NULL          |
+------------+---------+---------+-----------+-------------+---------+---------------+---------------+

> **Function/Step 2**

+-------------+----------+----------+-----------+-------------+----------+---------------+---------------+
| 0x02        | 0x00     | 0x00     | 0x00      | 0x01        | 0x00     | 0x00          | 0x00          |
+:===========:+=========:+=========:+===========+=============+:========:+:=============:+:=============:+
| Interval    | Reserved            |           | > Increment | No Goto  | No            | No            |
|             |                     |           |             | Line     |               |               |
| > CountUp   |                     |           |             |          | > repetitions | > repetitions |
+-------------+----------+----------+-----------+-------------+----------+---------------+---------------+
| 0x00        | 0x00     | 0x00     | 0x00      | 0x0A        | 0x00     | 0x00          | 0x00          |
+-------------+----------+----------+-----------+-------------+----------+---------------+---------------+
|             | Start: 0 seconds    |           |             | Stop: 10 seconds         |               |
+-------------+----------+----------+-----------+-------------+----------+---------------+---------------+
| 0x03        | 0x33     | 0x23     | 0x00      | 0x00        | 0x00     | 0x00          | 0x00          |
+-------------+----------+----------+-----------+-------------+----------+---------------+---------------+
| Relay on    | H: Cyan  | Matrix:  | Reserved  |             | No matrix text           |               |
| for 3       | M/S:     |          |           |             |                          |               |
| seconds     | Cyan     | Bar      |           |             |                          |               |
|             |          | Graph    |           |             |                          |               |
|             |          | Cyan     |           |             |                          |               |
+-------------+----------+----------+-----------+-------------+----------+---------------+---------------+
| 0x00        | 0x00     | 0x00     | 0x00      | 0x00        | 0x00     | 0x00          | 0x00          |
+-------------+----------+----------+-----------+-------------+----------+---------------+---------------+
|             |                     | No matrix text          |                          |               |
+-------------+---------------------+-------------------------+--------------------------+---------------+
|             |                     | **Function/Step 3**     |                          |               |
+-------------+----------+----------+-----------+-------------+----------+---------------+---------------+
| 0x01        | 0x00     | 0x00     | 0x00      | 0x01        | 0x00     | 0x00          | 0x00          |
+-------------+----------+----------+-----------+-------------+----------+---------------+---------------+
| Interval    | Reserved            |           | > Increment | No Goto  | No            | No            |
|             |                     |           |             | Line     |               |               |
| > countdown |                     |           |             |          | > repetitions | > repetitions |
+-------------+----------+----------+-----------+-------------+----------+---------------+---------------+
| 0x05        | 0x00     | 0x00     | 0x00      | 0x00        | 0x00     | 0x00          | 0x00          |
+-------------+----------+----------+-----------+-------------+----------+---------------+---------------+
|             | Start: 5 seconds    |           |             | Stop: 0 seconds          |               |
+-------------+----------+----------+-----------+-------------+----------+---------------+---------------+
| 0x01        | 0x22     | 0x22     | 0x00      | 0x00        | 0x00     | 0x00          | 0x00          |
+-------------+----------+----------+-----------+-------------+----------+---------------+---------------+
| Relay on    | H: Blue  | Matrix:  | Reserved  |             | No matrix text           |               |
| for 1       | M/S:     |          |           |             |                          |               |
| second      | Blue     | Bar      |           |             |                          |               |
|             |          | Graph    |           |             |                          |               |
|             |          | Blue     |           |             |                          |               |
+-------------+----------+----------+-----------+-------------+----------+---------------+---------------+
| 0x00        | 0x00     | 0x00     | 0x00      | 0x00        | 0x00     | 0x00          | 0x00          |
+-------------+----------+----------+-----------+-------------+----------+---------------+---------------+
|             |                     | No matrix text          |                          |               |
+-------------+---------------------+-------------------------+--------------------------+---------------+
|             |                     | > **Function/Step 4**   |                                          |
+-------------+----------+----------+-----------+-------------+----------+---------------+---------------+
| > 0x07      | 0x00     | 0x00     | 0x00      | 0x00        | 0x01     | > 0x03        | > 0x00        |
+-------------+----------+----------+-----------+-------------+----------+---------------+---------------+
| > Goto Line | Reserved            |           | > No        | > Goto   | 3 repetitions | 0 repetitions |
|             |                     |           | >           | > Line 2 |               | so far        |
|             |                     |           | > increment |          |               |               |
+-------------+----------+----------+-----------+-------------+----------+---------------+---------------+
| > 0x00      | 0x00     | 0x00     | 0x00      | 0x00        | 0x00     | > 0x00        | > 0x00        |
+-------------+----------+----------+-----------+-------------+----------+---------------+---------------+
|             | Start: 0 seconds    |           |             | > Stop: 0 seconds                        |
+-------------+----------+----------+-----------+-------------+----------+---------------+---------------+
| > 0x00      | 0x00     | 0x00     | 0x00      | 0x00        | 0x00     | > 0x00        | > 0x00        |
+-------------+----------+----------+-----------+-------------+----------+---------------+---------------+
| > No relay  | > Color  | > Matrix | Reserved  |             | > No matrix text                         |
|             | > N/A    | > N/A    |           |             |                                          |
+-------------+----------+----------+-----------+-------------+----------+---------------+---------------+
| > 0x00      | 0x00     | 0x00     | 0x00      | 0x00        | 0x00     | > 0x00        | > 0x00        |
+-------------+----------+----------+-----------+-------------+----------+---------------+---------------+
|             |                     | > No matrix text        |                                          |
+-------------+---------------------+-------------------------+------------------------------------------+

### Function/Step 5

+-----------+----------+----------+----------+-------------+---------+---------------+---------------+
| > 0x08    | 0x00     | 0x00     | 0x00     | 0x00        | 0x01    | > 0x00        | > 0x00        |
+:=========:+=========:+=========:+==========+:===========:+:=======:+:=============:+:=============:+
| > End     | Reserved            |          | > No        | Reset   | > No          | > No          |
| > Program |                     |          | >           | after   | >             | >             |
|           |                     |          | > increment | program | > repetitions | > repetitions |
|           |                     |          |             | end     |               |               |
+-----------+----------+----------+----------+-------------+---------+---------------+---------------+
| > 0x00    | 0x00     | 0x00     | 0x00     | 0x00        | 0x00    | > 0x00        | > 0x00        |
+-----------+----------+----------+----------+-------------+---------+---------------+---------------+
|           | > Start: 0 seconds  |          |             | > Stop: 0 seconds       |               |
+-----------+----------+----------+----------+-------------+---------+---------------+---------------+
| > 0x00    | 0x00     | 0x00     | 0x00     | 0x00        | 0x00    | > 0x00        | > 0x00        |
+-----------+----------+----------+----------+-------------+---------+---------------+---------------+
| > No      | > Color  | > Matrix | Reserved |             | > No matrix text        |               |
| > relay   | > N/A    | > N/A    |          |             |                         |               |
+-----------+----------+----------+----------+-------------+---------+---------------+---------------+
| > 0x00    | 0x00     | 0x00     | 0x00     | 0x00        | 0x00    | > 0x00        | > 0x00        |
+-----------+----------+----------+----------+-------------+---------+---------------+---------------+
|           |                     | > No matrix text       |                         |               |
+-----------+---------------------+------------------------+-------------------------+---------------+

The rest of the total 334 bytes are then filled in with 0x00 since there
are no more functions. Once this command has been sent to the clock and
a response of \'A\' confirms that it has been received and saved to
flash memory, the data in the binary file can be deleted and replaced
with a simple "B8 06", the command to run the clock\'s seventh saved
program. The same command prompt command is used after this new data is
saved to the binary file, and the program will run and reset afterward.
To put the clock back into time mode, follow the same process but with
hex data "A8 01 00".

##  1.4 Misc Control Sequences

###  1.4.1 Relay Close

> Immediately close the relay on the display for X seconds.

+----------------+----------------------+------------------------------+
| > **Bytes**    |                      | **Description**              |
+:==============:+======================+==============================+
| > 0            | > Command Byte: 0xB4 |                              |
+----------------+----------------------+------------------------------+
| > 1            | > Number of seconds  |                              |
|                | > to close           |                              |
+----------------+----------------------+------------------------------+
| > 2            | > 0x00               |                              |
+----------------+----------------------+------------------------------+

###  1.4.2 Relay Toggle

Set the relay to either the closed or open position indefinitely. This
condition is not checked by other events which could change the position
of the relay. For example, if a timer ended with a triggered relay
closure, that would change the state of the relay independent of this
command.

+----------------+----------------------+------------------------------+
| > **Bytes**    |                      | **Description**              |
+:==============:+======================+==============================+
| > 0            | > Command Byte: 0xBC |                              |
+----------------+----------------------+------------------------------+
| > 1            | > 0=open, 1=closed   |                              |
+----------------+----------------------+------------------------------+
| > 2            | > 0x00               |                              |
+----------------+----------------------+------------------------------+

###  1.4.3 Revert to Time Timeout

Prior to this release of the API, there was a hard-coded 30 minute
timeout that would revert the clock from a non-running, un-used timer
back to time display on the clock. This command allows this value to be
changed. A value of 0, disables the revert timer. This value is not
saved to non-volatile memory and will need to be updated any time power
is lost.

+----------------+----------------------+------------------------------+
| > **Bytes**    |                      | **Description**              |
+:==============:+======================+==============================+
| > 0            | > Command Bytes:     |                              |
|                | > 0xBB               |                              |
+----------------+----------------------+------------------------------+
| > 1            | > Timeout in         |                              |
|                | > Minutes, LSB       |                              |
+----------------+----------------------+------------------------------+
| > 2            | > Timeout in         |                              |
|                | > Minutes, MSB       |                              |
+----------------+----------------------+------------------------------+

###  1.4.4 Dimmer Set

> Immediately change the dimming level of the display. This setting is
> not saved to non-volatile memory and is thus only active until changed
> or the clock reboots.

+----------------+-----------------------------------------------------+
| > **Bytes**    | > **Description**                                   |
+:==============:+=====================================================+
| > 0            | Command Byte: 0xB5                                  |
+----------------+-----------------------------------------------------+
| > 1            | Digit Brightness: 0-100                             |
+----------------+-----------------------------------------------------+
| > 2            | AM/PM/Colon dot brightness: 0-100                   |
+----------------+-----------------------------------------------------+

###  1.4.5 TimeZone Variable Set

Starting in POE 5.4 and WiFi 3.4, for two line TimeZone clocks, it is
possible to set up to 5 variables that can be periodically displayed by
the TimeZone clock on the second line (see documentation discussing
variable date format display). These values are not stored during loss
of power.

+----------------+-----------------------------------------------------+
| > **Bytes**    | > **Description**                                   |
+:==============:+=====================================================+
| > 0            | Command Byte: 0xBE                                  |
+----------------+-----------------------------------------------------+
| > 1            | Which string variable to set, 0-4                   |
+----------------+-----------------------------------------------------+
| > 2 to 102     | Null terminate string to store in memory for        |
|                | display                                             |
+----------------+-----------------------------------------------------+

###  1.4.6 Color Set

> On RGB equipped displays, immediately change the colors being
> displayed. This setting is not saved to non-volatile memory and is
> thus only active until changed or the clock reboots.

+----------------+-----------------------------------------------------+
| > **Bytes**    | > **Description**                                   |
+:==============:+=====================================================+
| > 0            | Command Byte: 0xB6                                  |
+----------------+-----------------------------------------------------+
| > 1, 2, 3      | MM:SS Digit Color RGB components: 0-255             |
+----------------+-----------------------------------------------------+
| > 4, 5, 6      | HH Digit Color RGB components: 0-255                |
+----------------+-----------------------------------------------------+

#  2 Revision History

  ----------------------------------------------------------------
  ***2.1 Version 1.0***                     ***Initial Release Feb
                                            9, 2018***
  ----------------------------------------- ----------------------
  ***2.2 Version 1.1***                     ***Updated March 22,
                                            2018***

  ----------------------------------------------------------------

- UpTimer and DownTimer Start/Pause commands were updated such that the
  second byte of each now has meaning in the command. This prevented
  repeated packets from negating the previous packets meaning. Support
  for this started in version 2.3 of the WiFi clock, 4.5 of the POE
  clock, and 1.1 of the DotMatrix.

- Support for TM2000A locator protocol.

##  2.3 Version 2.0 Updated March 2021

- Changed format of general Locator response for clocks to allow
  displays to report timer function in real time

- Added direct relay control, dimmer control, RGB color set

##  2.4 Version 2.1

- Added support for countdown to date

- Added support for 9 digit clock displays

- Added option to toggle relay on and off

##  2.5 Version 2.2 May 2023

- Added support for 2 line clock with dot matrix 2^nd^ line display

- Bargraph enable bit on count downs

##  2.6 Version 2.3 July 2025

• Added documentation on how to work with Timer Programs directly
