
/*
	Topic: Chars

		--- code

		char test = 'x';

		char apostrophe = '\'';

		char quote1 = '"';

		char quote2 = '\"';

		char backslash = '\\';

		char hex1 = '\x07';

		char hex2 = '\xABCD';

		---


	Topic: Quoted Strings

		--- code

		string quotedString = "abc def";

		string quotedStringEscaping1 = "int \" int \" int";

		string quotedStringEscaping2 = "abc\\";

		string quotedStringEscaping3 = "int \\\" int \\\" int";

		---


	Topic: Verbatim Strings

		--- code

		string verbatimString = @"abc def";

		string verbatimStringEscaping1 = @"int "" int "" int ";

		string verbatimStringEscaping2 = @"int \"" int \"" int";

		---


	Topic: Raw Strings

		--- code

		string rawString1 = """abc def""";

		string rawString2 = """"abc def"""";

		string rawString3 = """"""abc def"""""";

		string rawStringEscaping1 = """int " int "" int""";

		string rawStringEscaping2 = """"int " int "" int """ int"""";

		string rawStringEscaping3 = """"""int " int "" int """ int """" int """"" int"""""";

		---


	Topic: Interpolated Quoted Strings

		--- code

		string interpolatedQuotedString1 = $"abc {def} ghi";

		string interpolatedQuotedString2 = $"abc {4+5} ghi";

		string interpolatedQuotedString3 = $"abc {obj.Function(12, false)} ghi";

		string interpolatedQuotedString4 = $"abc { (x == y ? "y" : "n") } ghi";

		string interpolatedQuotedString5 = $"abc {{ def";

		string interpolatedQuotedString6 = $"abc {{ def } ghi";

		string interpolatedQuotedString7 = $"abc { def }} ghi";

		string interpolatedQuotedString8 = $"abc {{ def }} ghi";

		string interpolatedQuotedString9 = $"abc {{{ def }}} ghi";

		string interpolatedQuotedString10 = $"abc {{{{ def }}}} ghi";

		---


	Topic: Interpolated Verbatim Strings

		Both $@ and @$ are explicitly supported according to the language specification.

		--- code

		string interpolatedVerbatimString1 = $@"abc {def} ghi";

		string interpolatedVerbatimString2 = @$"abc {4+5} ghi";

		string interpolatedVerbatimString3 = $@"abc {obj.Function(12, false)} ghi";

		string interpolatedVerbatimString4 = @$"abc { (x == y ? "y" : "n") } ghi";

		string interpolatedVerbatimString5 = $@"abc {{ def";

		string interpolatedVerbatimString6 = @$"abc {{ def } ghi";

		string interpolatedVerbatimString7 = $@"abc { def }} ghi";

		string interpolatedVerbatimString8 = @$"abc {{ def }} ghi";

		string interpolatedVerbatimString9 = $@"abc {{{ def }}} ghi";

		string interpolatedVerbatimString10 = @$"abc {{{{ def }}}} ghi";

		---


	Topic: Interpolated Raw Strings

		If there is a single $, then braces are interpolations.  Unlike quoted and verbatim strings, you cannot use double braces to
		escape them.

		--- code

		string interpolatedRawString1_1_1 = $"""abc {def} ghi""";

		string interpolatedRawString1_1_2 = $""""abc {4+5} ghi"""";

		string interpolatedRawString1_1_3 = $"""""abc {obj.Function(12, false)} ghi""""";

		string interpolatedRawString1_1_4 = $""""""abc { (x == y ? "y" : "n") } ghi"""""";

		string interpolatedRawString1_1_5 = $"""abc { def }} ghi""";

		---

		If there is a double $, single braces are part of the string, double braces are interpolations, triple braces have one as part of
		the string and two as the interpolation, and four or more are invalid.

		--- code

		string interpolatedRawString2_1_1 = $$"""abc {def} ghi""";

		string interpolatedRawString2_1_2 = $$""""abc {4+5} ghi"""";

		string interpolatedRawString2_1_3 = $$"""""abc {obj.Function(12, false)} ghi""""";

		string interpolatedRawString2_1_4 = $$""""""abc { (x == y ? "y" : "n") } ghi"""""";

		string interpolatedRawString2_1_5 = $$"""abc {def}} ghi""";

		string interpolatedRawString2_2_1 = $$"""abc {{def}} ghi""";

		string interpolatedRawString2_2_2 = $$""""abc {{4+5}} ghi"""";

		string interpolatedRawString2_2_3 = $$"""""abc {{obj.Function(12, false)}} ghi""""";

		string interpolatedRawString2_2_4 = $$""""""abc {{ (x == y ? "y" : "n") }} ghi"""""";

		string interpolatedRawString2_2_5 = $$"""abc {{def}}} ghi""";

		string interpolatedRawString2_3_1 = $$"""abc {{{def}}} ghi""";

		string interpolatedRawString2_3_2 = $$""""abc {{{4+5}}} ghi"""";

		string interpolatedRawString2_3_3 = $$"""""abc {{{obj.Function(12, false)}}} ghi""""";

		string interpolatedRawString2_3_4 = $$""""""abc {{{ (x == y ? "y" : "n") }}} ghi"""""";

		string interpolatedRawString2_3_5 = $$"""abc {{{def}}}}} ghi""";

		---

		If there is a triple $, single and double braces are part of the string, triple braces are interpolations, four and five have one
		and two as part of the string and then the three following as the interpolation, and six or more are invalid.

		--- code

		string interpolatedRawString3_1_1 = $$$"""abc {def} ghi""";

		string interpolatedRawString3_1_2 = $$$""""abc {4+5} ghi"""";

		string interpolatedRawString3_1_3 = $$$"""""abc {obj.Function(12, false)} ghi""""";

		string interpolatedRawString3_1_4 = $$$""""""abc { (x == y ? "y" : "n") } ghi"""""";

		string interpolatedRawString3_1_5 = $$$"""abc {def}} ghi""";

		string interpolatedRawString3_2_1 = $$$"""abc {{def}} ghi""";

		string interpolatedRawString3_2_2 = $$$""""abc {{4+5}} ghi"""";

		string interpolatedRawString3_2_3 = $$$"""""abc {{obj.Function(12, false)}} ghi""""";

		string interpolatedRawString3_2_4 = $$$""""""abc {{ (x == y ? "y" : "n") }} ghi"""""";

		string interpolatedRawString3_2_5 = $$$"""abc {{def}}} ghi""";

		string interpolatedRawString3_3_1 = $$$"""abc {{{def}}} ghi""";

		string interpolatedRawString3_3_2 = $$$""""abc {{{4+5}}} ghi"""";

		string interpolatedRawString3_3_3 = $$$"""""abc {{{obj.Function(12, false)}}} ghi""""";

		string interpolatedRawString3_3_4 = $$$""""""abc {{{ (x == y ? "y" : "n") }}} ghi"""""";

		string interpolatedRawString3_3_5 = $$$"""abc {{{def}}}} ghi""";

		string interpolatedRawString3_4_1 = $$$"""abc {{{{def}}}} ghi""";

		string interpolatedRawString3_4_2 = $$$""""abc {{{{4+5}}}} ghi"""";

		string interpolatedRawString3_4_3 = $$$"""""abc {{{{obj.Function(12, false)}}}} ghi""""";

		string interpolatedRawString3_4_4 = $$$""""""abc {{{{{ (x == y ? "y" : "n") }}}} ghi"""""";

		string interpolatedRawString3_4_5 = $$$"""abc {{{{def}}}}}} ghi""";

		string interpolatedRawString3_5_1 = $$$"""abc {{{{{def}}}}} ghi""";

		string interpolatedRawString3_5_2 = $$$""""abc {{{{{4+5}}}}} ghi"""";

		string interpolatedRawString3_5_3 = $$$"""""abc {{{{{obj.Function(12, false)}}}}} ghi""""";

		string interpolatedRawString3_5_4 = $$$""""""abc {{{{{ (x == y ? "y" : "n") }}}}} ghi"""""";

		string interpolatedRawString3_5_5 = $$$"""abc {{{{{def}}}}}}} ghi""";

		---

		This continues for an arbitrary amount of $ symbols, just like there can be an arbitrary number of quotes.

		--- code

		string interpolatedRawString5_3 = $$$$$"""abc {{{def}}} ghi""";

		string interpolatedRawString5_5 = $$$$$"""abc {{{{{def}}}}} ghi""";

		string interpolatedRawString5_7 = $$$$$"""abc {{{{{{{def}}}}}}} ghi""";

		string interpolatedRawString7_4 = $$$$$$$"""abc {{{{def}}}} ghi""";

		string interpolatedRawString7_7 = $$$$$$$"""abc {{{{{{{def}}}}}}} ghi""";

		string interpolatedRawString7_9 = $$$$$$$"""abc {{{{{{{{{def}}}}}}}}} ghi""";

		---

		The below strings are all invalid so how they are handled isn't important, just that the parser doesn't hang
		or crash.

		--- code

		string invalidRawString1 = $"""abc { def""";

		string invalidRawString2 = $$"""abc {{ def""";

		string invalidRawString3 = $$$"""abc {{{ def""";

		string invalidRawString4 = $"""abc {{ def }} ghi""";

		string invalidRawString5 = $$"""abc {{{{ def }}}} ghi""";

		string invalidRawString6 = $$$"""abc {{{{{{ def }}}}}} ghi""";

		string invalidRawString7 = $"""abc {{ def } ghi""";

		string invalidRawString8 = $$"""abc {{{{ def }} ghi""";

		---


	Topic: Multiline Verbatim Strings

		--- code

		string multilineVerbatimString1 = @"abc
			def
			ghi";

		string multilineVerbatimInterpolatedString1 = $@"abc
			def { ghi } jkl
			mno";

		string multilineVerbatimInterpolatedString2 = @$"abc
			def { (x > 2 ?
					 "y" :
					 "n") } jkl
			mno";

		string multilineVerbatimInterpolatedString3 = $@"abc
			def {
				(x > 2 ?
				 "y" :
				 "n" )
			} jkl
			mno";

		---


	Topic: Multiline Raw Strings

		--- code

		string multilineRawString1 = $"""
			abc
			def
			""";

		string multilineRawInterpolatedString1 = $""""
			abc
			def { ghi } jkl
			mno
			"""";

		string multilineRawInterpolatedString2 = $"""""
			abc
			def { (x > 2 ?
					 "y" :
					 "n") } jkl
			mno
			""""";

		string multilineRawInterpolatedString3 = $""""""
			abc
			def {
				(x > 2 ?
				 "y" :
				 "n" )
			} jkl
			mno
			"""""";

		---

*/