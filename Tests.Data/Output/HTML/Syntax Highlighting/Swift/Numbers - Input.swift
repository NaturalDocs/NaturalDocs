
/* Topic: Integer Literals
	_____________________________________________

	Allows decimal, binary, hex, and octal.  All forms can have underscore separators.

	--- Code
	123
	123_456

	0b0110
	0b0110_1001
	0b_0110__1001

	0x01AC
	0x01AC_5BFF
	0x_01AC__5BFF

	0o1377
	0x1377_0211
	0x_1377__0211

	-123
	-123_456
	---
*/


/* Topic: Floating Point Literals
	_____________________________________________

	Floating point literals must have a leading digit.  They cannot start with a dot like ".2".

	--- Code
	1.2
	-1.2
	+1.2

	12e3
	-12E-3
	+12E+3

	1.2e3
	-1.2E-3
	+1.2E+3
	---

	Floating point literals may be in hex.  The exponent uses "p" instead of "e" (for "p"ower of 2) and is still in decimal.

	--- Code
	0xC.D
	-0xC.D
	+0xC.D

	0xCp3
	-0xCP-3
	+0xCP+3

	0xC.0xDp3
	-0xC.0xDP-3
	+0xC.0xDP+3
	---

*/


/* Topic: Traps
	_____________________________________________

	--- Code
	-1
	1-1

	+1
	1+1
	---
*/
