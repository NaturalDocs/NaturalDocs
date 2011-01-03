/* 
 * Struct: GregValure.NaturalDocs.Engine.Output.Shrinker
 * ____________________________________________________________________________
 * 
 * A class used to condense JavaScript and CSS so that it doesn't contain any unnecessary comments or whitespace.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2011 Greg Valure.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;
using System.Text;
using GregValure.NaturalDocs.Engine.Tokenization;

namespace GregValure.NaturalDocs.Engine.Output
	{
	public static class Shrinker
		{

		// Group: Functions
		// __________________________________________________________________________

		public static string ShrinkJS (string javascript)
			{
			Tokenizer source = new Tokenizer(javascript);
			StringBuilder output = new StringBuilder(javascript.Length / 2);  // Guess, but better than nothing.
			List<string> savedComments = new List<string>();

			TokenIterator iterator = source.FirstToken;

			string spaceSeparatedSymbols = "+-";
			string regexPrefixCharacters = "({[,;:=&|!?\0";

			while (iterator.IsInBounds)
				{
				TokenIterator prevIterator = iterator;
				char lastChar = (output.Length > 0 ? output[output.Length - 1] : '\0');

				if (TryToSkipWhitespace(ref iterator, savedComments, false) == true)
					{
					char nextChar = iterator.Character; 

					if ( (spaceSeparatedSymbols.IndexOf(lastChar) != -1 &&
						   spaceSeparatedSymbols.IndexOf(nextChar) != -1) ||
						  (Tokenizer.FundamentalTypeOf(lastChar) == TokenType.Text &&
						   Tokenizer.FundamentalTypeOf(nextChar) == TokenType.Text) )
						{  output.Append(' ');  }
					}
				else
					{
					if (TryToSkipString(ref iterator) == true ||
						 (regexPrefixCharacters.IndexOf(lastChar) != -1 && TryToSkipRegex(ref iterator) == true) )
						{  }
					else
						{  iterator.Next();  }

					source.AppendTextBetweenTo(prevIterator, iterator, output);
					}
				}

			if (savedComments.Count > 0)
				{
				StringBuilder combinedSaved = new StringBuilder();

				foreach (string savedComment in savedComments)
					{
					combinedSaved.Append(savedComment);
					combinedSaved.AppendLine();
					combinedSaved.AppendLine();
					}

				output.Insert(0, combinedSaved);
				}
			
			return output.ToString();
			}


		public static string ShrinkCSS (string css)
			{
			Tokenizer source = new Tokenizer(css);
			StringBuilder output = new StringBuilder(css.Length / 2);  // Guess, but better than nothing.
			List<string> savedComments = new List<string>();

			TokenIterator iterator = source.FirstToken;

			// We have to be more cautious than the JS shrinker.  You don't want something like "head .class" to become
			// "head.class".  Colon is a special case because we only want to remove spaces after it ("font-size: 12pt")
			// and not before ("body :link").
			string safeToCondenseAround = "{},;:+>[]=\0";

			while (iterator.IsInBounds)
				{
				TokenIterator prevIterator = iterator;
				char lastChar = (output.Length > 0 ? output[output.Length - 1] : '\0');

				if (TryToSkipWhitespace(ref iterator, savedComments, true) == true)
					{
					char nextChar = iterator.Character; 

					if (nextChar == ':' ||
						  (safeToCondenseAround.IndexOf(lastChar) == -1 &&
						   safeToCondenseAround.IndexOf(nextChar) == -1) )
						{  output.Append(' ');  }
					}
				else if (TryToSkipString(ref iterator) == true)
					{
					source.AppendTextBetweenTo(prevIterator, iterator, output);
					}
				else
					{
					if (iterator.Character == '}' && lastChar == ';')
						{
						// Semicolons are unnecessary at the end of blocks.  However, we have to do this here instead of in a 
						// global search and replace for ";}" because we don't want to alter that sequence if it appears in a string.
						output[output.Length - 1] = '}';
						}
					else
						{  iterator.AppendTokenTo(output);  }

					iterator.Next();
					}
				}

			if (savedComments.Count > 0)
				{
				StringBuilder combinedSaved = new StringBuilder();

				foreach (string savedComment in savedComments)
					{
					combinedSaved.Append(savedComment);
					combinedSaved.AppendLine();
					combinedSaved.AppendLine();
					}

				output.Insert(0, combinedSaved);
				}

			return output.ToString();
			}


		// Group: Parsing Functions
		// __________________________________________________________________________


		/* Function: TryToSkipWhitespace
		 * If the iterator is on whitespace or a comment, skips over it and returns true.  Otherwise leaves the iterator alone and
		 * returns false.  If any comments contain the text "(include in output)" it will be added to savedComments.
		 */
		private static bool TryToSkipWhitespace (ref TokenIterator iterator, List<string> savedComments, bool blockCommentsOnly)
			{
			bool result = false;

			for (;;)
				{
				if (iterator.Type == TokenType.Whitespace || iterator.Type == TokenType.LineBreak)
					{
					iterator.Next();
					result = true;
					}
				else if (TryToSkipBlockComment(ref iterator, savedComments) || 
							 (!blockCommentsOnly && TryToSkipLineComment(ref iterator, savedComments)) )
					{  
					result = true;  
					}
				else
					{  return result;  }
				}
			}

		/* Function: TryToSkipLineComment
		 * If the iterator is on the opening symbol of a line comment, skips over it and returns true.  Otherwise leaves the iterator
		 * alone and returns false.  If any comments contain the text "(include in output)" it will be added to savedComments.
		 */
		private static bool TryToSkipLineComment (ref TokenIterator iterator, List<string> savedComments)
			{
			if (iterator.MatchesAcrossTokens("//"))
				{
				TokenIterator beginningOfComment = iterator;

				for (;;)
					{
					iterator.NextByCharacters(2);

					while (iterator.IsInBounds && iterator.Type != TokenType.LineBreak)
						{  iterator.Next();  }

					iterator.Next();

					// See if there's another // starting the next line.  If so we want to include it as part of this comment for the purposes
					// of finding "(include in output)".
					TokenIterator lookahead = iterator;

					while (lookahead.Type == TokenType.Whitespace)
						{  lookahead.Next();  }

					if (lookahead.MatchesAcrossTokens("//"))
						{  iterator = lookahead;  }
					else
						{  break;  }
					}

				System.Text.RegularExpressions.Match match = 
					iterator.Tokenizer.MatchTextBetween(keepInOutputRegex, beginningOfComment, iterator);

				if (match.Success)
					{
					string comment = iterator.Tokenizer.TextBetween(beginningOfComment, iterator);
					comment = comment.Remove(match.Index - beginningOfComment.RawTextIndex, match.Length);
					savedComments.Add(comment);
					}

				return true;
				}
			else
				{  return false;  }
			}

		/* Function: TryToSkipBlockComment
		 * If the iterator is on the opening symbol of a block comment, skips over it and returns true.  Otherwise leaves the iterator
		 * alone and returns false.  If any comments contain the text "(include in output)" it will be added to savedComments.
		 */
		private static bool TryToSkipBlockComment (ref TokenIterator iterator, List<string> savedComments)
			{
			if (iterator.MatchesAcrossTokens("/*"))
				{
				TokenIterator beginningOfComment = iterator;
				iterator.NextByCharacters(2);

				while (iterator.IsInBounds && !iterator.MatchesAcrossTokens("*/"))
					{  iterator.Next();  }

				iterator.NextByCharacters(2);

				System.Text.RegularExpressions.Match match = 
					iterator.Tokenizer.MatchTextBetween(keepInOutputRegex, beginningOfComment, iterator);

				if (match.Success)
					{
					string comment = iterator.Tokenizer.TextBetween(beginningOfComment, iterator);
					comment = comment.Remove(match.Index - beginningOfComment.RawTextIndex, match.Length);
					savedComments.Add(comment);
					}

				return true;
				}
			else
				{  return false;  }
			}

		/* Function: TryToSkipString
		 * If the iterator is on the opening symbol of a string, skips over it and returns true.  Otherwise leaves the iterator alone and
		 * returns false.
		 */
		private static bool TryToSkipString (ref TokenIterator iterator)
			{
			return (TryToSkipQuotedText(ref iterator, '\'') || TryToSkipQuotedText(ref iterator, '\"'));
			}

		/* Function: TryToSkipRegex
		 * If the iterator is on the opening symbol of a regular expression, skips over it and returns true.  Otherwise leaves the iterator
		 * alone and returns false.
		 */
		private static bool TryToSkipRegex (ref TokenIterator iterator)
			{
			// Starts and ends with a slash except when escaped.  Just like a string right?
			return TryToSkipQuotedText(ref iterator, '/');
			}

		/* Function: TryToSkipQuotedText
		 * If the iterator is on the opening symbol of a section of quated text as specified by the passed character, skips over it and 
		 * returns true.  Otherwise leaves the iterator alone and returns false.  
		 * 
		 * Quoted text is a segment that starts and ends with the passed character.  Everything in between is part of the quoted section
		 * until it reaches the character again, excluding when that character is preceded by a backslash.
		 */
		private static bool TryToSkipQuotedText (ref TokenIterator iterator, char quoteChar)
			{
			if (iterator.Character == quoteChar)
				{
				iterator.Next();

				while (iterator.IsInBounds)
					{
					if (iterator.Character == '\\')
						{  iterator.Next(2);  }

					else if (iterator.Character == quoteChar)
						{
						iterator.Next();
						break;
						}

					else
						{  iterator.Next();  }
					}

				return true;
				}
			else
				{  return false;  }
			}

		private static Regex.Comments.KeepInOutput keepInOutputRegex = new Regex.Comments.KeepInOutput();

		}
	}