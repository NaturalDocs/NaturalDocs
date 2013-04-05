/* 
 * Class: GregValure.NaturalDocs.Engine.StringBuilderExtensions
 * ____________________________________________________________________________
 * 
 * A static class for all the functions added to the StringBuilder type.
 * 
 */

// This file is part of Natural Docs, which is Copyright © 2003-2013 Greg Valure.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Text;


namespace GregValure.NaturalDocs.Engine
	{
	static public class StringBuilderExtensions
		{
		
		/* Function: EntityEncodeAndAppend
		 * Appends the input character to the StringBuilder with <, >, ", and & replaced by their entity encodings.
		 */
		public static void EntityEncodeAndAppend (this StringBuilder output, char input)
			{
			output.Append(input.EntityEncode());
			}


		/* Function: EntityEncodeAndAppend
		 * Appends the contents of the input string to the StringBuilder with <, >, ", and & replaced by their entity 
		 * encodings.
		 */
		public static void EntityEncodeAndAppend (this StringBuilder output, string input)
			{
			output.EntityEncodeAndAppend(input, 0, input.Length);
			}
		
		
		/* Function: EntityEncodeAndAppend
		 * Appends the contents of the input string to the StringBuilder with <, >, ", and & replaced by their entity 
		 * encodings.  Offset and length represent the portion of the input string to convert.
		 */
		public static void EntityEncodeAndAppend (this StringBuilder output, string input, int offset, int length)
			{
			int endOfInput = offset + length;
			
			while (offset < endOfInput)
				{
				int nextEntityChar = input.IndexOfAny(StringExtensions.EntityCharLiterals, offset, endOfInput - offset);
				
				if (nextEntityChar == -1)
					{  break;  }
				
				if (nextEntityChar != offset)
					{  output.Append(input, offset, nextEntityChar - offset);  }
					
				output.Append( input[nextEntityChar].EntityEncode() );
					
				offset = nextEntityChar + 1;
				}
				
			if (offset < endOfInput)
				{  output.Append(input, offset, endOfInput - offset);  }
			}
			
			
		/* Function: StringEscapeAndAppend
		 * Appends the contents of the input string to the StringBuilder with ', ", and \ escaped.
		 */
		public static void StringEscapeAndAppend (this StringBuilder output, string input)
			{
			output.StringEscapeAndAppend(input, 0, input.Length);
			}
		
		
		/* Function: StringEscapeAndAppend
		 * Appends the contents of the input string to the StringBuilder with ', ", and \ escaped.  Offset and length 
		 * represent the portion of the input string to convert.
		 */
		public static void StringEscapeAndAppend (this StringBuilder output, string input, int offset, int length)
			{
			int endOfInput = offset + length;
			
			while (offset < endOfInput)
				{
				int nextEscapedChar = input.IndexOfAny(StringExtensions.EscapedStringChars, offset, endOfInput - offset);
				
				if (nextEscapedChar == -1)
					{  break;  }
				
				if (nextEscapedChar != offset)
					{  output.Append(input, offset, nextEscapedChar - offset);  }
					
				output.Append('\\');
				output.Append(input[nextEscapedChar]);
					
				offset = nextEscapedChar + 1;
				}
				
			if (offset < endOfInput)
				{  output.Append(input, offset, endOfInput - offset);  }
			}
		
		}
	}