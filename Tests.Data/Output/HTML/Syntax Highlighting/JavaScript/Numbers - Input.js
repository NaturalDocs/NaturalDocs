
/*
	Topic: Integers and Floating Point

		--- Code
		12
		-12
		12e1
		12.34e+5
		-12.34e-5
		.012
		-.012
		.012E+5
		-.012e10
		---


	Topic: Hex, Binary, and Octal

		--- Code
		0x01230ABC
		0Xabc12345
		0b00100110
		0B0110100110011100
		0o1234567
		---


	Topic: Digit Separators

		--- Code
		1_234_567
		0xabc1_2345
		0B0110_1001_1001_1100
		1.234_567e1_2
		---


	Topic: Special Numbers

		"n" extension for bignum:

		--- Code
		123n
		---

		Highlight NaN as a number:

		--- Code
		NaN
		---


	Topic: Traps

		The fact that the hex value ends in E shouldn't make it think the following token is part of
		the number as an exponent.

		--- Code
		0x000E+a
		0x000e-1
		---

		The minus sign should be part of the number for -1 but not for x-1.

		--- Code
		-1
		y-1
		y -1
		1-1
		1 -1
		(-1)
		1 + -1
		---

		Digit separators can't start a number.

		--- Code
		_12
		---

		Numbers appearing in other contexts shouldn't be mistaken for constants.

		--- Code
		a1_2_3 = "45"
		---
*/
