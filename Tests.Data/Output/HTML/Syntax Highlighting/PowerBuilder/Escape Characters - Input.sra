
/* Topic: Escape Characters

	Strings are *not* escaped with backslashes.  They are escaped with tildes.

	--- Code

		li_Pos = LastPos( ls_FileWithPath, '\' )
		li_Pos = LastPos( ls_FileWithPath, "\" )
		li_Pos = LastPos( ls_FileWithPath, '~'' )
		li_Pos = LastPos( ls_FileWithPath, "~"" )
		li_Pos = LastPos( ls_FileWithPath, '~~' )
		li_Pos = LastPos( ls_FileWithPath, "~~" )

	---
*/