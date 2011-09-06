/* 
 * Class: GregValure.NaturalDocs.Engine.NDMarkup.Iterator
 * ____________________________________________________________________________
 * 
 * A class to handle walking through a <NDMarkup>-formatted string.  It moves by element, treating things like tags
 * and stretches of unformatted text as one step.
 * 
 */

// This file is part of Natural Docs, which is Copyright © 2003-2011 Greg Valure.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Text;


namespace GregValure.NaturalDocs.Engine.NDMarkup
	{
	public struct Iterator
		{

		// Group: Types
		// __________________________________________________________________________


		/* enum: ElementType
		 */
		public enum ElementType : byte
			{
			OutOfBounds,
			Text,

			LTEntityChar, GTEntityChar, QuoteEntityChar, AmpEntityChar,

			LowestTagValue,

			ParagraphTag,
			HeadingTag,
			PreTag, PreLineBreakTag,
			ImageTag,
			BulletListTag, BulletListItemTag,
			DefinitionListTag, DefinitionListEntryTag, DefinitionListDefinitionTag,

			BoldTag, ItalicsTag, UnderlineTag,
			LinkTag,

			HighestTagValue
			}



		// Group: Functions
		// __________________________________________________________________________
		
		
		/* Constructor: Iterator
		 * Static constructor.
		 */
		static Iterator ()
			{
			TagNameToElementType = new Collections.StringTable<ElementType>(false, false);

			TagNameToElementType.Add("p", ElementType.ParagraphTag);
			TagNameToElementType.Add("h", ElementType.HeadingTag);
			TagNameToElementType.Add("pre", ElementType.PreTag);
			TagNameToElementType.Add("br", ElementType.PreLineBreakTag);
			TagNameToElementType.Add("image", ElementType.ImageTag);
			TagNameToElementType.Add("ul", ElementType.BulletListTag);
			TagNameToElementType.Add("li", ElementType.BulletListItemTag);
			TagNameToElementType.Add("dl", ElementType.DefinitionListTag);
			TagNameToElementType.Add("de", ElementType.DefinitionListEntryTag);
			TagNameToElementType.Add("dd", ElementType.DefinitionListDefinitionTag);
			TagNameToElementType.Add("b", ElementType.BoldTag);
			TagNameToElementType.Add("i", ElementType.ItalicsTag);
			TagNameToElementType.Add("u", ElementType.UnderlineTag);
			TagNameToElementType.Add("link", ElementType.LinkTag);

			EntityCharToElementType = new Collections.StringTable<ElementType>(false, false);

			EntityCharToElementType.Add("lt", ElementType.LTEntityChar);
			EntityCharToElementType.Add("gt", ElementType.GTEntityChar);
			EntityCharToElementType.Add("quot", ElementType.QuoteEntityChar);
			EntityCharToElementType.Add("amp", ElementType.AmpEntityChar);
			}


		/* Constructor: Iterator
		 * Instance constructor.
		 */
		public Iterator (string ndMarkup)
			{
			content = ndMarkup;
			index = 0;

			type = ElementType.OutOfBounds;
			length = 0;
			isOpeningTag = false;

			DetermineElement();
			}


		/* Function: DetermineElement
		 * Determines which <ElementType> the iterator is currently on, setting <elementType>, <elementLength>, and if
		 * appropriate, <isOpeningTag>.
		 */
		private void DetermineElement ()
			{
			if (index >= content.Length)
				{
				type = ElementType.OutOfBounds;
				length = 0;
				}

			else if (content[index] == '&')
				{
				int semicolonIndex = content.IndexOf(';', index + 1);

				if (semicolonIndex == -1)
					{  throw new Exceptions.InvalidMarkup(content, index);  }

				length = semicolonIndex - index + 1;

				string entity = content.Substring(index + 1, length - 2);
				bool found = EntityCharToElementType.TryGetValue(entity, out type);

				if (found == false)
					{  throw new Exceptions.InvalidMarkup(content, index + 1);  }
				}

			else if (content[index] == '<')
				{
				int closingBracketIndex = content.IndexOf('>', index + 1);

				if (closingBracketIndex == -1)
					{  throw new Exceptions.InvalidMarkup(content, index);  }

				length = closingBracketIndex - index + 1;

				// All NDMarkup is machine generated and thus we can normally just assume it is valid.  Perform tag
				// validation in debug code though.
				#if DEBUG
					if (TagValidationRegex.Match(content, index, length).Success == false)
						{  throw new Exceptions.InvalidTag(content, index, length);  }
				#endif

				int tagNameStartIndex = index + 1;

				if (content[tagNameStartIndex] == '/')
					{
					isOpeningTag = false;
					tagNameStartIndex++;
					}
				else
					{  isOpeningTag = true;  }

				int tagNameEndIndex = index + length - 1;
				int firstSpaceIndex = content.IndexOf(' ', tagNameStartIndex + 1, tagNameEndIndex - (tagNameStartIndex + 1));

				if (firstSpaceIndex != -1)
					{  tagNameEndIndex = firstSpaceIndex;  }

				string tag = content.Substring(tagNameStartIndex, tagNameEndIndex - tagNameStartIndex);
				bool found = TagNameToElementType.TryGetValue(tag, out type);

				if (found == false)
					{  throw new Exceptions.InvalidMarkup(content, tagNameStartIndex);  }
				}

			else
				{
				type = ElementType.Text;

				int nextElement = content.IndexOfAny(AmpersandOrOpeningBracket, index + 1);

				if (nextElement == -1)
					{  length = content.Length - index;  }
				else
					{  length = nextElement - index;  }
				}
			}


		/* Function: Next
		 * Moves the iterator forward, returning whether it's still in bounds.
		 */
		public bool Next (int count = 1)
			{
			while (count > 0)
				{
				if (type == ElementType.OutOfBounds)
					{  return false;  }

				index += length;
				DetermineElement();

				count--;
				}

			return (type != ElementType.OutOfBounds);
			}


		/* Function: AppendTo
		 * Appends the current element string to the passed StringBuilder.  This is more efficient than appending
		 * <ElementString> because it works from the original memory instead of making an intermediate copy.
		 */
		public void AppendTo (StringBuilder stringBuilder)
			{
			if (length != 0)
				{  stringBuilder.Append(content, index, length);  }
			}


		/* Function: Property
		 * If the iterator is on a tag and it contains the passed property, returns it sans quotes.  The value is 
		 * automatically passed through <TextConverter.DecodeEntityChars()>.  If the property isn't defined it 
		 * will return null.
		 */
		public string Property (string propertyName)
			{
			if (type > ElementType.HighestTagValue || type < ElementType.LowestTagValue)
				{  return null;  }

			int searchIndex = index + 1;
			int closingBracketIndex = index + length - 1;
			
			int spaceIndex = content.IndexOf(' ', searchIndex, closingBracketIndex - searchIndex);
			if (spaceIndex == -1)
				{  return null;  }

			do
				{
				// We can simply assume the text is valid NDMarkup and not worry about bounds checking or IndexOf() returning -1.
				// The tag format was already validated in DetermineElement().

				int propertyStartIndex = spaceIndex + 1;
				int propertyEndIndex = content.IndexOf('=', propertyStartIndex + 1, closingBracketIndex - (propertyStartIndex + 1));
				int contentStartIndex = propertyEndIndex + 2;  // Skip equals and opening quote
				int contentEndIndex = content.IndexOf('"', contentStartIndex, closingBracketIndex - contentStartIndex);

				if (propertyEndIndex - propertyStartIndex == propertyName.Length &&
					 string.Compare(propertyName, 0, content, propertyStartIndex, propertyEndIndex - propertyStartIndex) == 0)
					{
					return content.Substring(contentStartIndex, contentEndIndex - contentStartIndex).EntityDecode();
					}

				spaceIndex = contentEndIndex + 1;  // Skip closing quote
				}
			while (content[spaceIndex] == ' ');

			return null;
			}
			
			
			
		// Group: Properties
		// __________________________________________________________________________
		
		
		/* Property: Type
		 * The type of content the iterator is currently on.
		 */
		public ElementType Type
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
		 * Returns the element contents as a string.  This allocates a copy of the string section, so whenever possible you
		 * should avoid this property and use things like <AppendTo()>.  These functions are more efficient because they work
		 * off the original memory instead of making an intermediate copy.
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

		/* Property: IsOpeningTag
		 * If <Element> is on a tag, whether it is an opening or a closing tag.  This value is undefined if the iterator is
		 * on a non-tag or standalone tag element.
		 */
		public bool IsOpeningTag
			{
			get
				{  return isOpeningTag;  }
			}

		/* Property: IsClosingTag
		 * If <Element> is on a tag, whether it is an opening or a closing tag.  This value is undefined if the iterator is
		 * on a non-tag or standalone tag element.
		 */
		public bool IsClosingTag
			{
			get
				{  return !isOpeningTag;  }
			}

		/* Property: IsInBounds
		 */
		public bool IsInBounds
			{
			get
				{  return (index < content.Length);  }
			}

		/* Property: RawTextIndex
		 * The iterator's position as an index into the string.
		 */
		public int RawTextIndex
			{
			get
				{  return index;  }
			}
			

		
		// Group: Variables
		// __________________________________________________________________________
		

		/* var: content
		 * The <NDMarkup>-formatted string being iterated over.
		 */
		private string content;

		/* var: index
		 * The current position into <content>.
		 */
		private int index;
								
		/* var: elementType
		 * The current element.
		 */
		private ElementType type;

		/* var: elementLength
		 * The length of the current element.
		 */
		private int length;

		/* var: isOpeningTag
		 * If the <ElementType> is a tag, whether it is an opening or closing tag.  The value is undefined if the iterator is on
		 * a non-tag element or a standalone tag element.
		 */
		private bool isOpeningTag;



		// Group: Static Variables
		// __________________________________________________________________________

		private static char[] SpaceOrClosingBracket = { ' ', '>' };
		private static char[] AmpersandOrOpeningBracket = { '&', '<' };

		private static Collections.StringTable<ElementType> TagNameToElementType = null;
		private static Collections.StringTable<ElementType> EntityCharToElementType = null;

		#if DEBUG
			private static System.Text.RegularExpressions.Regex TagValidationRegex
				= new System.Text.RegularExpressions.Regex("^</?[a-z]+(?: [a-z]+=\"[^<>\"]*\")*>$", 
																								  System.Text.RegularExpressions.RegexOptions.Compiled | 
																								  System.Text.RegularExpressions.RegexOptions.Singleline);
		#endif

		}
	}