
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
	c ###" "# "## "### c
	d ###" \(x + 5) "### d
	---
*/


/* Topic: Extended Multiline String Literals
	_____________________________________________

	--- Code
	a
	###"""
	b "c" d """ e \(f+5) g """## h
	"""###
	i
	---
*/
