/* 
 * Class: GregValure.NaturalDocs.Engine.Output.Shrinker
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
using System.Text.RegularExpressions;
using GregValure.NaturalDocs.Engine.Collections;
using GregValure.NaturalDocs.Engine.Comments;
using GregValure.NaturalDocs.Engine.Tokenization;

namespace GregValure.NaturalDocs.Engine.Output
	{
	public class Shrinker
		{

		// Group: Functions
		// __________________________________________________________________________

		public Shrinker ()
			{
			source = null;
			output = null;
			substitutions = null;
			}

		public string ShrinkJS (string javascript)
			{
			source = new Tokenizer(javascript);
			output = new StringBuilder(javascript.Length / 2);  // Guess, but better than nothing.
			substitutions = new StringToStringTable(false, false);


			// Search comments for sections to include in the output and substitution definitions.

			Languages.Language jsLanguage = new Languages.Language("JavaScript");
			jsLanguage.LineCommentStrings = new string[] { "//" };
			jsLanguage.BlockCommentStringPairs = new string[] { "/*", "*/" };

			List<PossibleDocumentationComment> comments = jsLanguage.GetComments(source);

			foreach (PossibleDocumentationComment comment in comments)
				{  ProcessComment(comment);  }

			if (output.Length > 0)
				{  
				output.AppendLine(" */");
				output.AppendLine();  
				}


			// Build the condensed output.

			TokenIterator iterator = source.FirstToken;

			#if !DONT_SHRINK_FILES
				string spaceSeparatedSymbols = "+-";
			#endif

			string regexPrefixCharacters = "({[,;:=&|!?\0";
			char lastNonWSChar = '\0';

			while (iterator.IsInBounds)
				{
				TokenIterator prevIterator = iterator;
				char lastChar = (output.Length > 0 ? output[output.Length - 1] : '\0');

				if (lastChar != ' ' && lastChar != '\t')
					{  lastNonWSChar = lastChar;  }

				if (TryToSkipWhitespace(ref iterator, false) == true)
					{
					#if DONT_SHRINK_FILES
						source.AppendTextBetweenTo(prevIterator, iterator, output);
					#else
					char nextChar = iterator.Character; 

					if ( nextChar == '`' ||
						  (spaceSeparatedSymbols.IndexOf(lastChar) != -1 &&
						   spaceSeparatedSymbols.IndexOf(nextChar) != -1) ||
						  (Tokenizer.FundamentalTypeOf(lastChar) == FundamentalType.Text &&
						   Tokenizer.FundamentalTypeOf(nextChar) == FundamentalType.Text) )
						{  output.Append(' ');  }
					#endif
					}
				else if (iterator.Character == '`')
					{
					string substitution = null;

					if (TryToGetSubstitution(ref iterator, out substitution))
						{  
						output.Append(substitution);  
						}
					else
						{
						output.Append('`');
						iterator.Next();
						}
					}
				else
					{
					if (TryToSkipString(ref iterator) == true ||
							(regexPrefixCharacters.IndexOf(lastNonWSChar) != -1 && TryToSkipRegex(ref iterator) == true) )
						{  }
					else
						{  iterator.Next();  }

					source.AppendTextBetweenTo(prevIterator, iterator, output);
					}
				}

			return output.ToString();
			}


		public string ShrinkCSS (string css)
			{
			source = new Tokenizer(css);
			output = new StringBuilder(css.Length / 2);  // Guess, but better than nothing.
			substitutions = new StringToStringTable(false, false);


			// Search comments for sections to include in the output and substitution definitions.

			Languages.Language cssLanguage = new Languages.Language("CSS");
			cssLanguage.BlockCommentStringPairs = new string[] { "/*", "*/" };

			List<PossibleDocumentationComment> comments = cssLanguage.GetComments(source);

			foreach (PossibleDocumentationComment comment in comments)
				{  ProcessComment(comment);  }

			if (output.Length > 0)
				{  
				output.AppendLine(" */");
				output.AppendLine();  
				}


			// Build the condensed output.

			TokenIterator iterator = source.FirstToken;

			// We have to be more cautious than the JS shrinker.  You don't want something like "head .class" to become
			// "head.class".  Colon is a special case because we only want to remove spaces after it ("font-size: 12pt")
			// and not before ("body :link").
			#if !DONT_SHRINK_FILES
				string safeToCondenseAround = "{},;:+>[]=\0";
			#endif

			while (iterator.IsInBounds)
				{
				TokenIterator prevIterator = iterator;
				char lastChar = (output.Length > 0 ? output[output.Length - 1] : '\0');

				if (TryToSkipWhitespace(ref iterator, true) == true)
					{
					#if DONT_SHRINK_FILES
						source.AppendTextBetweenTo(prevIterator, iterator, output);
					#else
						char nextChar = iterator.Character; 

						if (nextChar == ':' ||
							  (safeToCondenseAround.IndexOf(lastChar) == -1 &&
								safeToCondenseAround.IndexOf(nextChar) == -1) )
							{  output.Append(' ');  }
					#endif
					}
				else if (TryToSkipString(ref iterator) == true)
					{
					source.AppendTextBetweenTo(prevIterator, iterator, output);
					}
				else if (iterator.Character == '`')
					{
					string substitution = null;

					if (TryToGetSubstitution(ref iterator, out substitution))
						{  
						output.Append(substitution);  
						}
					else
						{
						output.Append('`');
						iterator.Next();
						}
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

			return output.ToString();
			}


		/* Function: ProcessComment
		 * Searches the passed comment for sections that should be included in the output or for substitution definitions.
		 * Comments that should be included in the output will be added to <output>.
		 */
		protected void ProcessComment (PossibleDocumentationComment comment)
			{
			LineIterator iterator = comment.Start;

			while (iterator < comment.End)
				{
				if (iterator.Match(substitutionHeaderRegex, LineBoundsMode.CommentContent).Success)
					{
					iterator.Next();

					while (iterator < comment.End && 
								iterator.Match(keepInOutputRegex, LineBoundsMode.CommentContent).Success == false)
						{
						Match match = iterator.Match(substitutionDefinitionRegex, LineBoundsMode.CommentContent);

						if (match.Success)
							{
							substitutions[ match.Groups[1].ToString() ] = match.Groups[2].ToString();
							}

						iterator.Next();
						}
					}

				#if !DONT_SHRINK_FILES
				// We only need to worry about Keep in Output if we're shrinking files.  Unshrunk files will have the comments
				// anyway, and adding them again would throw off the line numbers compared to the original.
				else if (iterator.Match(keepInOutputRegex, LineBoundsMode.CommentContent).Success == true)
					{
					iterator.Next();

					while (iterator < comment.End && iterator.IsEmpty(LineBoundsMode.CommentContent))
						{  iterator.Next();  }

					LineIterator start = iterator;
					LineIterator end = iterator;
					int commonIndent = 9999;

					while (iterator < comment.End && 
								iterator.Match(keepInOutputRegex, LineBoundsMode.CommentContent).Success == false &&
								iterator.Match(substitutionHeaderRegex, LineBoundsMode.CommentContent).Success == false)
						{
						if (iterator.IsEmpty(LineBoundsMode.CommentContent))
							{  iterator.Next();  }
						else
							{
							if (iterator.Indent(LineBoundsMode.CommentContent) < commonIndent)
								{  commonIndent = iterator.Indent(LineBoundsMode.CommentContent);  }

							iterator.Next();
							end = iterator;
							}
						}

					if (start < end)
						{
						if (output.Length > 0)
							{  output.AppendLine(" *");  }

						do
							{
							if (start.IsEmpty(LineBoundsMode.CommentContent))
								{  output.AppendLine(" *");  }
							else
								{
								if (output.Length == 0)
									{  output.Append("/* ");  }
								else
									{  output.Append(" * ");  }

								int indentDifference = start.Indent(LineBoundsMode.CommentContent) - commonIndent;

								if (indentDifference > 0)
									{  output.Append(' ', indentDifference);  }

								start.AppendTo(output, LineBoundsMode.CommentContent);
								output.AppendLine();
								}

							start.Next();
							}
						while (start < end);
						}
					}
				#endif

				else
					{  iterator.Next();  }
				}
			}



		// Group: Parsing Functions
		// __________________________________________________________________________


		/* Function: TryToSkipWhitespace
		 * If the iterator is on whitespace or a comment, skips over it and returns true.  Otherwise leaves the iterator alone and
		 * returns false.
		 */
		protected bool TryToSkipWhitespace (ref TokenIterator iterator, bool blockCommentsOnly)
			{
			bool result = false;

			for (;;)
				{
				if (iterator.FundamentalType == FundamentalType.Whitespace || 
					 iterator.FundamentalType == FundamentalType.LineBreak)
					{
					iterator.Next();
					result = true;
					}
				else if (TryToSkipBlockComment(ref iterator) || (!blockCommentsOnly && TryToSkipLineComment(ref iterator)) )
					{  
					result = true;  
					}
				else
					{  return result;  }
				}
			}

		/* Function: TryToSkipLineComment
		 * If the iterator is on the opening symbol of a line comment, skips over it and returns true.  Otherwise leaves the iterator
		 * alone and returns false.
		 */
		protected bool TryToSkipLineComment (ref TokenIterator iterator)
			{
			if (iterator.MatchesAcrossTokens("//"))
				{
				iterator.NextByCharacters(2);

				while (iterator.IsInBounds && iterator.FundamentalType != FundamentalType.LineBreak)
					{  iterator.Next();  }

				iterator.Next();
				return true;
				}
			else
				{  return false;  }
			}

		/* Function: TryToSkipBlockComment
		 * If the iterator is on the opening symbol of a block comment, skips over it and returns true.  Otherwise leaves the iterator
		 * alone and returns false.
		 */
		protected bool TryToSkipBlockComment (ref TokenIterator iterator)
			{
			if (iterator.MatchesAcrossTokens("/*"))
				{
				iterator.NextByCharacters(2);

				while (iterator.IsInBounds && !iterator.MatchesAcrossTokens("*/"))
					{  iterator.Next();  }

				iterator.NextByCharacters(2);
				return true;
				}
			else
				{  return false;  }
			}

		/* Function: TryToSkipString
		 * If the iterator is on the opening symbol of a string, skips over it and returns true.  Otherwise leaves the iterator alone and
		 * returns false.
		 */
		protected bool TryToSkipString (ref TokenIterator iterator)
			{
			return (TryToSkipQuotedText(ref iterator, '\'') || TryToSkipQuotedText(ref iterator, '\"'));
			}

		/* Function: TryToSkipRegex
		 * If the iterator is on the opening symbol of a regular expression, skips over it and returns true.  Otherwise leaves the iterator
		 * alone and returns false.
		 */
		protected bool TryToSkipRegex (ref TokenIterator iterator)
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
		protected bool TryToSkipQuotedText (ref TokenIterator iterator, char quoteChar)
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


		/* Function: TryToGetSubstitution
		 * If the iterator is on a valid substitution token, advances it past it, returns the replacement text, and returns true.
		 * Otherwise the iterator will be left alone and it will return false.
		 */
		protected bool TryToGetSubstitution (ref TokenIterator iterator, out string substitution)
			{
			if (iterator.Character != '`')
				{
				substitution = null;
				return false;
				}

			TokenIterator tokenIterator = iterator;
			tokenIterator.Next();

			StringBuilder token = new StringBuilder();
			string tokenSubstitution = null;

			// Locale substitutions
			if (tokenIterator.MatchesAcrossTokens("Locale{"))
				{
				tokenIterator.NextByCharacters(7);

				while (tokenIterator.IsInBounds && tokenIterator.Character != '}')
					{  
					tokenIterator.AppendTokenTo(token);
					tokenIterator.Next();  
					}

				if (tokenIterator.Character != '}')
					{
					substitution = null;
					return false;
					}

				tokenIterator.Next();

				string tokenString = token.ToString();
				tokenSubstitution = Engine.Locale.SafeGet("NaturalDocs.Engine", tokenString, tokenString);
				tokenSubstitution = '"' + tokenSubstitution.StringEscape() + '"';
				}

			// Standard comment substitutions
			else
				{
				while (tokenIterator.IsInBounds && (tokenIterator.FundamentalType == FundamentalType.Text || 
							 tokenIterator.Character == '.' || tokenIterator.Character == '_'))
					{
					tokenIterator.AppendTokenTo(token);
					tokenIterator.Next();
					}

				tokenSubstitution = substitutions[token.ToString()];
				}

			if (tokenSubstitution != null)
				{
				substitution = tokenSubstitution;
				iterator = tokenIterator;
				return true;
				}
			else
				{
				substitution = null;
				return false;
				}
			}


		// Group: Variables
		// __________________________________________________________________________

		protected Tokenizer source;
		protected StringBuilder output;
		protected StringToStringTable substitutions;

		
		// Group: Static Variables
		// __________________________________________________________________________


		protected static Regex.Comments.Shrinker.KeepInOutput keepInOutputRegex = new Regex.Comments.Shrinker.KeepInOutput();
		protected static Regex.Comments.Shrinker.SubstitutionDefinition substitutionDefinitionRegex = new Regex.Comments.Shrinker.SubstitutionDefinition();
		protected static Regex.Comments.Shrinker.SubstitutionHeader substitutionHeaderRegex = new Regex.Comments.Shrinker.SubstitutionHeader();

		}
	}