/*
 * Class: CodeClear.NaturalDocs.Engine.Output.ResourceProcessor
 * ____________________________________________________________________________
 *
 * A base class used for shared functionality when processing JS and CSS files.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2017 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Text;
using System.Text.RegularExpressions;
using CodeClear.NaturalDocs.Engine.Collections;
using CodeClear.NaturalDocs.Engine.Comments;
using CodeClear.NaturalDocs.Engine.Tokenization;

namespace CodeClear.NaturalDocs.Engine.Output
	{
	public abstract class ResourceProcessor
		{

		// Group: Functions
		// __________________________________________________________________________

		static ResourceProcessor ()
			{
			IncludeInOutputRegex = new Regex.Comments.IncludeInOutput();
			}

		public ResourceProcessor ()
			{
			source = null;
			output = null;
			substitutions = null;
			}


		/* Function: Process
		 */
		abstract public string Process (string input, bool shrink = true);


		/* Function: IncludeInOutput
		 * Extracts and returns any comment content marked "include in output".  All comment symbols and extra indents
		 * will be removed.  Returns null if the comment doesn't have any.
		 */
		protected string IncludeInOutput (PossibleDocumentationComment comment)
			{
			Comments.LineFinder.MarkTextBoxes(comment);

			LineIterator iterator = comment.Start;


			// Find the "include in output" header if it exists

			while (iterator < comment.End &&
					 iterator.Match(IncludeInOutputRegex, LineBoundsMode.CommentContent).Success == false)
				{  iterator.Next();  }

			if (iterator >= comment.End)
				{  return null;  }


			// Find the bounds of the content excluding whitespace and the shared indent level

			iterator.Next();

			while (iterator < comment.End && iterator.IsEmpty(LineBoundsMode.CommentContent))
				{  iterator.Next();  }

			LineIterator start = iterator;
			LineIterator end = iterator;
			int commonIndent = 9999;

			while (iterator < comment.End)
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


			// Build and return the comment content

			if (start >= end)
				{  return null;  }

			StringBuilder output = new StringBuilder();

			do
				{
				int indentDifference = start.Indent(LineBoundsMode.CommentContent) - commonIndent;

				if (indentDifference > 0)
					{  output.Append(' ', indentDifference);  }

				start.AppendTo(output, LineBoundsMode.CommentContent);
				output.AppendLine();

				start.Next();
				}
			while (start < end);

			return output.ToString();
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


		/* Function: TryToSkipSubstitutionDefinition
		 * 
		 * If the iterator is on a valid substitution definition, advances it past it, determines its properties, and returns true.  Otherwise the 
		 * iterator will be left alone and it will return false.
		 * 
		 * identifier - "$identifier" in "$identifier = value;"
		 * value - "value" in "$identifier = value;"
		 * declaration - "$identifier = value;" in "$identifier = value;"
		 */
		protected bool TryToSkipSubstitutionDefinition (ref TokenIterator iterator, out string identifier, out string value, out string declaration)
			{
			identifier = null;
			value = null;
			declaration = null;

			if (iterator.Character != '$' && iterator.Character != '@')
				{  return false;  }
			
			TokenIterator lookahead = iterator;
			lookahead.Next();

			if (!lookahead.IsInBounds ||
				 (lookahead.FundamentalType != FundamentalType.Text &&
				  lookahead.Character != '_'))
				{  return false;  }

			do
				{  lookahead.Next();  }
			while (lookahead.IsInBounds &&
					 (lookahead.FundamentalType == FundamentalType.Text ||
					  lookahead.Character == '_'));

			TokenIterator endOfIdentifier = lookahead;
			lookahead.NextPastWhitespace();

			if (lookahead.Character != ':' && lookahead.Character != '=')
				{  return false;  }

			lookahead.Next();
			lookahead.NextPastWhitespace();
			
			TokenIterator startOfValue = lookahead;

			while (lookahead.IsInBounds && lookahead.Character != ';')
				{
				if (TryToSkipLineComment(ref lookahead) ||
					TryToSkipBlockComment(ref lookahead) ||
					lookahead.FundamentalType == FundamentalType.LineBreak ||
					lookahead.Character == '{')
					{  return false;  }

				if (!TryToSkipString(ref lookahead))
					{  lookahead.Next();  }
				}

			if (lookahead.Character != ';')
				{  return false;  }

			identifier = iterator.Tokenizer.TextBetween(iterator, endOfIdentifier);
			value = iterator.Tokenizer.TextBetween(startOfValue, lookahead);

			lookahead.Next();

			declaration = iterator.Tokenizer.TextBetween(iterator, lookahead);

			iterator = lookahead;
			return true;
			}


		/* Function: TryToSkipSubstitution
		 * If the iterator is on a valid substitution identifier, advances it past it, returns the replacement text, and returns true.
		 * Otherwise the iterator will be left alone and it will return false.
		 */
		protected bool TryToSkipSubstitution (ref TokenIterator iterator, out string substitution)
			{
			if (iterator.Character != '$' && iterator.Character != '@')
				{
				substitution = null;
				return false;
				}

			TokenIterator startOfIdentifier = iterator;
			TokenIterator lookahead = iterator;
			lookahead.Next();

			// Locale substitutions
			if (lookahead.MatchesAcrossTokens("Locale{"))
				{
				lookahead.NextByCharacters(7);
				TokenIterator startOfLocaleIdentifier = lookahead;

				while (lookahead.IsInBounds && lookahead.Character != '}')
					{  lookahead.Next();  }

				if (lookahead.Character != '}')
					{
					substitution = null;
					return false;
					}

				string localeIdentifier = iterator.Tokenizer.TextBetween(startOfLocaleIdentifier, lookahead);
				lookahead.Next();

				string possibleSubstitution = Engine.Locale.SafeGet("NaturalDocs.Engine", localeIdentifier, null);

				if (possibleSubstitution == null)
					{
					substitution = null;
					return false;
					}
				else
					{
					substitution = '"' + possibleSubstitution.StringEscape() + '"';
					iterator = lookahead;
					return true;
					}
				}

			// Standard substitutions
			else
				{
				while (lookahead.IsInBounds &&
						 (lookahead.FundamentalType == FundamentalType.Text ||
						  lookahead.Character == '.' || 
						  lookahead.Character == '_'))
					{  lookahead.Next();  }

				string identifier = iterator.Tokenizer.TextBetween(startOfIdentifier, lookahead);
				string possibleSubstitution = substitutions[identifier.ToString()];

				if (possibleSubstitution == null)
					{
					substitution = null;
					return false;
					}
				else
					{
					substitution = possibleSubstitution;
					iterator = lookahead;
					return true;
					}
				}
			}


		// Group: Constants
		// __________________________________________________________________________

		protected const Collections.KeySettings KeySettingsForSubstitutions = KeySettings.Literal;


		// Group: Variables
		// __________________________________________________________________________

		protected Tokenizer source;
		protected StringBuilder output;
		protected StringToStringTable substitutions;


		// Group: Static Variables
		// __________________________________________________________________________

		protected static Regex.Comments.IncludeInOutput IncludeInOutputRegex;

		}
	}
