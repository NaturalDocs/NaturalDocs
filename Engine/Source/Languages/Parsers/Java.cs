/*
 * Class: CodeClear.NaturalDocs.Engine.Languages.Parsers.Java
 * ____________________________________________________________________________
 *
 * Additional language support for Java.
 *
 */

// This file is part of Natural Docs, which is Copyright © 2003-2026 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using CodeClear.NaturalDocs.Engine.Prototypes;
using CodeClear.NaturalDocs.Engine.Tokenization;


namespace CodeClear.NaturalDocs.Engine.Languages.Parsers
{
	public class Java : Parser
		{

		// Group: Functions
		// __________________________________________________________________________


		/* Constructor: Java
		 */
		public Java (Engine.Instance engineInstance, Language language) : base (engineInstance, language)
			{
			}


		/* Function: TryToSkipMetadata
		 *
		 * Override to support detecting annotations as metadata.
		 *
		 * Supported Modes:
		 *
		 *		- <ParseMode.IterateOnly>
		 *		- <ParseMode.SyntaxHighlight>
		 *		- <ParseMode.ParsePrototype>
		 *			- Each annotation will create a new prototype section.
		 *		- Everything else is treated as <ParseMode.IterateOnly>.
		 */
		override protected bool TryToSkipMetadata (ref TokenIterator iterator, ParseMode mode = ParseMode.IterateOnly)
			{
			return TryToSkipAnnotation(ref iterator, mode);
			}


		/* Function: TryToSkipAnnotations
		 *
		 * Tries to move the iterator past one or more annotations, like "@Preliminary" or "@Copynight("String")".
		 *
		 * Supported Modes:
		 *
		 *		- <ParseMode.IterateOnly>
		 *		- <ParseMode.SyntaxHighlight>
		 *		- <ParseMode.ParsePrototype>
		 *			- Each annotation will create a new prototype section.
		 *		- Everything else is treated as <ParseMode.IterateOnly>.
		 */
		protected bool TryToSkipAnnotations (ref TokenIterator iterator, ParseMode mode = ParseMode.IterateOnly)
			{
			if (TryToSkipAnnotation(ref iterator, mode))
				{
				TokenIterator lookahead = iterator;

				for (;;)
					{
					TryToSkipWhitespace(ref lookahead, true, mode);

					if (TryToSkipAnnotation(ref lookahead, mode))
						{  iterator = lookahead;  }
					else
						{  break;  }
					}

				return true;
				}
			else
				{  return false;  }
			}


		/* Function: TryToSkipAnnotation
		 *
		 * Tries to move the iterator past a single annotation, like "@Preliminary" or "@Copynight("String")".
		 *
		 * Supported Modes:
		 *
		 *		- <ParseMode.IterateOnly>
		 *		- <ParseMode.SyntaxHighlight>
		 *		- <ParseMode.ParsePrototype>
		 *			- Each annotation will create a new prototype section.
		 *		- Everything else is treated as <ParseMode.IterateOnly>.
		 */
		protected bool TryToSkipAnnotation (ref TokenIterator iterator, ParseMode mode = ParseMode.IterateOnly)
			{
			if (iterator.Character != '@')
				{  return false;  }

			TokenIterator lookahead = iterator;
			lookahead.Next();

			// Whitespace is allowed between the @ and the identifier, though it's not recommended
			TryToSkipWhitespace(ref lookahead, true, mode);

			if (!TryToSkipIdentifier(ref lookahead, mode))
				{  return false;  }

			// That's all we need to be successful.  Try to find parameters though.
			TokenIterator annotationStart = iterator;
			TokenIterator annotationEnd = lookahead;

			if (mode == ParseMode.SyntaxHighlight)
				{  annotationStart.SetSyntaxHighlightingTypeBetween(annotationEnd, SyntaxHighlightingType.Metadata);  }

			TryToSkipWhitespace(ref lookahead, true, mode);

			if (TryToSkipAnnotationParameters(ref lookahead, mode))
				{  annotationEnd = lookahead;  }

			if (mode == ParseMode.ParsePrototype)
				{
				annotationStart.PrototypeParsingType = PrototypeParsingType.StartOfPrototypeSection;
				annotationEnd.PrototypeParsingType = PrototypeParsingType.EndOfPrototypeSection;
				}

			iterator = annotationEnd;
			return true;
			}


