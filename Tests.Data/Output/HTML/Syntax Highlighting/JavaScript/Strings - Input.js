
/* Topic: Single Quote Strings
	_____________________________________________

	--- Code
	a 'x' a
	b '\'' b
	c '"' c
	d '\\' d
	---
*/


/* Topic: Double Quote Strings
	_____________________________________________

	--- Code
	a "x" a
	b "'" b
	c "\"" c
	d "\\" d
	---
*/


/* Topic: Backtick Strings
	_____________________________________________

	--- Code
	a `x` a
	b `'` b
	c `"` c
	d `\`` d
	e `\\` e
	---
*/


/* Topic: Backtick String Interpolation
	_____________________________________________

	--- Code
	a ` x ${x} x` a
	b ` x ${ `x` } x` b
	c ` x ${ (x + 5) * 2 } x ` c
	d ` x \` x \$ x \${x} x ` d
	---
*/


/* Topic: Multiline Backtick Strings
	_____________________________________________

	--- Code
	a
	`b ${c} d ${"e"} f
	g ${(h + 5) * 2} i`
	j
	---
*/
