/* 
 * Class: GregValure.NaturalDocs.Engine.Tokenization.Exceptions.InvalidConversion
 * ____________________________________________________________________________
 * 
 * Thrown when attempting to change a token from one <Tokenization.TokenType> to another in an
 * unsupported way.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2011 Greg Valure.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;


namespace GregValure.NaturalDocs.Engine.Tokenization.Exceptions
	{
	public class InvalidConversion : Exception
		{
		public InvalidConversion (TokenType from, TokenType to)
			: base ("Cannot convert token from " + from + " to " + to + ".")
			{
			}
		}
	}