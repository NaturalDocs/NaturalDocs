/* 
 * Struct: GregValure.NaturalDocs.Engine.Tokenization.Token
 * ____________________________________________________________________________
 * 
 * Represents a token in a string.  Managed by <Tokenizer>.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2011 Greg Valure.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;


namespace GregValure.NaturalDocs.Engine.Tokenization
	{

	public struct Token
		{
		/* Var: Type
		 * The token type.
		 */
		public TokenType Type;
		
		/* Var: Length
		 * The length of the token, up to 255 characters.
		 */
		public byte Length;
		}
	}