/* 
 * Struct: GregValure.NaturalDocs.Engine.Comments.Components.XMLIterator
 * ____________________________________________________________________________
 * 
 * A struct to handle walking through XML-formatted content.  It moves by element, treating things like tags
 * and stretches of unformatted text as one step.  The iterator assumes you are going to walk through it in
 * a linear fashion rather than navigating a parsed XML tree.
 * 
 */

// This file is part of Natural Docs, which is Copyright © 2003-2012 Greg Valure.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using GregValure.NaturalDocs.Engine.Tokenization;


namespace GregValure.NaturalDocs.Engine.Comments.Components
	{
	public struct XMLIterator
		{

		// Group: Functions
		// __________________________________________________________________________


		/* Constructor: XMLIterator
		 * Creates a new XML iterator that will navigate between the two <TokenIterators>.
		 */
		public XMLIterator (TokenIterator start, TokenIterator end)
			{
			tokenIterator = start;
			endingRawTextIndex = end.RawTextIndex;

			type = XMLElementType.OutOfBounds;
			length = 0;
			tagType = null;

			DetermineElement();
			}


		/* Function: DetermineElement
		 * Determines which <XMLElementType> the iterator is currently on, setting <type> and <length>.
		 */
		private void DetermineElement ()
			{
			bool found = false;

			if (tokenIterator == null || !IsInBounds)
				{
				type = XMLElementType.OutOfBounds;
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
				type = XMLElementType.Indent;
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
				type = XMLElementType.LineBreak;
				length = tokenIterator.RawTextLength;
				found = true;
				}
			else if (tokenIterator.Character == '<')
				{
				int nextBracketIndex = RawText.IndexOfAny(AngleBrackets, RawTextIndex + 1);

				if (nextBracketIndex != -1 && nextBracketIndex < endingRawTextIndex && RawText[nextBracketIndex] == '>')
					{
					type = XMLElementType.Tag;
					length = nextBracketIndex + 1 - RawTextIndex;

					Match tagMatch = TagRegex.Match(RawText, RawTextIndex, length);

					if (tagMatch.Success)
						{
						found = true;
						tagType = tagMatch.Groups[1].ToString().ToLower();
						}
					}
				}
			else if (tokenIterator.Character == '&')
				{
				int semicolonIndex = RawText.IndexOf(';', RawTextIndex + 1);

				if (semicolonIndex != -1 && semicolonIndex < endingRawTextIndex)
					{
					type = XMLElementType.EntityChar;
					length = semicolonIndex + 1 - RawTextIndex;

					found = (NonASCIILettersRegex.Match(RawText, RawTextIndex + 1, length - 2).Success == false);
					}
				}

			if (!found)
				{
				type = XMLElementType.Text;

				int nextElementIndex = RawText.IndexOfAny(StartOfElement, RawTextIndex + 1);

				if (nextElementIndex == -1)
					{  length = endingRawTextIndex - RawTextIndex;  }
				else
					{  length = nextElementIndex - RawTextIndex;  }
				}
			}


		/* Function: Next
		 * Moves the iterator forward by the passed number of elements, returning whether it's still in bounds.
		 */
		public bool Next (int count = 1)
			{
			while (count > 0)
				{
				if (type == XMLElementType.OutOfBounds)
					{  return false;  }

				tokenIterator.NextByCharacters(length);
				DetermineElement();

				count--;
				}

			return (type != XMLElementType.OutOfBounds);
			}


		/* Function: GetAllTagProperties
		 * If <Type> is <XMLElementType.Tag>, this generates a dictionary of all the properties in the tag, if any.
		 */
		public Dictionary<string, string> GetAllTagProperties ()
			{
			if (type != XMLElementType.Tag)
				{  throw new InvalidOperationException();  }

			Dictionary<string, string> properties = new Dictionary<string,string>();

			Match match = TagRegex.Match(RawText, RawTextIndex, length);
			CaptureCollection captures = match.Groups[2].Captures;

			foreach (Capture capture in captures)
				{
				Match propertyMatch = TagPropertyRegex.Match(RawText, capture.Index, capture.Length);
				properties.Add( propertyMatch.Groups[1].ToString(), 
										DecodePropertyValue(propertyMatch.Groups[2].Index, propertyMatch.Groups[2].Length) );
				}

			return properties;
			}


		/* Function: DecodePropertyValue
		 * Extracts the property value from the passed XML value, stripping off the surrounding quotes and any escaped quotes
		 * within.
		 */
		private string DecodePropertyValue (int valueIndex, int valueLength)
			{
			string value = RawText.Substring(valueIndex + 1, valueLength - 2);

			value = value.Replace("\\\"", "\"");
			value = value.Replace("\\'", "'");
			value = value.EntityDecode();

			return value;
			}


		
		// Group: Properties
		// __________________________________________________________________________


		/* Property: Type
		 * The type of content the iterator is currently on.
		 */
		public XMLElementType Type
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
		 * Returns the element as a string.  Because this returns a copy of the element string, whenever possible you should 
		 * avoid this property and use functions like <AppendTo()> instead.  These functions are more efficient because they
		 * don't need to make an intermediate copy.
		 */
		public string String
			{
			get
				{
				if (length == 0)
					{  return "";  }
				else if (type == XMLElementType.Indent)
					{  
					// Don't want comment symbols or tabs, so convert to spaces
					return new string(' ', Indent);  
					}
				else
					{  return RawText.Substring(RawTextIndex, length);  }
				}
			}


		/* Property: TagType
		 * If <Type> is <XMLElementType.Tag>, this is the tag type as a string.  For example, "<br />" would return "br".  It
		 * will always be in lowercase.
		 */
		public string TagType
			{
			get
				{
				if (type != XMLElementType.Tag)
					{  throw new InvalidOperationException();  }

				return tagType;
				}
			}


		/* Property: TagForm
		 * If <Type> is <XMLElementType.Tag>, this is the <XMLTagForm> it takes.
		 */
		public XMLTagForm TagForm
			{
			get
				{
				if (type != XMLElementType.Tag)
					{  throw new InvalidOperationException();  }

				if (RawText[RawTextIndex + 1] == '/')
					{  return XMLTagForm.Closing;  }
				else if (RawText[RawTextIndex + length - 2] == '/')
					{  return XMLTagForm.Standalone;  }
				else
					{  return XMLTagForm.Opening;  }
				}
			}


		/* Function: TagProperty
		 * If <Type> is <XMLElementType.Tag>, return the value of the passed property, or null if it doesn't exist.  The property
		 * name is case-insensitive.
		 */
		public string TagProperty (string name)
			{
			if (type != XMLElementType.Tag)
				{  throw new InvalidOperationException();  }

			Match match = TagRegex.Match(RawText, RawTextIndex, length);
			CaptureCollection captures = match.Groups[2].Captures;

			foreach (Capture capture in captures)
				{
				Match propertyMatch = TagPropertyRegex.Match(RawText, capture.Index, capture.Length);

				if (name.Length == propertyMatch.Groups[1].Length &&
					string.Compare(name, 0, RawText, propertyMatch.Groups[1].Index, name.Length, true) == 0)
					{
					return DecodePropertyValue(propertyMatch.Groups[2].Index, propertyMatch.Groups[2].Length);
					}
				}

			return null;
			}


		/* Property: EntityValue
		 * If <Type> is <XMLElementType.EntityChar>, this is the decoded character.
		 */
		public string EntityValue
			{
			get
				{
				if (type != XMLElementType.EntityChar)
					{  throw new InvalidOperationException();  }

				if (tokenIterator.MatchesAcrossTokens("&amp;", true))
					{  return "&";  }
				else if (tokenIterator.MatchesAcrossTokens("&lt;", true))
					{  return "<";  }
				else if (tokenIterator.MatchesAcrossTokens("&gt;", true))
					{  return ">";  }
				else if (tokenIterator.MatchesAcrossTokens("&quot;", true))
					{  return "\"";  }
				else
					{  return RawText.Substring(RawTextIndex, length);  }
				}
			}


		/* Property: Indent
		 * If <Type> is <XMLElementType.Indent>, this is the indent length with tabs expanded.
		 */
		public int Indent
			{
			get
				{
				if (type != XMLElementType.Indent)
					{  throw new InvalidOperationException();  }

				int indent = 0;
				string rawText = RawText;
				int rawTextIndex = RawTextIndex;
			
				for (int i = 0; i < length; i++)
					{
					if (rawText[rawTextIndex + i] == '\t')
						{
						indent += Engine.Instance.Config.TabWidth;
						indent -= (indent % Engine.Instance.Config.TabWidth);
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
		 * The <XMLElementType> of the current element.
		 */
		private XMLElementType type;

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
		static private char[] StartOfElement = { '<', '&', '\n', '\r' };

		static private Regex.NonASCIILetters NonASCIILettersRegex = new Regex.NonASCIILetters();

		static private Regex.Comments.XML.Tag TagRegex = new Regex.Comments.XML.Tag();
		static private Regex.Comments.XML.TagProperty TagPropertyRegex = new Regex.Comments.XML.TagProperty();

		}
	}