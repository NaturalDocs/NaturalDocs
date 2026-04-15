
/* Topic: Char Literals
	_____________________________________________

	--- Code
	a 'x' a
	b '\'' b
	c '\"' c
	d '\\' d
	---
*/


/* Topic: String Literals
	_____________________________________________

	--- Code
	a "x" a
	b "\'" b
	c "\"" c
	d "\\" d
	---
*/


/* Topic: C and Byte Literals
	_____________________________________________

	--- Code
	a b'x' a
	b c'\'' b
	c b'\"' c
	d c'\\' d

	a b"x" a
	b c"\'" b
	c b"\"" c
	d c"\\" d
	---
*/


/* Topic: Raw String Literals
	_____________________________________________

	--- Code
	a r"xxx'xxx" a
	b br#"xxx"xxx"# b
	c cr####"xxx"###xxx"#### c
	---
*/


/* Topic: Line Breaks
	_____________________________________________

	Line breaks are allowed if preceded by a backslash.

	--- Code
	a "xxx\
	xxx" a
	---
*/


/* Topic: Char Literals vs. Lifetimes
	_____________________________________________

	--- Code
	fn Lifetimes<'a> (a: &'a i32,
                      b: &'a mut i32,
                      c: &'a [i32; 5],
                      d: &'a mut [i32; 5]);

	fn Lifetimes<'a> (a: &'a i32, b: &'a mut i32, c: &'a [i32; 5], d: &'a mut [i32; 5]);

	'a'
	' '
	'\ '
	x<'a'
	---
*/