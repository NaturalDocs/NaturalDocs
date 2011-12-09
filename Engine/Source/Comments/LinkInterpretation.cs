/* 
 * Class: GregValure.NaturalDocs.Engine.Comments.LinkInterpretation
 * ____________________________________________________________________________
 * 
 * A class representing a possible interpretation of a Natural Docs link in <NDMarkup>.
 * 
 * Some of the properties seem overly specific, like <PluralConversionIndex>, but they must be recorded so that
 * it's possible to score these interpretations on a consistent scale.  Although which conversion has a lower index
 * may be arbitrary, at least it allows them to always rank in the same position on a list.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2011 Greg Valure.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;


namespace GregValure.NaturalDocs.Engine.Comments
	{
	public class LinkInterpretation
		{
		
		// Group: Functions
		// __________________________________________________________________________
		
		
		/* Function: LinkInterpretation
		 */
		public LinkInterpretation ()
			{
			target = null;
			text = null;
			atKeywordPosition = -1;
			pluralConversionIndex = -1;
			possessiveConversionIndex = -1;
			}
						
			
			
		// Group: Properties
		// __________________________________________________________________________
		
		
		/* Property: Target
		 * The link target.
		 */
		public string Target
			{
			get
				{  return target;  }
			set
				{  target = value;  }
			}
			
		/* Property: Text
		 * The link text.  If you set it to null, it will automatically return <Target> when you attempt to read it.
		 */
		public string Text
			{
			get
				{
				if (text == null)
					{  return target;  }
				else
					{  return text;  }
				}
			set
				{  text = value;  }
			}

		/* Property: UsesAtKeyword
		 * Whether this interpretation is made with an "at" keyword.
		 */
		public bool UsesAtKeyword
			{
			get
				{  return (atKeywordPosition == -1);  }
			}
			
		/* Property: AtKeywordPosition
		 * The index into the original target string of where the "at" keyword appears, or -1 if it doesn't.
		 */
		public int AtKeywordPosition
			{
			get
				{  return atKeywordPosition;  }
			set
				{  atKeywordPosition = value;  }
			}

		/* Property: UsesPluralConversion
		 * Whether this interpretation is made with a plural conversion.
		 */
		public bool UsesPluralConversion
			{
			get
				{  return (pluralConversionIndex == -1);  }
			}
			
		/* Property: PluralConversionIndex
		 * The index into the list of plural conversions of the one that was applied, or -1 if one wasn't.
		 */
		public int PluralConversionIndex
			{
			get
				{  return pluralConversionIndex;  }
			set
				{  pluralConversionIndex = value;  }
			}

		/* Property: UsesPossessiveConversion
		 * Whether this interpretation uses a possessive conversion.
		 */
		public bool UsesPossessiveConversion
			{
			get
				{  return (possessiveConversionIndex == -1);  }
			}
			
		/* Property: PossessiveConversionIndex
		 * The index into the list of possessive conversions of the one that was applied, or -1 if one wasn't.
		 */
		public int PossessiveConversionIndex
			{
			get
				{  return possessiveConversionIndex;  }
			set	
				{  possessiveConversionIndex = value;  }
			}

		/* Property: IsLiteral
		 * Whether this interpretation is not made from an "at" keyword, plural, or possessive conversion.
		 */
		public bool IsLiteral
			{
			get
				{  return (!UsesAtKeyword && !UsesPluralConversion && !UsesPossessiveConversion);  }
			}
			
			
			
		// Group: Variables
		// __________________________________________________________________________
		
		
		/* var: target
		 * The link target.
		 */
		protected string target;
		
		/* var: text
		 * The link text, if different than the target.  If null, <Text> returns <target>.
		 */
		protected string text;
		
		/* var: atKeywordPosition
		 * The position in the original target string where the "at" keyword appeared, or -1 if none.
		 */
		protected int atKeywordPosition;
		
		/* var: pluralConversionIndex
		 * The index into the list of possible plural conversions that was applied, or -1 if none.
		 */
		protected int pluralConversionIndex;
		
		/* var: possessiveConversionIndex
		 * The index into the list of possible possessive conversions that was applied, or -1 if none.
		 */
		protected int possessiveConversionIndex;
		
		}
	}