
/*
	Topic: Integers

		--- Code
		12
		-12
		123456789l
		-123456789L
		12U
		123456789ul
		---


	Topic: Floating Point

		--- Code
		12e1
		-12E-1
		12.34f
		-12.34F
		12.34e+5
		-12.34e-5f
		.012
		-.012F
		.012E+5
		-.012e10
		0.0123f
		-0.0123F
		0.0123e+5
		-0.0123E-5
		---


	Topic: Hex and Binary

		--- Code
		0x01230ABC
		0xabc12345UL
		0b00100110
		0B0110100110011100ul
		---


	Topic: Digit Separators

		--- Code
		1_234_567
		0xabc1_2345UL
		0x_ab12_cd45UL
		0B0110_1001_1001_1100ul
		0b_0110_1001_1001_1100ul
		1.234_567e1_2
		1____2____3
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
