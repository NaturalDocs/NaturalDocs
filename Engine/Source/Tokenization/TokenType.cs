/* 
 * Enum: GregValure.NaturalDocs.Engine.Tokenization.TokenType
 * ____________________________________________________________________________
 * 
 * Fundamental Types:
 * 
 *		LineBreak - One line break in CR, LF, or CR/LF format.
 *		Whitespace - A series of consecutive space and/or tab characters.
 *		Text - A series of consecutive ASCII letters, numbers, and/or characters above ASCII 0x7F.
 *				 This does *not* include underscores.
 *		Symbol - One character not mentioned above, which are all the symbol characters available
 *					 on the standard US Qwerty keyboard plus ASCII control characters.
 *		Null - Returned when the token iterator is out of bounds.
 *		
 * 
 * Code Types:
 * 
 *		CommentSymbol - A comment symbol or part of one.
 *		CommentDecoration - A symbol that only provides decoration for a comment, such as part of 
 *										 a horizontal line.
 *										 
 * 
 * Natural Docs Content Types:
 * 
 *		PossibleOpeningTag - An opening symbol that's a candidate for being part of a link, bold, or underline tag.
 *		PossibleClosingTag - A closing symbol that's a candidate for being part of a link, bold, or underline tag.
 *		OpeningTag - An opening symbol that's a part of a link, bold, or underline tag.
 *		ClosingTag - A closing symbol that's a part of a link, bold, or underline tag.
 *		URL - A URL.
 *		EMail - An e-mail address.
 *		
 *		
 * Range Values:
 * 
 *		EndOfFundamentalTypes - Values lower than this are fundamental types.
 * 
 */

// This file is part of Natural Docs, which is Copyright © 2003-2011 Greg Valure.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;


namespace GregValure.NaturalDocs.Engine.Tokenization
	{
	public enum TokenType : byte
		{  
		Null, LineBreak, Whitespace, Text, Symbol,
			
		EndOfFundamentalTypes,
		
		CommentSymbol, CommentDecoration,
		
		PossibleOpeningTag, PossibleClosingTag,
		OpeningTag, ClosingTag,
		URL, EMail

		}
	}