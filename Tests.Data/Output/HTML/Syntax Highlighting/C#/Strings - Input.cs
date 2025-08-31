
/*
	Topic: Strings

		(code)

		string test = "abc def";

		string embeddedQuotes = "int \" int \" int";

		string verbatimString = @"abc def";

		string verbatimStringEmbeddedQuotes = @"int "" int "" int ";

		string backslashTrap1 = "abc\\";

		string backslashTrap2 = "int \\\" int \\\" int";

		string backslashTrap3 = @"int \"" int \"" int";

		(end)


	Topic: Chars

		(code)

		char test = 'x';

		char apostrophe = '\'';

		char quote1 = '"';

		char quote2 = '\"';

		char backslash = '\\';

		char hex1 = '\x07';

		char hex2 = '\xABCD';

		(end)


	Topic: Interpolated Strings

		Both $@"" and @$"" are explicitly supported according to the language specification.

		(code)

		string interpolatedString1 = $"abc {def} ghi";

		string interpolatedString2 = @$"abc {4+5} ghi";

		string interpolatedString3 = $"abc {obj.Function(12, false)} ghi";

		string interpolatedString4 = $@"abc { (x == y ? "y" : "n") } ghi";

		string interpolatedString5 = $"abc {{ def";

		string interpolatedString6 = $"abc {{ def } ghi";

		string interpolatedString7 = $"abc { def }} ghi";

		string interpolatedString8 = $"abc {{ def }} ghi";

		string interpolatedVerbatimString1 = $@"abc {def} ghi";

		string interpolatedVerbatimString2 = @$"abc {4+5} ghi";

		string interpolatedVerbatimString3 = $"abc {obj.Function(12, false)} ghi";

		string interpolatedVerbatimString4 = $@"abc { (x == y ? "y" : "n") } ghi";

		string interpolatedVerbatimString5 = @$"abc {{ def";

		string interpolatedVerbatimString6 = $@"abc {{ def } ghi";

		string interpolatedVerbatimString7 = @$"abc { def }} ghi";

		string interpolatedVerbatimString8 = $@"abc {{ def }} ghi";

		string invalidInterpolatedString1 = $"abc { def";

		string invalidInterpolatedString2 = @$"abc { def";

		(end)


	Topic: Multiline Strings

		(code)

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

		(end)
*/