
/*
	Topic: Integers

		--- code ---

			x = 12;

			x = -12;

			x = +12;

		---


	Topic: Floating Point

		--- code ---

			x = 1.2;

			x = -1.2;

			x = +1.2;

			x = 1.2e3;

			x = 1.2e-3;

			x = +1.2e3;

			x = -1.2e+3;

			x = 12E3;

			x = -12E3;

		---


	Topic: Base Signifiers

		- Spaces between the apostrophe and the base signifier are illegal.
		- Spaces between the base signifier and the value are allowed.
		- +/- signs must appear before the whole thing, not between the base signifier and the value.

		--- code ---

			x = 'd 12345;

			x = 'D 12345;

			x = 'sd 12345;

			x = 'SD 12345;

			x = +'d12345;

			x = -'SD12345;

			x = 'h 123ABC;

			x = 'H ABC123;

			x = 'sH 123abc;

			x = 'Sh abc123;

			x = +'h123ABC;

			x = -'SHABC123;

			x = 'o 12345;

			x = 'O 01234;

			x = +'so12345;

			x = -'SO01234;

			x = +'b 10101;

			x = -'B01010;

			x = 'sb10101;

			x = 'SB 01010;

		---


	Topic: X, Z, and ?

		- They can replace any digit in a binary, octal, or hexadecimal value.
		- They can only be in decimal if the base is declared and they're the only digit.

		--- code ---

			x = 'h 012xxx;

			x = 'HZ00Z?;

			x = 'so 1x2X3z4Z5?6;

			x = 'BZZZZ0001;

			x = 'd?;

			x = 'D X;

		---


	Topic: Bit Constants

		- While ? can substitute for Z in most constants, there is no '?.

		--- code ---

			x = '0;

			x = '1;

			x = 'x;

			x = 'X;

			x = 'z;

			x = 'Z;

		---


	Topic: Digit Separators

		- _ can separate digits in numbers of any base.
		- They cannot be the first digit.

		--- code ---

			x = 123_456;

			x = 'h 0_1_2___a_b_c___;

			x = 12_3.4_56e7_89;

			x = 'SB 1001_00xx_ZZZZ;

			x = 'D X_____;

		---


	Topic: Sizes

		- A size can appear before the base signifier.
		- Spaces between it and the base signifier are allowed.

		--- code ---

			x = 12 'd 12345;

			x = 4'SB0101;

			x = +16'HZZAA;

			x = -9 'O 03?;

			x = 1_2'DZ______;

		---


	Topic: Time

		- Unsigned decimal or unsigned fixed point followed by a unit.
		- Spaces between the value and the unit are allowed.

		--- code ---

			x = 10s;

			x = 1.2ms;

			x = 100_000 ns;

			x = 2_3_4_ps;

			x = 1_2.3_4 fs;

		---


	Topic: Traps

		- A hex value ending in E shouldn't make it think the following token is part of the number as an exponent.

		--- code ---

			x = 'h 000E+a;

			x = 'H000e-1;

		---


		- The minus sign should be part of the number for -1 but not for x-1.

		--- code ---

			x = -1;

			x = y-1;

			x = y -1;

			x = 1-1;

			x = 1 -1;

			x = (-1);

			x = 1 + -1;

		---

		- Digit separators can't start a number.

		--- code ---

			x = _12;

		---
*/
