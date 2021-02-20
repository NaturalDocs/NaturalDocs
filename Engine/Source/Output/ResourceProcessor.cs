/*
 * Class: CodeClear.NaturalDocs.Engine.Output.ResourceProcessor
 * ____________________________________________________________________________
 *
 * A base class used for shared functionality when processing JS and CSS files.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2021 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;
using System.Text;
using CodeClear.NaturalDocs.Engine.Collections;
using CodeClear.NaturalDocs.Engine.Comments;
using CodeClear.NaturalDocs.Engine.Tokenization;

namespace CodeClear.NaturalDocs.Engine.Output
	{
	public abstract class ResourceProcessor : Languages.Language
		{

		// Group: Functions
		// __________________________________________________________________________

		static ResourceProcessor ()
			{
			IncludeInOutputRegex = new Regex.Comments.IncludeInOutput();
			}

		public ResourceProcessor (Engine.Instance engineInstance, string name) : base (engineInstance.Languages, name)
			{
			this.quoteCharacters = null;
			}


		/* Function: Process
		 */
		abstract public string Process (string input, bool shrink = true);


		/* Function: FindIncludeInOutput
		 * Extracts and returns all comment content marked "include in output".  All comment symbols and extra indents
		 * will be removed.  Returns null if none of the comments have any.
		 */
		protected string FindIncludeInOutput (List<PossibleDocumentationComment> comments)
			{
			StringBuilder output = null;

			foreach (var comment in comments)
				{
				string includeInOutput = FindIncludeInOutput(comment);

				if (includeInOutput != null)
					{
					if (output == null)
						{  output = new StringBuilder();  }
					else
						{  output.AppendLine();  }

					output.Append(includeInOutput);
					}
				}

			if (output == null)
				{  return null;  }
			else
				{  return output.ToString();  }
			}


		/* Function: FindIncludeInOutput
		 * Extracts and returns any comment content marked "include in output".  All comment symbols and extra indents
		 * will be removed.  Returns null if the comment doesn't have any.
		 */
		protected string FindIncludeInOutput (PossibleDocumentationComment comment)
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


		/* Function: FindSubstitutions
		 * Searches the source for substitution definitions and returns them as a <StringToStringTable>.
		 */
		protected StringToStringTable FindSubstitutions (Tokenizer source)
			{
			StringToStringTable substitutions = new StringToStringTable();
			TokenIterator iterator = source.FirstToken;

			string identifier, value, declaration;

			while (iterator.IsInBounds)
				{
				if (TryToSkipSubstitutionDefinition(ref iterator, out identifier, out value, out declaration))
					{  substitutions.Add(identifier, value);  }
				else
					{  GenericSkip(ref iterator);  }
				}

			return substitutions;
			}


		/* Function: ApplySubstitutions
		 * Finds all substitutions in the source that match those in the table and replaces them with their values.  Will also comment
		 * out any substitution definitions found.
		 */
		protected Tokenizer ApplySubstitutions (Tokenizer source, StringToStringTable substitutions, bool applyNestedSubstitutions = true)
			{
			TokenIterator iterator = source.FirstToken;


			// Find the first valid substitution identifier or definition.  If there aren't any we don't want to \do unnecessary memory 
			// allocation and processing.

			bool foundSubstitution = false;
			string identifier = null;
			string localeIdentifier, value, declaration;

			while (iterator.IsInBounds)
				{
				if (TryToSkipSubstitutionIdentifier(ref iterator, out identifier) ||
					TryToSkipLocaleSubstitutionIdentifier(ref iterator, out identifier, out localeIdentifier))
					{
					foundSubstitution = true;
					break;
					}
				// else if (TryToSkipSubstitutionDefinition())
					// {
					// Unnecessary because definitions will start with identifiers so it will get picked up by that
					// }
				else
					{  GenericSkip(ref iterator);  }
				}

			if (!foundSubstitution)
				{  return source;  }


			// Now that we know we have one, we can back up the iterator and build new output

			iterator.PreviousByCharacters(identifier.Length);

			StringBuilder output = new StringBuilder(source.RawText.Length);
			output.Append(source.RawText, 0, iterator.RawTextIndex);

			while (iterator.IsInBounds)
				{
				TokenIterator previousIterator = iterator;

				if (TryToSkipSubstitutionDefinition(ref iterator, out identifier, out value, out declaration) )
					{
					if (this.blockCommentStringPairs != null)
						{
						output.Append(this.blockCommentStringPairs[0] + ' ' + declaration + ' ' + this.blockCommentStringPairs[1]);
						}
					}
				else if (TryToSkipSubstitutionIdentifier(ref iterator, out identifier))
					{
					string substitution = substitutions[identifier];

					if (substitution == null)
						{  output.Append(identifier);  }
					else
						{
						if (applyNestedSubstitutions)
							{  substitution = ApplyNestedSubstitutions(substitution, substitutions);  }

						output.Append(substitution);
						}
					}
				else if (TryToSkipLocaleSubstitutionIdentifier(ref iterator, out identifier, out localeIdentifier))
					{
					string substitution = Engine.Locale.SafeGet("NaturalDocs.Engine", localeIdentifier, null);

					if (substitution == null)
						{  output.Append(identifier);  }
					else
						{  output.Append('"' + substitution.StringEscape() + '"');  }
					}
				else
					{
					GenericSkip(ref iterator);
					source.AppendTextBetweenTo(previousIterator, iterator, output);
					}
				}

			return new Tokenizer( output.ToString() );
			}


		protected string ApplyNestedSubstitutions (string substitution, StringToStringTable substitutions)
			{
			if (substitution.IndexOfAny(SubstitutionIdentifierPrefixes ) == -1)
				{  return substitution;  }

			int rounds = 1;
			string currentResult = substitution;
			Tokenizer tokenizer = new Tokenizer(currentResult);

			while (rounds < 50)  // safety for infinite recursion
				{
				tokenizer = ApplySubstitutions(tokenizer, substitutions, false);

				if (tokenizer.RawText == currentResult)
					{  break;  }
				
				currentResult = tokenizer.RawText;
				rounds++;
				}

			return currentResult;
			}



		// Group: Parsing Functions
		// __________________________________________________________________________


		/* Function: GenericSkip
		 * 
		 * Moves the iterator ahead one code element, which could be a single token, whitespace, an entire comment, or an entire
		 * string.  The important part is that this skips comments and strings all in one step so that anything appearing inside them
		 * will not be misinterpreted as code.
		 * 
		 * It is virtual so you can extend it to handle things like regular expressions.  The default implementation handles strings
		 * and comments based on LineCommentStrings, BlockCommentStringPairs, and <QuoteCharacters>.
		 */
		protected virtual void GenericSkip (ref TokenIterator iterator)
			{
			if (!TryToSkipComment(ref iterator) &&
				!TryToSkipString(ref iterator))
				{  
				iterator.Next();  
				}
			}


		/* Function: TryToSkipWhitespace
		 * If the iterator is on whitespace or a comment, skips over it and returns true.  Otherwise leaves the iterator alone and
		 * returns false.
		 */
		protected bool TryToSkipWhitespace (ref TokenIterator iterator)
			{
			bool result = false;

			while (iterator.IsInBounds)
				{
				if (iterator.FundamentalType == FundamentalType.Whitespace ||
					 iterator.FundamentalType == FundamentalType.LineBreak)
					{
					result = true;
					iterator.Next();
					}
				else if (TryToSkipComment(ref iterator))
					{  result = true;  }
				else
					{  break;  }
				}

			return result;
			}


		/* Function: TryToSkipComment
		 * If the iterator is on the opening symbol of any kind of comment, skips over it and returns true.  Otherwise leaves the
		 * iterator alone and returns false.
		 */
		protected bool TryToSkipComment (ref TokenIterator iterator)
			{
			if (TryToSkipLineComment(ref iterator) ||
				TryToSkipBlockComment(ref iterator))
				{  return true;  }
			else
				{  return false;  }
			}


		/* Function: TryToSkipLineComment
		 * If the iterator is on the opening symbol of a line comment, skips over it and returns true.  Otherwise leaves the iterator
		 * alone and returns false.
		 */
		protected bool TryToSkipLineComment (ref TokenIterator iterator)
			{
			if (this.lineCommentStrings == null)
				{  return false;  }

			foreach (string lineCommentString in this.lineCommentStrings)
				{
				if (TryToSkipLineComment(ref iterator, lineCommentString))
					{  return true;  }
				}

			return false;
			}


		/* Function: TryToSkipLineComment
		 * If the iterator is on the opening symbol of a line comment, skips over it and returns true.  Otherwise leaves the iterator
		 * alone and returns false.
		 */
		protected bool TryToSkipLineComment (ref TokenIterator iterator, string symbol)
			{
			if (iterator.MatchesAcrossTokens(symbol))
				{
				iterator.NextByCharacters(symbol.Length);

				while (iterator.IsInBounds && iterator.FundamentalType != FundamentalType.LineBreak)
					{  iterator.Next();  }

				if (iterator.FundamentalType == FundamentalType.LineBreak)
					{  iterator.Next();  }

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
			if (this.blockCommentStringPairs == null)
				{  return false;  }

			for (int i = 0; i < this.blockCommentStringPairs.Length; i += 2)
				{
				if (TryToSkipBlockComment(ref iterator, this.blockCommentStringPairs[i], this.blockCommentStringPairs[i + 1]))
					{  return true;  }
				}

			return false;
			}


		/* Function: TryToSkipBlockComment
		 * If the iterator is on the opening symbol of a block comment, skips over it and returns true.  Otherwise leaves the iterator
		 * alone and returns false.
		 */
		protected bool TryToSkipBlockComment (ref TokenIterator iterator, string openingSymbol, string closingSymbol)
			{
			if (iterator.MatchesAcrossTokens(openingSymbol))
				{
				iterator.NextByCharacters(openingSymbol.Length);

				while (iterator.IsInBounds && !iterator.MatchesAcrossTokens(closingSymbol))
					{  iterator.Next();  }

				if (iterator.IsInBounds)
					{  iterator.NextByCharacters(closingSymbol.Length);  }

				return true;
				}
			else
				{  return false;  }
			}


		/* Function: TryToSkipString
		 * If the iterator is on the opening quote character of a string, skips over it and returns true.  Otherwise leaves the iterator alone
		 * and returns false.
		 */
		protected bool TryToSkipString (ref TokenIterator iterator)
			{
			if (this.quoteCharacters == null)
				{  return false;  }

			foreach (char quoteCharacter in this.quoteCharacters)
				{
				if (TryToSkipString(ref iterator, quoteCharacter))
					{  return true;  }
				}

			return false;
			}


		/* Function: TryToSkipString
		 * If the iterator is on the opening quote character of a string, skips over it and returns true.  Otherwise leaves the iterator alone
		 * and returns false.
		 */
		protected bool TryToSkipString (ref TokenIterator iterator, char quoteChar)
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


		/* Function: TryToSkipSubstitutionIdentifier
		 * If the iterator is on a valid substitution identifier, advances it past it, returns it, and returns true.
		 * Otherwise the iterator will be left alone and it will return false.
		 */
		protected bool TryToSkipSubstitutionIdentifier (ref TokenIterator iterator, out string identifier)
			{
			if (iterator.Character != '$' && iterator.Character != '@')
				{
				identifier = null;
				return false;
				}

			TokenIterator lookahead = iterator;
			lookahead.Next();

			// Locale substitutions
			if (lookahead.MatchesAcrossTokens("Locale{"))
				{  
				identifier = null;
				return false;
				}

			while (lookahead.IsInBounds &&
					(lookahead.FundamentalType == FundamentalType.Text ||
					lookahead.Character == '.' || 
					lookahead.Character == '_'))
				{  lookahead.Next();  }

			identifier = iterator.TextBetween(lookahead);
			iterator = lookahead;
			return true;
			}


		/* Function: TryToSkipLocaleSubstitutionIdentifier
		 * If the iterator is on a valid locale substitution identifier, advances it past it, returns it, and returns true.
		 * Otherwise the iterator will be left alone and it will return false.
		 */
		protected bool TryToSkipLocaleSubstitutionIdentifier (ref TokenIterator iterator, out string identifier, out string localeIdentifier)
			{
			if (iterator.Character != '$' && iterator.Character != '@')
				{
				identifier = null;
				localeIdentifier = null;
				return false;
				}

			TokenIterator lookahead = iterator;
			lookahead.Next();

			if (lookahead.MatchesAcrossTokens("Locale{") == false)
				{
				identifier = null;
				localeIdentifier = null;
				return false;
				}

			lookahead.NextByCharacters(7);
			TokenIterator startOfLocaleIdentifier = lookahead;

			while (lookahead.IsInBounds && lookahead.Character != '}')
				{  lookahead.Next();  }

			if (lookahead.Character != '}')
				{
				identifier = null;
				localeIdentifier = null;
				return false;
				}

			localeIdentifier = startOfLocaleIdentifier.TextBetween(lookahead);
			lookahead.Next();

			identifier = iterator.TextBetween(lookahead);

			iterator = lookahead;
			return true;
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

			TokenIterator lookahead = iterator;

			if (TryToSkipSubstitutionIdentifier(ref lookahead, out identifier) == false)
				{  return false;  }
			
			lookahead.NextPastWhitespace();

			if (lookahead.Character != ':' && lookahead.Character != '=')
				{
				identifier = null;
				return false;  
				}

			lookahead.Next();
			lookahead.NextPastWhitespace();
			
			TokenIterator startOfValue = lookahead;

			while (lookahead.IsInBounds && lookahead.Character != ';' && lookahead.FundamentalType != FundamentalType.LineBreak)
				{
				GenericSkip(ref lookahead);
				}

			value = startOfValue.TextBetween(lookahead);

			if (lookahead.Character == ';')
				{  lookahead.Next();  }

			declaration = iterator.TextBetween(lookahead);

			iterator = lookahead;
			return true;
			}



		// Group: Properties
		// __________________________________________________________________________

		public char[] QuoteCharacters
			{
			get
				{  return quoteCharacters;  }
			set
				{  quoteCharacters = value;  }
			}


		// Group: Variables
		// __________________________________________________________________________

		protected char[] quoteCharacters;


		// Group: Static Variables
		// __________________________________________________________________________

		protected static Regex.Comments.IncludeInOutput IncludeInOutputRegex;
		protected static char[] SubstitutionIdentifierPrefixes = { '@', '$' };

		}
	}
