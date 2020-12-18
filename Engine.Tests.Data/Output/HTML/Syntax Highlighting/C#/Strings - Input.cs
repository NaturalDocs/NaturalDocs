
/*
	Topic: Strings

		(code)

		string test = "abc def";

		string embeddedQuotes = "int \" int \" int";

		string atString = @"abc def";

		string atStringEmbeddedQuotes = @"int "" int "" int ";

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

		(code)

        string interpolatedString1 = $"abc {def} ghi";

        string interpolatedString2 = $@"abc {def} ghi";

        string interpolatedString3 = $"abc {4+5} ghi";

        string interpolatedString4 = $@"abc {obj.Function(12, false)} ghi";

        string interpolatedString4 = $"abc { (x == y ? "y" : "n") } ghi";

        string interpolatedString5 = $"abc {{ def";

        string invalidInterpolatedString = $"abc { def";

		(end)

*/