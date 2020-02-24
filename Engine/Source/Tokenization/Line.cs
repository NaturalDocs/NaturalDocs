/* 
 * Struct: CodeClear.NaturalDocs.Engine.Tokenization.Line
 * ____________________________________________________________________________
 * 
 * A struct representing a line in a string.  Managed by <Tokenizer>.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2020 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;


namespace CodeClear.NaturalDocs.Engine.Tokenization
	{
	public struct Line
		{

		/* var: TokenLength
		 * The length of the line in tokens.  This includes the trailing line break if present.
		 */
		public int TokenLength;
		
		/* var: RawTextLength
		 * The length of the line in characters.  This includes the trailing line break if present.
		 */
		public int RawTextLength;

		}
	}