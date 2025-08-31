
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

		--- code

		string interpolatedRawString1 = $"""abc {def} ghi""";

		string interpolatedRawString2 = $""""abc {4+5} ghi"""";

		string interpolatedRawString3 = $"""""abc {obj.Function(12, false)} ghi""""";

		string interpolatedRawString4 = $""""""abc { (x == y ? "y" : "n") } ghi"""""";

		string interpolatedRawString5 = $"""abc {{ def""";

		string interpolatedRawString6 = $""""abc {{ def } ghi"""";

		string interpolatedRawString7 = $"""""abc { def }} ghi""""";

		string interpolatedRawString8 = $""""""abc {{ def }} ghi"""""";

		string interpolatedRawString9 = $"""""""abc {{{ def }}} ghi""""""";

		string interpolatedRawString10 = $""""""""abc {{{{ def }}}} ghi"""""""";

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