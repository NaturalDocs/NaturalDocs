/*
 * Class: CodeClear.NaturalDocs.Engine.Links.LinkInterpretation
 * ____________________________________________________________________________
 *
 * A class representing a possible interpretation of a Natural Docs link in <NDMarkup>.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2024 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;


namespace CodeClear.NaturalDocs.Engine.Links
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
			namedLink = false;
			pluralConversion = false;
			possessiveConversion = false;
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

		/* Property: NamedLink
		 * Whether the interpretation was made with a named link.
		 */
		public bool NamedLink
			{
			get
				{  return namedLink;  }
			set
				{  namedLink = value;  }
			}

		/* Property: PluralConversion
		 * Whether this interpretation was made with a plural conversion.
		 */
		public bool PluralConversion
			{
			get
				{  return pluralConversion;  }
			set
				{  pluralConversion = value;  }
			}

		/* Property: PossessiveConversion
		 * Whether this interpretation uses a possessive conversion.
		 */
		public bool PossessiveConversion
			{
			get
				{  return possessiveConversion;  }
			set
				{  possessiveConversion = value;  }
			}

		/* Property: IsLiteral
		 * Whether this interpretation is not made from an "at" keyword, plural, or possessive conversion.
		 */
		public bool IsLiteral
			{
			get
				{  return (!NamedLink && !PluralConversion && !PossessiveConversion);  }
			}



		// Group: Variables
		// __________________________________________________________________________


		/* var: target
		 *
		 * The link target.
		 *
		 * This isn't a SymbolString because LinkInterpretations may be used to find named URL and e-mail links, not
		 * just Natural Docs links, and the target shouldn't be normalized in those cases.
		 */
		protected string target;

		/* var: text
		 * The link text, if different than the target.  If null, <Text> returns <target>.
		 */
		protected string text;

		/* var: namedLink
		 * Whether the interpretation was made with a named link.
		 */
		protected bool namedLink;

		/* var: pluralConversion
		 * Whether the interpretation was made with a plural conversion.
		 */
		protected bool pluralConversion;

		/* var: possessiveConversion
		 * Whether the interpretation was made with a possessive conversion.
		 */
		protected bool possessiveConversion;

		}
	}
