
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


/* Topic: Integer Literals with Suffixes
	_____________________________________________

	--- Code
	123u8
	123_456_u16

	0b0110i8
	0b0110_1001_i16

	0x01ACu32
	0x01AC_5BFF_u64
	0x_01AC__5BFF__u128

	0o1377i32
	0x1377_0211_i64
	0x_1377__0211__i128

	-123usize
	-123_456_isize
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

	Underscores are allowed to precede the exponent digits.

	--- Code
	100_000e300_000
	+100_000.200_000e300_000
	-100_000.200_000e-_300_000
	0.200_000e+_300_000
	---
*/


/* Topic: Floating Point Literals with Suffixes
	_____________________________________________

	--- Code
	1.2f32
	-12E3f32
	+1.2E+3f64
	---
*/


/* Topic: Traps
	_____________________________________________

	--- Code
	-1
	1-1

	1-1u8
	1u8-1u8
	1u8-1

	1-1usize
	1usize-1usize
	1usize-1

	+1
	1+1

	1+1u8
	1u8+1u8
	1u8+1

	1+1usize
	1usize+1usize
	1usize+1
	---
*/
