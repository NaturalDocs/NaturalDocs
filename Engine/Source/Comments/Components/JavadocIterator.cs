/*
 * Struct: CodeClear.NaturalDocs.Engine.Comments.Components.JavadocIterator
 * ____________________________________________________________________________
 *
 * A struct to handle walking through Javadoc/HTML-formatted content.  It moves by element, treating things
 * like tags and stretches of unformatted text as one step.  The iterator assumes you are going to walk through
 * it in a linear fashion rather than navigating a parsed HTML tree.
 *
 */

// This file is part of Natural Docs, which is Copyright © 2003-2026 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using CodeClear.NaturalDocs.Engine.Tokenization;


namespace CodeClear.NaturalDocs.Engine.Comments.Components
	{
	public partial struct JavadocIterator
		{

		// Group: Functions
		// __________________________________________________________________________


		/* Constructor: JavadocIterator
		 * Creates a new Javadoc iterator that will navigate between the two <TokenIterators>.
		 */
		public JavadocIterator (TokenIterator start, TokenIterator end)
			{
			tokenIterator = start;
			endingRawTextIndex = end.RawTextIndex;

			type = JavadocElementType.OutOfBounds;
			length = 0;
			tagType = null;

			DetermineElement();
			}


		/* Function: Next
		 * Moves the iterator forward by the passed number of elements, returning whether it's still in bounds.
		 */
		public bool Next (int count = 1)
			{
			while (count > 0)
				{
				if (type == JavadocElementType.OutOfBounds)
					{  return false;  }

				tokenIterator.NextByCharacters(length);
				DetermineElement();

				count--;
				}

			return (type != JavadocElementType.OutOfBounds);
			}


		/* Function: IsOn
		 * Returns whether the iterator is on the passed <JavadocElementType>
		 */
		public bool IsOn (JavadocElementType elementType)
			{
			return (type == elementType);
			}

		/* Function: IsOn
		 * Returns whether the iterator is on the passed <JavadocElementType> and tag type.  The tag type is only relevant
		 * for <JavadocElementType.HTMLTag> or <JavadocElementType.JavadocTag>.
		 */
		public bool IsOn (JavadocElementType elementType, string tagType)
			{
			#if DEBUG
			if (elementType != JavadocElementType.HTMLTag && elementType != JavadocElementType.JavadocTag)
				{  throw new Exception ("Can't call IsOn() with a tag type unless the element type is a HTML or Javadoc tag.");  }
			#endif

			return (type == elementType && this.tagType == tagType);
			}


		/* Function: IsOn
		 * Returns whether the iterator is on the passed <JavadocElementType>, tag type, and <TagForm>.  This function must be
		 * used with <JavadocElementType.HTMLTag> since that's the only type where it's relevant.  <JavadocElementType> is
		 * passed anyway for consistency with other IsOn() functions.
		 */
		public bool IsOn (JavadocElementType elementType, string tagType, TagForm tagForm)
			{
			#if DEBUG
			if (elementType != JavadocElementType.HTMLTag)
				{  throw new Exception ("Can't call IsOn() with a tag form unless the element type is a HTML tag.");  }
			#endif

			return IsOnHTMLTag(tagType, tagForm);
			}


		/* Function: IsOn
		 * Returns whether the iterator is on the passed <JavadocElementType>, tag type, and <JavadocTagForm>.  This function
		 * must be used with <JavadocElementType.JavadocTag> since that's the only type where it's relevant.  <JavadocElementType>
		 * is passed anyway for consistency with other IsOn() functions.
		 */
		public bool IsOn (JavadocElementType elementType, string tagType, JavadocTagForm tagForm)
			{
			#if DEBUG
			if (elementType != JavadocElementType.HTMLTag)
				{  throw new Exception ("Can't call IsOn() with a tag form unless the element type is a HTML tag.");  }
			#endif

			return IsOnJavadocTag(tagType, tagForm);
			}


		/* Function: IsOnHTMLTag
		 * Returns whether the iterator is on the passed HTML tag type.
		 */
		public bool IsOnHTMLTag (string tagType)
			{
			return (type == JavadocElementType.HTMLTag && this.tagType == tagType);
			}


		/* Function: IsOnHTMLTag
		 * Returns whether the iterator is on the passed HTML tag type and <TagForm>.
		 */
		public bool IsOnHTMLTag (string tagType, TagForm tagForm)
			{
			return (type == JavadocElementType.HTMLTag && this.tagType == tagType && HTMLTagForm == tagForm);
			}


		/* Function: IsOnJavadocTag
		 * Returns whether the iterator is on the passed Javadoc tag type.
		 */
		public bool IsOnJavadocTag (string tagType)
			{
			return (type == JavadocElementType.JavadocTag && this.tagType == tagType);
			}


		/* Function: IsOnJavadocTag
		 * Returns whether the iterator is on the passed Javadoc tag type and <JavadocTagForm>.
		 */
		public bool IsOnJavadocTag (string tagType, JavadocTagForm tagForm)
			{
			return (type == JavadocElementType.JavadocTag && this.tagType == tagType && JavadocTagForm == tagForm);
			}


		/* Function: GetAllHTMLTagProperties
		 * If <Type> is <JavadocElementType.HTMLTag>, this generates a dictionary of all the properties in the tag, if any.
		 */
		public Dictionary<string, string> GetAllHTMLTagProperties ()
			{
			if (type != JavadocElementType.HTMLTag)
			    {  throw new InvalidOperationException();  }

			Dictionary<string, string> properties = new Dictionary<string,string>();

			Match match = IsHTMLTagRegex().Match(RawText, RawTextIndex, length);
			CaptureCollection captures = match.Groups[2].Captures;

			foreach (Capture capture in captures)
			    {
			    Match propertyMatch = IsHTMLTagPropertyRegex().Match(RawText, capture.Index, capture.Length);
			    properties.Add( propertyMatch.Groups[1].ToString(),
									  DecodeHTMLPropertyValue(propertyMatch.Groups[2].Index, propertyMatch.Groups[2].Length) );
			    }

			return properties;
			}



		// Group: Private Functions
		// __________________________________________________________________________


		/* Function: DetermineElement
		 * Determines which <JavadocElementType> the iterator is currently on, setting <type> and <length>.
		 */
		private void DetermineElement ()
			{
			bool found = false;

			if (tokenIterator == null || !IsInBounds)
				{
				type = JavadocElementType.OutOfBounds;
				length = 0;
				found = true;
				}
			// If we're on a new line and either whitespace or a comment token...
			else if ( ( RawTextIndex == 0 ||
						  Tokenizer.FundamentalTypeOf(RawText[RawTextIndex - 1]) == FundamentalType.LineBreak )
						&&
						( tokenIterator.FundamentalType == FundamentalType.Whitespace ||
						  tokenIterator.CommentParsingType == CommentParsingType.CommentSymbol ||
						  tokenIterator.CommentParsingType == CommentParsingType.CommentDecoration ) )
				{
				type = JavadocElementType.Indent;
				length = 0;

				TokenIterator lookahead = tokenIterator;

				do
					{
					length += lookahead.RawTextLength;
					lookahead.Next();
					}
				while (lookahead.RawTextIndex < endingRawTextIndex &&
						  (lookahead.FundamentalType == FundamentalType.Whitespace ||
						   lookahead.CommentParsingType == CommentParsingType.CommentSymbol ||
						   lookahead.CommentParsingType == CommentParsingType.CommentDecoration) );

				found = true;
				}
			else if (tokenIterator.FundamentalType == FundamentalType.LineBreak)
				{
				type = JavadocElementType.LineBreak;
				length = tokenIterator.RawTextLength;
				found = true;
				}
			else if (tokenIterator.MatchesAcrossTokens("<!--"))
				{
				type = JavadocElementType.HTMLComment;

				int endingCommentIndex = RawText.IndexOf("-->", RawTextIndex + 4, endingRawTextIndex - (RawTextIndex + 4));

				if (endingCommentIndex == -1)
					{  length = endingRawTextIndex - RawTextIndex;  }
				else
					{  length = (endingCommentIndex + 3) - RawTextIndex;  }

				found = true;
				}
			else if (tokenIterator.Character == '<')
				{
				int nextBracketIndex = RawText.IndexOfAny(AngleBrackets, RawTextIndex + 1);

				if (nextBracketIndex != -1 && nextBracketIndex < endingRawTextIndex && RawText[nextBracketIndex] == '>')
					{
					type = JavadocElementType.HTMLTag;
					length = nextBracketIndex + 1 - RawTextIndex;

					Match tagMatch = IsHTMLTagRegex().Match(RawText, RawTextIndex, length);

					if (tagMatch.Success)
						{
						found = true;
						tagType = tagMatch.Groups[1].ToString().ToLower(CultureInfo.InvariantCulture);
						}
					}
				}
			else if (tokenIterator.MatchesAcrossTokens("{@"))
				{
				int closingBrace = RawText.IndexOf('}', RawTextIndex + 2);

				TokenIterator lookahead = tokenIterator;
				lookahead.NextByCharacters(2);

				if (closingBrace != -1 && closingBrace < endingRawTextIndex &&
					lookahead.FundamentalType == FundamentalType.Text &&
					Javadoc.Parser.InlineTags.Contains(lookahead.String))
					{
					found = true;
					type = JavadocElementType.JavadocTag;
					tagType = lookahead.String;
					length = (closingBrace + 1) - RawTextIndex;
					}
				}
			else if (tokenIterator.Character == '@')
				{
				TokenIterator lookahead = tokenIterator;
				lookahead.Next();

				if (lookahead.FundamentalType == FundamentalType.Text &&
					Javadoc.Parser.BlockTags.Contains(lookahead.String))
					{
					found = true;
					type = JavadocElementType.JavadocTag;
					tagType = lookahead.String;
					length = tagType.Length + 1;
					}
				}
			else if (tokenIterator.Character == '&')
				{
				int semicolonIndex = RawText.IndexOf(';', RawTextIndex + 1);

				if (semicolonIndex != -1 && semicolonIndex < endingRawTextIndex &&
					HTMLEntityChars.IsEntityChar(RawText, RawTextIndex, semicolonIndex + 1 - RawTextIndex))
					{
					type = JavadocElementType.EntityChar;
					length = semicolonIndex + 1 - RawTextIndex;
					found = true;
					}
				}

			if (!found)
				{
				type = JavadocElementType.Text;

				int nextElementIndex = RawText.IndexOfAny(StartOfElement, RawTextIndex + 1);

				if (nextElementIndex == -1)
					{  length = endingRawTextIndex - RawTextIndex;  }
				else
					{  length = nextElementIndex - RawTextIndex;  }
				}
			}


		/* Function: DecodeHTMLPropertyValue
		 * Extracts the property value from the passed HTML tag value, stripping off the surrounding quotes and any escaped quotes
		 * within.
		 */
		private string DecodeHTMLPropertyValue (int valueIndex, int valueLength)
			{
			if (RawText[valueIndex] == '"' || RawText[valueIndex] == '\'')
				{
				string value = RawText.Substring(valueIndex + 1, valueLength - 2);

				value = value.Replace("\\\"", "\"");
				value = value.Replace("\\'", "'");
				value = HTMLEntityChars.DecodeAll(value);

				return value;
				}
			else
				{  return RawText.Substring(valueIndex, valueLength);  }
			}



		// Group: Properties
		// __________________________________________________________________________


		/* Property: Type
		 * The type of content the iterator is currently on.
		 */
		public JavadocElementType Type
			{
			get
				{  return type;  }
			}


		/* Property: Length
		 * The length of the element the iterator is currently on.
		 */
		public int Length
			{
			get
				{  return length;  }
			}


		/* Property: String
		 * Returns the element as a string.
		 */
		public string String
			{
			get
				{
				if (length == 0)
					{  return "";  }
				else if (type == JavadocElementType.Indent)
					{
					// Don't want comment symbols or tabs, so convert to spaces
					return new string(' ', Indent);
					}
				else
					{  return RawText.Substring(RawTextIndex, length);  }
				}
			}


		/* Property: TagType
		 * If <Type> is <JavadocElementType.HTMLTag> or <JavadocElementType.JavadocTag>, this is the tag type as a string.  For example,
		 * "<br />" would return "br" and "@param" will return "param".  HTML tag types will always be in lowercase.
		 */
		public string TagType
			{
			get
				{
				if (type != JavadocElementType.HTMLTag &&
					type != JavadocElementType.JavadocTag)
					{  throw new InvalidOperationException();  }

				return tagType;
				}
			}


		/* Property: JavadocTagForm
		 * If <Type> is <JavadocElementType.JavadocTag> this is the <JavadocTagForm> it takes.
		 */
		public JavadocTagForm JavadocTagForm
			{
			get
				{
				if (type != JavadocElementType.JavadocTag)
					{  throw new InvalidOperationException();  }

				if (RawText[RawTextIndex] == '@')
					{  return JavadocTagForm.Block;  }
				else
					{  return JavadocTagForm.Inline;  }
				}
			}


		/* Property: JavadocTagValue
		 * If <Type> is <JavadocElementType.JavadocTag> and <JavadocTagForm> is <JavadocTagForm.Inline>, this
		 * is the content of the tag.  If there is none it will return null.
		 */
		public string JavadocTagValue
			{
			get
				{
				if (type != JavadocElementType.JavadocTag)
					{  throw new InvalidOperationException();  }
				if (JavadocTagForm != Components.JavadocTagForm.Inline)
					{  return null;  }

				TokenIterator start = tokenIterator;
				start.NextByCharacters(2);  // {@
				start.Next();  // Tag name
				start.NextPastWhitespace();

				TokenIterator end = tokenIterator;
				end.NextByCharacters(length);
				end.Previous();  // }
				end.PreviousPastWhitespace(PreviousPastWhitespaceMode.EndingBounds, start);

				if (end > start)
					{  return start.TextBetween(end);  }
				else
					{  return null;  }
				}
			}


		/* Property: HTMLTagForm
		 * If <Type> is <JavadocElementType.HTMLTag> this is the <TagForm> it takes.  Note that HTML doesn't always follow XML-like rules.
		 * This function will say "<br>" is an opening tag and "<br />" is a standalone tag, but in HTML they are actually both standalone tags.
		 */
		public TagForm HTMLTagForm
			{
			get
				{
				if (type != JavadocElementType.HTMLTag)
					{  throw new InvalidOperationException();  }

				if (RawText[RawTextIndex + 1] == '/')
					{  return TagForm.Closing;  }
				else if (RawText[RawTextIndex + length - 2] == '/')
					{  return TagForm.Standalone;  }
				else
					{  return TagForm.Opening;  }
				}
			}


		/* Function: HTMLTagProperty
		 * If <Type> is <JavadocElementType.HTMLTag>, return the value of the passed property, or null if it doesn't exist.  The property
		 * name is case-insensitive.
		 */
		public string HTMLTagProperty (string name)
			{
			if (type != JavadocElementType.HTMLTag)
			    {  throw new InvalidOperationException();  }

			Match match = IsHTMLTagRegex().Match(RawText, RawTextIndex, length);
			CaptureCollection captures = match.Groups[2].Captures;

			foreach (Capture capture in captures)
			    {
			    Match propertyMatch = IsHTMLTagPropertyRegex().Match(RawText, capture.Index, capture.Length);

			    if (name.Length == propertyMatch.Groups[1].Length &&
			        string.Compare(name, 0, RawText, propertyMatch.Groups[1].Index, name.Length, true) == 0)
			        {
			        return DecodeHTMLPropertyValue(propertyMatch.Groups[2].Index, propertyMatch.Groups[2].Length);
			        }
			    }

			return null;
			}


		/* Property: EntityValue
		 * If <Type> is <JavadocElementType.EntityChar>, this is the decoded character.
		 */
		public char EntityValue
			{
			get
				{
				if (type != JavadocElementType.EntityChar)
					{  throw new InvalidOperationException();  }

				return HTMLEntityChars.DecodeSingle(RawText, RawTextIndex, length);
				}
			}


		/* Property: Indent
		 * If <Type> is <JavadocElementType.Indent>, this is the indent length with tabs expanded.
		 */
		public int Indent
			{
			get
				{
				if (type != JavadocElementType.Indent)
					{  throw new InvalidOperationException();  }

				int indent = 0;
				string rawText = RawText;
				int rawTextIndex = RawTextIndex;

				for (int i = 0; i < length; i++)
					{
					if (rawText[rawTextIndex + i] == '\t')
						{
						indent += tokenIterator.Tokenizer.TabWidth;
						indent -= (indent % tokenIterator.Tokenizer.TabWidth);
						}
					else
						{  indent++;  }
					}

				return indent;
				}
			}


		/* Property: IsInBounds
		 */
		public bool IsInBounds
			{
			get
				{  return (tokenIterator.RawTextIndex < endingRawTextIndex);  }
			}


		/* Property: RawTextIndex
		 * The iterators position as an index into the string.
		 */
		public int RawTextIndex
			{
			get
				{  return tokenIterator.RawTextIndex;  }
			}


		/* Property: RawText
		 * The raw text <tokenIterator> is walking over.
		 */
		private string RawText
			{
			get
				{  return tokenIterator.Tokenizer.RawText;  }
			}



		// Group: Variables
		// __________________________________________________________________________


		/* var: tokenIterator
		 * The current position of the iterator.
		 */
		private TokenIterator tokenIterator;

		/* var: endingRawTextIndex
		 * The end of the text we are iterating over.
		 */
		private int endingRawTextIndex;

		/* var: type
		 * The <JavadocElementType> of the current element.
		 */
		private JavadocElementType type;

		/* var: tagType
		 * The type of the current tag if the iterator is on a tag.
		 */
		private string tagType;

		/* var: length
		 * The length of the current element.
		 */
		private int length;



		// Group: Static Variables
		// __________________________________________________________________________


		static private char[] AngleBrackets = { '<', '>' };

		// DEPENDENCY: Assumes this encompasses all the line break chars recognized by Tokenizer
		static private char[] StartOfElement = { '<', '&', '{', '@', '\n', '\r' };



		// Group: Regular Expressions
		// __________________________________________________________________________


		/* Regex: IsHTMLTagRegex
		 *
		 * Will match if the entire string is a HTML tag.
		 *
		 * Capture Groups:
		 *
		 *		1 - The tag name.  It will not include the leading slash on closing tags.
		 *		2* - Properties.  Each capture is a single key/value pair, or in certain cases, a standalone modifier without a value
		 *			   such as "nowrap".
		 *
		 */
		[GeneratedRegex("""^</? *([a-z0-9!]+)( +(?:[a-z\-]+ *= *"(?:\\"|[^">])*"|[a-z\-]+ *= *'(?:\\'|[^'>])*'|[a-z\-]+ *= *[a-z0-9\-_%]+|checked|compact|declare|defer|disabled|ismap|multiple|nohref|noresize|noshade|nowrap|readonly|selected))* */?>$""",
								  RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
		static private partial Regex IsHTMLTagRegex();


		/* Regex: IsHTMLTagPropertyRegex
		 *
		 * Will match if the entire string is a HTML property key/value pair.  This does not match standalone modifiers such as
		 * "nowrap".
		 *
		 * Capture Groups:
		 *
		 *		1 - The property name.
		 *		2 - The property value expression exactly as it appears.  It will include the surrounding quotes and the value will
		 *			  not be unescaped.
		 */
		[GeneratedRegex("""^ *([a-z0-9!]+) *= *("(?:\\"|[^">])*"|'(?:\\'|[^'>])*'|[a-z0-9\-_%]+)$""",
								  RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
		static private partial Regex IsHTMLTagPropertyRegex();

		}
	}