		/* Function: TryToSkipAnnotationParameters
		 *
		 * Tries to move the iterator past an annotation parameter section, such as "("String")" in "@Copynight("String")".
		 *
		 * Supported Modes:
		 *
		 *		- <ParseMode.IterateOnly>
		 *		- <ParseMode.SyntaxHighlight>
		 *		- <ParseMode.ParsePrototype>
		 *			- The contents will be marked with parameter tokens.
		 *		- Everything else is treated as <ParseMode.IterateOnly>.
		 */
		protected bool TryToSkipAnnotationParameters (ref TokenIterator iterator, ParseMode mode = ParseMode.IterateOnly)
			{
			if (iterator.Character != '(')
				{  return false;  }

			TokenIterator lookahead = iterator;

			if (!TryToSkipBlock(ref lookahead, false))
				{  return false;  }

			TokenIterator end = lookahead;

			if (mode == ParseMode.SyntaxHighlight)
				{
				iterator.SetSyntaxHighlightingTypeBetween(end, SyntaxHighlightingType.Metadata);
				}

			else if (mode == ParseMode.ParsePrototype)
				{
				TokenIterator openingParen = iterator;

				TokenIterator closingParen = lookahead;
				closingParen.Previous();

				openingParen.PrototypeParsingType = PrototypeParsingType.StartOfParams;
				closingParen.PrototypeParsingType = PrototypeParsingType.EndOfParams;

				lookahead = openingParen;
				lookahead.Next();

				TokenIterator startOfParam = lookahead;

				while (lookahead < closingParen)
					{
					if (lookahead.Character == ',')
						{
						MarkAnnotationParameter(startOfParam, lookahead, mode);

						lookahead.PrototypeParsingType = PrototypeParsingType.ParamSeparator;
						lookahead.Next();

						startOfParam = lookahead;
						}

					else if (TryToSkipComment(ref lookahead) ||
							   TryToSkipString(ref lookahead) ||
							   TryToSkipBlock(ref lookahead, true))
						{  }

					else
						{  lookahead.Next();  }
					}

				MarkAnnotationParameter(startOfParam, lookahead, mode);
				}

			iterator = end;
			return true;
			}


		/* Function: MarkAnnotationParameter
		 *
		 * Applies types to an annotation parameter, such as ""String"" in "@Copynight("String")" or "id = 12" in
		 * "@RequestForEnhancement(id = 12, engineer = "String")".
		 *
		 * Supported Modes:
		 *
		 *		- <ParseMode.ParsePrototype>
		 *			- The contents will be marked with parameter tokens.
		 *		- Everything else has no effect.
		 */
		protected void MarkAnnotationParameter (TokenIterator start, TokenIterator end, ParseMode mode = ParseMode.IterateOnly)
			{
			if (mode != ParseMode.ParsePrototype)
				{  return;  }

			start.NextPastWhitespace(end);
			end.PreviousPastWhitespace(PreviousPastWhitespaceMode.EndingBounds, start);

			if (start >= end)
				{  return;  }


			// Find and mark the equals sign, if there is one

			TokenIterator equals = start;

			while (equals < end)
				{
				if (equals.Character == '=')
					{
					equals.PrototypeParsingType = PrototypeParsingType.PropertyValueSeparator;
					break;
					}
				else if (TryToSkipComment(ref equals) ||
							TryToSkipString(ref equals) ||
							TryToSkipBlock(ref equals, true))
					{  }
				else
					{  equals.Next();  }
				}


			// The equals sign will be at or past the end if it doesn't exist.

			if (equals >= end)
				{
				start.SetPrototypeParsingTypeBetween(end, PrototypeParsingType.PropertyValue);
				}
			else
				{
				TokenIterator iterator = equals;
				iterator.PreviousPastWhitespace(PreviousPastWhitespaceMode.EndingBounds, start);

				if (start < iterator)
					{  start.SetPrototypeParsingTypeBetween(iterator, PrototypeParsingType.Name);  }

				iterator = equals;
				iterator.Next();
				iterator.NextPastWhitespace(end);

				if (iterator < end)
					{  iterator.SetPrototypeParsingTypeBetween(end, PrototypeParsingType.PropertyValue);  }
				}
			}


		/* Function: TryToSkipIdentifier
		 *
		 * Tries to move the iterator past a qualified identifier, such as "X.Y.Z".  Use <TryToSkipUnqualifiedIdentifier()> if you only want
		 * to skip a single segment.
		 *
		 * Supported Modes:
		 *
		 *		- <ParseMode.IterateOnly>
		 *		- Everything else is treated as <ParseMode.IterateOnly>.
		 */
		protected bool TryToSkipIdentifier (ref TokenIterator iterator, ParseMode mode = ParseMode.IterateOnly)
			{
			TokenIterator lookahead = iterator;

			for (;;)
				{
				if (TryToSkipUnqualifiedIdentifier(ref lookahead) == false)
					{  return false;  }

				if (lookahead.Character == '.')
					{  lookahead.Next();  }
				else
					{  break;  }
				}

			iterator = lookahead;
			return true;
			}


		/* Function: TryToSkipUnqualifiedIdentifier
		 *
		 * Tries to move the iterator past a single unqualified identifier, which means only "X" in "X.Y.Z".
		 *
		 * Supported Modes:
		 *
		 *		- <ParseMode.IterateOnly>
		 *		- Everything else is treated as <ParseMode.IterateOnly>.
		 */
		protected bool TryToSkipUnqualifiedIdentifier (ref TokenIterator iterator, ParseMode mode = ParseMode.IterateOnly)
			{
			TokenIterator lookahead = iterator;

			if (lookahead.FundamentalType == FundamentalType.Text)
				{
				if (lookahead.Character >= '0' && lookahead.Character <= '9')
					{  return false;  }
				}
			else if (lookahead.FundamentalType == FundamentalType.Symbol)
				{
				if (lookahead.Character != '_')
					{  return false;  }
				}
			else
				{  return false;  }

			iterator = lookahead;

			do
				{  iterator.Next();  }
			while (iterator.FundamentalType == FundamentalType.Text || iterator.Character == '_');

			return true;
			}

		}
	}
