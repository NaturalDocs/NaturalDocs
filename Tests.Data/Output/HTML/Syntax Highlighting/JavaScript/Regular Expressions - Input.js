
/* Topic: Distinguishing Regular Expressions
	_____________________________________________

	Slashes that indicate regular expressions:

	--- Code
	regex = /abc/;
	str.replace(/abc/, "xyz");
	return /abc/;
	MyFunction("abc", /xyz/);
	if (/abc/.test(str)) { }
	---

	Slashes that don't indicate regular expressions:

	--- Code
	num = x/y;
	num = (x / y);
	num = (1/2);
	num = 1 / 2;
	num = 1 // 2
	---

	Characters like "gi" attached to the end should be highlighted as part of it:

	--- Code
	str = str.replace(/abc/gi, "xyz");
	---

	Quotes and escaped characters should not throw it off:

	--- Code
	regex = /a\/b\"c"d\'e'f/;
	---

	Unescaped slashes can appear inside character classes.  Both of these work and are equivalent:

	--- Code
	regex = /[a-z/]/;
	regex = /[a-z\/]/;
	---

*/
