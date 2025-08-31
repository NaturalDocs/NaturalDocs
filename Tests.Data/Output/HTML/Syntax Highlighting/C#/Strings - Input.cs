
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

        string interpolatedString2 = $"abc {obj.Function(12, false)} ghi";

		string interpolatedVerbatimString1 = $@"abc {def} ghi";

        string interpolatedVerbatimString2 = @$"abc {4+5} ghi";

        string interpolatedVerbatimString3 = $@"abc { (x == y ? "y" : "n") } ghi";

        string interpolatedVerbatimString4 = @$"abc {{ def";

		string invalidInterpolatedString = $"abc { def";

		(end)

*/