/* 
 * Struct: GregValure.NaturalDocs.Engine.Comments.Parsers.XMLIterator
 * ____________________________________________________________________________
 * 
 * A struct to handle walking through an XML-formatted string.  It moves by element, treating things like tags
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


namespace GregValure.NaturalDocs.Engine.Comments.Parsers
	{
	public struct XMLIterator
		{

		// Group: Functions
		// __________________________________________________________________________


		/* Constructor: XMLIterator
		 */
		public XMLIterator (string xml)
			{
			content = xml;
			index = 0;

			type = XMLElementType.OutOfBounds;
			length = 0;

			DetermineElement();
			}


		/* Function: DetermineElement
		 * Determines which <XMLElementType> the iterator is currently on, setting <type> and <length>.
		 */
		private void DetermineElement ()
			{
			bool found = false;

			if (content == null || index < 0 || index >= content.Length)
				{
				type = XMLElementType.OutOfBounds;
				length = 0;
				found = true;
				}
			else if (content[index] == '<')
				{
				int nextBracketIndex = content.IndexOfAny(AngleBrackets, index + 1);

				if (nextBracketIndex != -1 && content[nextBracketIndex] == '>')
					{
					type = XMLElementType.Tag;
					length = nextBracketIndex + 1 - index;

					found = TagRegex.Match(content, index, length).Success;
					}
				}
			else if (content[index] == '&')
				{
				int semicolonIndex = content.IndexOf(';', index + 1);

				if (semicolonIndex != -1)
					{
					type = XMLElementType.EntityChar;
					length = semicolonIndex + 1 - index;

					found = (NonASCIILettersRegex.Match(content, index + 1, length - 2).Success == false);
					}
				}

			if (!found)
				{
				type = XMLElementType.Text;

				int nextElementIndex = content.IndexOfAny(OpeningAngleBracketAndAmp, index + 1);

				if (nextElementIndex == -1)
					{  length = content.Length - index;  }
				else
					{  length = nextElementIndex - index;  }
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

				index += length;
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

			Match match = TagRegex.Match(content, index, length);
			CaptureCollection captures = match.Groups[2].Captures;

			foreach (Capture capture in captures)
				{
				Match propertyMatch = TagPropertyRegex.Match(content, capture.Index, capture.Length);
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
			string value = content.Substring(valueIndex + 1, valueLength - 2);

			value = value.Replace("\\\"", "\"");
			value = value.Replace("\\'", "'");

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
				else
					{  return content.Substring(index, length);  }
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

				var match = TagRegex.Match(content, index, length);

				return match.Groups[1].ToString().ToLower();
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

				if (content[index + 1] == '/')
					{  return XMLTagForm.Closing;  }
				else if (content[index + length - 2] == '/')
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

			Match match = TagRegex.Match(content, index, length);
			CaptureCollection captures = match.Groups[2].Captures;

			foreach (Capture capture in captures)
				{
				Match propertyMatch = TagPropertyRegex.Match(content, capture.Index, capture.Length);

				if (name.Length == propertyMatch.Groups[1].Length &&
					string.Compare(name, 0, content, propertyMatch.Groups[1].Index, name.Length, true) == 0)
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

				string entity = content.Substring(index + 1, length - 2).ToLower();

				if (entity == "amp")
					{  return "&";  }
				else if (entity == "lt")
					{  return "<";  }
				else if (entity == "gt")
					{  return ">";  }
				else if (entity == "quot")
					{  return "\"";  }
				else
					{  return content.Substring(index, length);  }
				}
			}


		/* Property: IsInBounds
		 */
		public bool IsInBounds
			{
			get
				{  return (index < content.Length);  }
			}


		/* Property: RawTextIndex
		 * The iterators position as an index into the string.
		 */
		public int RawTextIndex
			{
			get
				{  return index;  }
			}



		// Group: Variables
		// __________________________________________________________________________


		/* var: content
		 * The XML string we are iterating over.
		 */
		private string content;

		/* var: index
		 * The current position of the iterator in <content>.
		 */
		private int index;

		/* var: type
		 * The <XMLElementType> of the current element.
		 */
		private XMLElementType type;

		/* var: length
		 * The length of the current element.
		 */
		private int length;



		// Group: Static Variables
		// __________________________________________________________________________


		static private char[] AngleBrackets = { '<', '>' };
		static private char[] OpeningAngleBracketAndAmp = { '<', '&' };

		static private Regex.NonASCIILetters NonASCIILettersRegex = new Regex.NonASCIILetters();

		static private Regex.Comments.XML.Tag TagRegex = new Regex.Comments.XML.Tag();
		static private Regex.Comments.XML.TagProperty TagPropertyRegex = new Regex.Comments.XML.TagProperty();

		}
	}