
// This file is part of Natural Docs, which is Copyright © 2003-2026 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;


namespace CodeClear.NaturalDocs.Engine.Tokenization
	{

	/* Enum: FundamentalType
	 *
	 * The type of token it is on the most basic level.
	 *
	 *		Null - Returned when the token iterator is out of bounds.
	 *		LineBreak - A single line break in CR, LF, or CR/LF format.
	 *		Whitespace - A series of consecutive space and/or tab characters.
	 *		Text - A series of consecutive ASCII letters, numbers, and/or characters above ASCII 0x7F.
	 *					This does *not* include underscores.
	 *		Symbol - One character not mentioned above, which are all the symbol characters available
	 *						  on the standard US QWERTY keyboard plus ASCII control characters.
	 */
	public enum FundamentalType : byte
		{  Null = 0, LineBreak, Whitespace, Text, Symbol  }


	/* Enum: CommentParsingType
	 *
	 * The type of token it is as is relevant to comment parsing.
	 *
	 *		Null - Returned when the token iterator is out of bounds or if a token hasn't been assigned one of these
	 *					values yet.
	 *
	 *		CommentSymbol - A comment symbol or part of one.
	 *		CommentDecoration - A symbol that only provides decoration for a comment, such as part of
	 *													a horizontal line.
	 *
	 *		PossibleOpeningTag - An opening symbol that's a candidate for being part of a link, bold, or underline tag.
	 *		PossibleClosingTag - A closing symbol that's a candidate for being part of a link, bold, or underline tag.
	 *		OpeningTag - An opening symbol that's a part of a link, bold, or underline tag.
	 *		ClosingTag - A closing symbol that's a part of a link, bold, or underline tag.
	 *
	 *		URL - Part of an URL.
	 *		EMail - Part of an e-mail address.
	 */
	public enum CommentParsingType : byte
		{
		Null = 0,
		CommentSymbol, CommentDecoration,
		PossibleOpeningTag, PossibleClosingTag,
		OpeningTag, ClosingTag,
		URL, EMail
		}


	/* Enum: SyntaxHighlightingType
	 *
	 * The type of token it is as is relevant to prototype parsing.
	 *
	 * Null - Returned when the token is out of bounds or one of these values hasn't been assigned to it yet.
	 * Keyword - A reserved word, like "int".
	 * Number - A numeric constant, like "12", "0xFF", or "-1.5".  The format doesn't matter.
	 * String - A string.  Also covers char constants for languages that have them.
	 * Comment - A comment, both symols and content.
	 * PreprocessingDirective - A preprocessing directive such as "#define x".
	 * Metadata - Code metadata such as the "[Flags]" attribute in C#.
	 */
	public enum SyntaxHighlightingType :  byte
		{
		Null = 0,
		Keyword, Number, String, Comment,
		PreprocessingDirective, Metadata
		}


	/* Enum: PrototypeParsingType
	 *
	 *		Null - Returned when the token is out of bounds or one of these values hasn't been assigned to it yet.
	 *
	 *
	 * Sections:
	 *
	 *		Sections are used to separate prototypes into blocks which are individually formatted.  For example, attributes in C# are
	 *		separated into their own sections so they appear on a separate line:
	 *
	 *		--- C#
	 *		[DllImport ("library.dll")]
	 *		extern static private bool LibraryFunction ();
	 *		---
	 *
	 *		StartOfPrototypeSection - The first token of a new prototype section.  It is possible for these to appear without
	 *											  corresponding <EndOfPrototypeSections>.
	 *		EndOfPrototypeSection - The last token of a prototype section, causing the next token to start a new one.  It is possible
	 *											for these to appear without corresponding <StartOfPrototypeSections>.
	 *
	 * Parameters:
	 *
	 *		StartOfParams - The start of a parameter list, such as an opening parenthesis.  It is possible for these to appear
	 *								without a corresponding <EndOfParams>.
	 *		EndOfParams - The end of a parameter list, such as a closing parenthesis.
	 *		ParamSeparator - A separator between parameters, such as a comma.
	 *
	 *		OpeningParamDecorator - A symbol at the beginning of a parameter, such as "{" in Tcl's "{a 12}".
	 *		ClosingParamDecorator - A symbol at the end of a parameter, such as "}" in Tcl's "{a 12}".
	 *
	 *
	 *	Types:
	 *
	 *		Type - The type excluding all modifiers and qualifiers, such as "int" in "unsigned int" or "Class" in "PkgA.PkgB.Class".
	 *		TypeModifier - A separate word modifying a type, such as "const" in "const int".
	 *		TypeQualifier - Everything prior to the ending word in a qualified type, such as "PkgA.PkgB." in "PkgA.PkgB.Class".
	 *		OpeningTypeModifier - An opening symbol modifying a type, such as "[" in "int[]" or "<" in "List<int>".  Can be followed
	 *										 by <OpeningExtensionSymbols> for multi-token symbols.
	 *		ClosingTypeModifier - A closing symbol modifying a type, such as "]" in "int[]" or ">" in "List<int>".  Can be followed by
	 *										<ClosingExtensionSymbols> for multi-token symbols.
	 *
	 *
	 *	Tuples:
	 *
	 *		Tuples are groups of types declared inline and used as a single type, essentially creating a struct without declaring it first.
	 *		In C# they look like this.  The members can be named or unnamed:
	 *
	 *		--- C#
	 *		public (string, int) varA;
	 *		public (string m, int n) varB;
	 *		---
	 *
	 *		They can also be nested:
	 *
	 *		--- C#
	 *		public (string, (int, float)) varC;
	 *		public (string m, (int i, float j) n) varD;
	 *		---
	 *
	 *		StartOfTuple - The start of a tuple, such as an opening parenthesis.  Can be followed by <OpeningExtensionSymbols> for
	 *							 multi-token symbols.
	 *		EndOfTuple - The end of a tuple, such as a closing parenthesis.  Can be followed by <ClosingExtensionSymbols> for
	 *							multi-token symbols.
	 *		TupleMemberSeparator - A separator between tuple members, such as a comma.
	 *		TupleMemberName - The name of a tuple member.
	 *
	 *
	 *	Names:
	 *
	 *		NameTypeSeparator - In languages that use them, the symbol separating a variable name from its type, such as ":" in
	 *										"x: int".  In languages that simply use a space this type won't appear.
	 *
	 *		Name - The name of the parameter or the code element being defined by the prototype.
	 *
	 *		KeywordName - The name of the code element being defined that is also a keyword, such as "operator" when overloading
	 *								operators or "get" and "set" when defining properties.
	 *
	 *
	 *	Parameter Modifiers:
	 *
	 *		Parameter modifiers are symbols that are part of the type but appear with the parameter name and only affect that parameter.
	 *		For example:
	 *
	 *		--- C++
	 *		int *x, y;
	 *		---
	 *
	 *		Here * is a parameter modifier, as the type of x is int* but the type of y is just int.
	 *
	 *		--- C++
	 *		int x[12], y;
	 *		---
	 *
	 *		Here [12] is a parameter modifier, as the type of x is int[12] but the type of y is just int.
	 *
	 *		ParamModifier - Any parameter modifiers.  These usually appear with the name but are part of the type, and aren't
	 *							    shared with other parameters inheriting the type, such as "*" in "int *x" in C++.
	 *		OpeningParamModifier - An opening symbol modifying a parameter.  These usually appear with the name but are part
	 *											of the type, such as "[" in "int x[5]".  Can be followed by <OpeningExtensionSymbols> for
	 *											multi-token symbols.
	 *		ClosingParamModifier - A closing symbol modifying a parameter.  These usually appear with the name but are part of
	 *										   the type, such as "]" in "int x[5]".  Can be followed by <ClosingExtensionSymbols> for
	 *										   multi-token symbols.
	 *
	 *
	 * Default Values:
	 *
	 *		DefaultValueSeparator - The symbol separating the name and type from its default value, such as "=" or ":=".
	 *		DefaultValue - The default value of the parameter.
	 *
	 *
	 *	Properties:
	 *
	 *		Properties are key/value pairs appearing in metadata, such as decorators in Python or annotations in Java:
	 *
	 *		--- Python
	 *		@decorator(arg1 = 12,
	 *		           arg2 = "text")
	 *		def DecoratedFunction ()
	 *		---
	 *
	 *		They're also used for attributes in SystemVerilog:
	 *
	 *		--- SystemVerilog
	 *		(* arg1 = 12,
	 *		   arg2 = "text" *)
	 *		module ModuleWithAttributes ()
	 *		---
	 *
	 *		Property names are marked with standard <Name> tags.
	 *
	 *		PropertyValueSeparator - The symbol separating a property name from its value, such as "=" or ":".
	 *		PropertyValue - The value of a property, such as "12" in "@RequestForEnhancement(id = 12)" in Java annotations.
	 *
	 *
	 *	Extension Symbols:
	 *
	 *		Some functions may need to skip blocks marked with opening and closing tokens like <OpeningTypeModifier> and
	 *		<ClosingTypeModifier>.  They would need to skip nested blocks as well.  This could be a problem when using unbalanced
	 *		symbol pairs like #( and ) since there would be two opening symbols and one closing one, leading the code to think there
	 *		was a nested modifier that wasn't closed.  If you use extension symbols it would be able to see that they are not two
	 *		separate opening symbols.
	 *
	 *		OpeningExtensionSymbol - Symbols following an opening symbol which are considered part of it, such as the asterisk in (*.
	 *		ClosingExtensionSymbol - Symbols following a closing symbol which are considered part of it, such as the closing parenthesis
	 *											   in *).
	 *
	 */
	public enum PrototypeParsingType :  byte
		{
		Null = 0,

		StartOfPrototypeSection, EndOfPrototypeSection,

		StartOfParams, EndOfParams, ParamSeparator, OpeningParamDecorator, ClosingParamDecorator,

		Type, TypeModifier, TypeQualifier, OpeningTypeModifier, ClosingTypeModifier,

		StartOfTuple, EndOfTuple, TupleMemberSeparator, TupleMemberName,

		NameTypeSeparator, Name, KeywordName,

		ParamModifier, OpeningParamModifier, ClosingParamModifier,

		DefaultValueSeparator, DefaultValue,

		PropertyValueSeparator, PropertyValue,

		OpeningExtensionSymbol, ClosingExtensionSymbol
		}


	/* Enum: ClassPrototypeParsingType
	 *
	 * Null - Returned when the token is out of bounds or one of these values hasn't been assigned to it yet.
	 *
	 * StartOfPrePrototypeLine - The first token of a new pre-prototype line.  Each token marked with this starts a new line.
	 * PrePrototypeLine - Part of a line that should be shown before the prototype.
	 *
	 * StartOfParents - The start of a parent list.
	 * ParentSeparator - A separator between parents, such as a comma.
	 * EndOfParents - The end of a parent list for languages like Python that use parenthesis.
	 *
	 * Modifier - A separate word modifying the class or parent, such as "public" or "static".
	 * Keyword - The keyword used to declare the class, such as "class", "struct", or "interface".
	 * Name - The name of the class or parent including qualifiers, such as "PkgA.PkgB.Class".
	 *
	 * TemplateSuffix - Extra template information after a class or parent, such as "<T>" in "List<T>".
	 *
	 * StartOfPostPrototypeLine - The first token of a new post-prototype line.  Each token marked with this starts a new line.
	 * PostPrototypeLine - Part of a line that should be shown after the prototype.
	 *
	 * StartOfBody - The start of the class's body if it is present in the prototype.  Nothing beyond that is demarcated, including the end
	 *								of the body.  This token is just present to indicate that the prototype does contain a body.
	 */
	public enum ClassPrototypeParsingType :  byte
		{
		Null = 0,

		StartOfPrePrototypeLine, PrePrototypeLine,

		StartOfParents, ParentSeparator, EndOfParents,

		Modifier, Keyword, Name,

		TemplateSuffix,

		StartOfPostPrototypeLine, PostPrototypeLine,

		StartOfBody
		}

	}
