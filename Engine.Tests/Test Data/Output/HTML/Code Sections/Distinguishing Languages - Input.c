
/*
	File: Distinguishing Languages
	_______________________________________________________________________________

	Only the comments matching the language should be highlighted as such.


	Topic: A

		Before

		(code)
		string language = "Unspecified, defaults to C";
		// C comment
		# Perl comment
		-- SQL comment
		(end)

		After


	Topic: B

		Before

		(C)
		string language = "C";
		// C comment
		# Perl comment
		-- SQL comment
		(end)

		After


	Topic: C

		Before

		(Perl)
		string language = "Perl";
		// C comment
		# Perl comment
		-- SQL comment
		(end)

		After


	Topic: D

		Before

		(SQL)
		string language = "SQL";
		// C comment
		# Perl comment
		-- SQL comment
		(end)

		After


	Topic: E

		Before

		(Pascal)
		string language = "Pascal";
		# Perl comment
		// Pascal comment 1
		(* Pascal comment 2 *)
		{ Pascal comment 3 }
		-- SQL comment
		(end)

		After


	Topic: F

		Before

		(Table)
		string language = "Text, no highlighting";
		# Perl comment
		// Pascal comment 1
		(* Pascal comment 2 *)
		{ Pascal comment 3 }
		-- SQL comment
		(end)

		After

*/