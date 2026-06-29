
/* Topic: Integer Literals
	_____________________________________________

	Allows decimal, binary, hex, and octal.  All forms can have apostrophe separators.

	Octal literals just start with zero, not zero and then the letter O.

	--- Code
	123
	123'456

	0b0110
	0b0110'1001

	0x01AC
	0x01'AC'5B'FF

	01377
	01377'0211

	-123
	-123'456
	---
*/


/* Topic: Integer Literals with Suffixes
	_____________________________________________

	--- Code
	123u
	123'456U

	0b0110LL
	0B0110'1001ull

	0x01ACz
	0X01AC'5BFFZ
	---
*/


/* Topic: Separators vs. Char Literals
	_____________________________________________

	--- Code
	'1'
	('123456'+12'34'56)
	[ '0xFFFF' + 0xF'FF'F ]
	---
*/


/* Topic: Floating Point Literals
	_____________________________________________

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

	.2e3
	-.2E-3
	+.2E+3
	---
*/


/* Topic: Floating Point Literals with Separators and Suffixes
	_____________________________________________

	--- Code
	100'000e300'000
	-100'000.200'000e-300'000

	1.2f
	-12E3F64
	+1.2E+3bf128
	---
*/


/* Topic: Hex Floating Point Literals

	--- Code
	0xC.D
	-0xC.D
	+0xC.D

	0xCp3
	-0xCP-3
	+0xCP+3

	0xC.Dp3
	-0xC.DP-3
	+0xC.DP+3

	0x.Dp3
	-0x.DP-3
	+0x.DP+3
	---
*/
