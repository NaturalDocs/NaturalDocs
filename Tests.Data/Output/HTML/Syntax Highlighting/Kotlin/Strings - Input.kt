
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


/* Topic: String Templates
	_____________________________________________

	--- Code
	a " x ${x} x" a
	b " x ${ "x" } x" b
	c " x ${ (x + 5) * 2 } x " c
	---
*/


/* Topic: Multiline String Templates
	_____________________________________________

	--- Code
	a
	"""
	b ${c} d ${"e"} f ${(g + 5) * 2} h
	"""
	i
	---
*/


/* Topic: Multi-Dollar String Templates
	_____________________________________________

	--- Code
	a $$"x ${x} $${x}" a
	b $$$" ${x} $${x} $$${x} " b
	---
*/


/* Topic: Extended Multiline String Literals
	_____________________________________________

	--- Code
	a
	$$$"""
	b "c" d ${e} $${f} $$${g} h
	"""
	i
	---
*/
