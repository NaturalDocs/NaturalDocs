
/* Topic: Rune Literals
	_____________________________________________

	Chars are called runes in Go.

	--- Code
	a 'x' a
	b '\'' b
	c '\"' c
	d '"' d
	e '`' e
	f '\\' f
	g '\xABCD' g
	---
*/


/* Topic: String Literals
	_____________________________________________

	--- Code
	a "x" a
	b "x \"x\" x" b
	c "x \'x\' x" c
	d "x 'x' x" d
	e "x `x` x" e
	f "x \\ x" f
	---
*/


/* Topic: Raw String Literals
	_____________________________________________

	Backslashes don't do anything in raw strings.

	--- Code
	a `x` a
	b `x \'x\' x` b
	c `x \"x\" x` c
	d `x 'x' x` d
	e `x "x" x` e
	f `x \\ x` f
	---

	This means \` isn't a thing, so the backtick should still end the string.

	--- Code
	a `x \` b `x \` c
	---
*/
