/*
 * Class: CodeClear.NaturalDocs.Engine.Output.ResourceProcessor
 * ____________________________________________________________________________
 *
 * A base class used for shared functionality when processing JS and CSS files.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2026 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using CodeClear.NaturalDocs.Engine.Collections;
using CodeClear.NaturalDocs.Engine.Comments;
using CodeClear.NaturalDocs.Engine.Tokenization;

namespace CodeClear.NaturalDocs.Engine.Output
	{
	public abstract partial class ResourceProcessor
		{

		// Group: Functions
		// __________________________________________________________________________

		public ResourceProcessor ()
			{
			quoteCharacters = null;
			lineCommentStrings = null;
			blockCommentStringPairs = null;
			}


		/* Function: Process
		 */
		abstract public string Process (string input, bool shrink = true);


		/* Function: GetPossibleDocumentationComments
		 *
		 * Goes through the file looking for comments that could possibly contain documentation and returns them as a list.  These
		 * comments are not guaranteed to have documentation in them, just to be acceptable candidates for them.  If there are no
		 * comments it will return an empty list.
		 */
		protected List<DocumentationComment> GetPossibleDocumentationComments (Tokenizer source)
			{
			List<DocumentationComment> possibleDocumentationComments = new List<DocumentationComment>();

			LineIterator lineIterator = source.FirstLine;

			while (lineIterator.IsInBounds)
				{
				bool foundComment = false;
				DocumentationComment possibleDocumentationComment = null;

				// Block comments
				if (blockCommentStringPairs != null)
					{
					for (int i = 0; foundComment == false && i < blockCommentStringPairs.Length; i += 2)
						{
						foundComment = TryToGetBlockComment(ref lineIterator, blockCommentStringPairs[i], blockCommentStringPairs[i+1],
																					 out possibleDocumentationComment);
						}
					}

				// Plain line comments
				if (foundComment == false && lineCommentStrings != null)
					{
					for (int i = 0; foundComment == false && i < lineCommentStrings.Length; i++)
						{
						foundComment = TryToGetLineComment(ref lineIterator, lineCommentStrings[i], out possibleDocumentationComment);
						}
					}

				// Nada.
				if (foundComment == false)
					{  lineIterator.Next();  }
				else
					{
					if (possibleDocumentationComment != null)
						{  possibleDocumentationComments.Add(possibleDocumentationComment);  }

					// lineIterator would have been moved already if foundComment is true
					}
				}

			return possibleDocumentationComments;
			}


		/* Function: TryToGetBlockComment
		 *
		 * If the iterator is on a line that starts with the opening symbol of a block comment, this function moves the iterator
		 * past the entire comment and returns true.  If the comment is a candidate for documentation it will also return it as
		 * a <DocumentationComment> and mark the symbols as <CommentParsingType.CommentSymbol>.  If the line does
		 * not start with an opening comment symbol it will return false and leave the iterator where it is.
		 *
		 * Not all the block comments it finds will be candidates for documentation, since some will have text after the closing
		 * symbol, so it's possible for this function to return true and have comment be null.
		 */
		protected bool TryToGetBlockComment (ref LineIterator lineIterator, string openingSymbol, string closingSymbol,
																 out DocumentationComment comment)
			{
			TokenIterator firstToken, endOfLine;
			lineIterator.GetBounds(LineBoundsMode.ExcludeWhitespace, out firstToken, out endOfLine);

			if (firstToken.MatchesAcrossTokens(openingSymbol) == false)
				{
				comment = null;
				return false;
				}

			// Advance past the opening symbol because it's possible for it to be the same as the closing one, such as with
			// Python's ''' and """ strings.
			firstToken.NextByCharacters(openingSymbol.Length);

			comment = new DocumentationComment();
			comment.Start = lineIterator;

			var tokenizer = lineIterator.Tokenizer;
			var lookahead = lineIterator;

			for (;;)
				{
				TokenIterator closingSymbolIterator;

				if (tokenizer.FindTokensBetween(closingSymbol, false, firstToken, endOfLine, out closingSymbolIterator) == true)
					{
					// Move past the end of the comment regardless of whether it's acceptable for documentation or not
					lookahead.Next();

					// Make sure nothing appears after the closing symbol on the line
					closingSymbolIterator.NextByCharacters(closingSymbol.Length);
					closingSymbolIterator.NextPastWhitespace();

					if (closingSymbolIterator.FundamentalType != FundamentalType.LineBreak &&
						closingSymbolIterator.FundamentalType != FundamentalType.Null)
						{  comment = null;  }
					else
						{  comment.End = lookahead;  }

					break;
					}

				lookahead.Next();

				// If we're not in bounds that means there was an unclosed comment at the end of the file.  Skip it but don't treat
				// it as a documentation candidate.
				if (!lookahead.IsInBounds)
					{
					comment = null;
					break;
					}

				lookahead.GetBounds(LineBoundsMode.ExcludeWhitespace, out firstToken, out endOfLine);
				}


			if (comment != null)
				{
				// Mark the symbols before returning

				firstToken = comment.Start.FirstToken(LineBoundsMode.ExcludeWhitespace);
				firstToken.SetCommentParsingTypeByCharacters(CommentParsingType.CommentSymbol, openingSymbol.Length);

				LineIterator lastLine = lookahead;
				lastLine.Previous();
				lastLine.GetBounds(LineBoundsMode.ExcludeWhitespace, out firstToken, out endOfLine);
				endOfLine.PreviousByCharacters(closingSymbol.Length);
				endOfLine.SetCommentParsingTypeByCharacters(CommentParsingType.CommentSymbol, closingSymbol.Length);
				}

			// If we made it this far that means we found a comment and can move the line iterator and return true.  Whether
			// that comment was suitable for documentation will be determined by the comment variable, but we are moving the
			// iterator and returning true either way.
			lineIterator = lookahead;
			return true;
			}


		/* Function: TryToGetLineComment
		 * If the iterator is on a line that starts with a line comment symbol, this function moves the iterator past the entire
		 * comment and returns true.  If the comment is a candidate for documentation it will also return it as a
		 * <DocumentationComment>.  If the line does not start with a line comment symbol it will return false and leave the
		 * iterator where it is.
		 */
		protected bool TryToGetLineComment (ref LineIterator lineIterator, string commentSymbol,
																out DocumentationComment comment)
			{
			TokenIterator firstToken = lineIterator.FirstToken(LineBoundsMode.ExcludeWhitespace);

			if (firstToken.MatchesAcrossTokens(commentSymbol) == false)
				{
				comment = null;
				return false;
				}

			comment = new DocumentationComment();
			comment.Start = lineIterator;
			lineIterator.Next();

			// Since we're definitely returning a comment we can mark the comment symbols as we go rather than waiting until
			// the end.
			firstToken.SetCommentParsingTypeByCharacters(CommentParsingType.CommentSymbol, commentSymbol.Length);

			while (lineIterator.IsInBounds)
				{
				firstToken = lineIterator.FirstToken(LineBoundsMode.ExcludeWhitespace);

				if (firstToken.MatchesAcrossTokens(commentSymbol) == false)
					{  break;  }

				firstToken.SetCommentParsingTypeByCharacters(CommentParsingType.CommentSymbol, commentSymbol.Length);
				lineIterator.Next();
				}

			comment.End = lineIterator;
			return true;
			}


		/* Function: FindIncludeInOutput
		 * Extracts and returns all comment content marked "include in output".  All comment symbols and extra indents
		 * will be removed.  Returns null if none of the comments have any.
		 */
		protected string FindIncludeInOutput (List<DocumentationComment> comments)
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
		protected string FindIncludeInOutput (DocumentationComment comment)
			{
			Comments.LineFinder.MarkTextBoxes(comment);

			LineIterator iterator = comment.Start;


			// Find the "include in output" header if it exists

			while (iterator < comment.End &&
					 iterator.Match(IsIncludeInOutputRegex(), LineBoundsMode.CommentContent).Success == false)
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

		/* Property: LineCommentStrings
		 * An array of strings representing line comment symbols.  Will be null if none are defined.
		 */
		public string[] LineCommentStrings
			{
			get
				{  return lineCommentStrings;  }
			set
				{
				if (value != null && value.Length != 0)
					{  lineCommentStrings = value;  }
				else
					{  lineCommentStrings = null;  }
				}
			}

		/* Property: BlockCommentStringPairs
		 * An array of string pairs representing start and stop block comment symbols.  Will be null if none are defined.
		 */
		public string[] BlockCommentStringPairs
			{
			get
				{  return blockCommentStringPairs;  }
			set
				{
				if (value != null && value.Length != 0)
					{
					if (value.Length % 2 == 1)
						{  throw new Engine.Exceptions.ArrayDidntHaveEvenLength("BlockCommentStringPairs");  }

					blockCommentStringPairs = value;
					}
				else
					{  blockCommentStringPairs = null;  }
				}
			}



		// Group: Variables
		// __________________________________________________________________________


		/* var: quoteCharacters
		 */
		protected char[] quoteCharacters;

		/* var: lineCommentStrings
		 * An array of strings that start line comments.
		 */
		protected string[] lineCommentStrings;

		/* var: blockCommentStringPairs
		 * An array of string pairs that start and end block comments.
		 */
		protected string[] blockCommentStringPairs;



		// Group: Static Variables
		// __________________________________________________________________________


		protected static char[] SubstitutionIdentifierPrefixes = { '@', '$' };



		// Group: Regular Expressions
		// __________________________________________________________________________


		/* Regex: IsIncludeInOutputRegex
		 * Will match if the entire string is "Include in output:" or one of its acceptable variants.
		 */
		[GeneratedRegex("""^(?:keep|include) in output:$""",
								  RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
		static private partial Regex IsIncludeInOutputRegex();

		}
	}
