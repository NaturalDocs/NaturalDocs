
/* Topic: String Literals
	_____________________________________________

	--- Code
	a "x" a
	b "\'" b
	c "\"" c
	d "\\" d
	---
*/


/* Topic: Multiline String Literals
	_____________________________________________

	--- Code
	a
	"""
	b "c" d \"e\" f
	"""
	g
	---
*/


/* Topic: Interpolated String Literals
	_____________________________________________

	--- Code
	a " x \(x) x" a
	b " x \( "x" ) x" b
	c " x \( (x + 5) * 2 ) x " c
	---
*/


/* Topic: Multiline Interpolated String Literals
	_____________________________________________

	--- Code
	a
	"""
	b \(c) d \("e") f \((g + 5) * 2) h \("""i""") j
	"""
	k
	---
*/


/* Topic: Extended String Literals
	_____________________________________________

	--- Code
	a #"x"# a
	b #" x "x" x "# b
	c ###" x "# x "## x "### c
	---
*/


/* Topic: Extended Multiline String Literals
	_____________________________________________

	--- Code
	a
	###"""
	b "c" d """ e """## f
	"""###
	g
	---
*/

/* Topic: Interpolated Extended String Literals
	_____________________________________________

	The number of hashes after the backslash must match the delimiters.

	--- Code
	a #"x \(x) \#(x) x"# a
	b ##"x \(x) \#(x) \##(x) x"## b
	c ###"x \(x) \#(x) \##(x) \###(x) x"### c
	---
*/


/* Topic: Multiline Interpolated Extended String Literals
	_____________________________________________

	--- Code
	a
	###"""
	b \(c) \#(d) \##(e) \###(f) g
	"""###
	h
	---
*/
