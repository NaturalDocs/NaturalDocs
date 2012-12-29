/* 
 * Class: GregValure.NaturalDocs.Engine.Languages.ParentElement
 * ____________________________________________________________________________
 * 
 * A <Element> that also may contain child elements.  This is used not only for classes but for groups (so that settings
 * may be inherited) and list topics and enums (to store embedded topics.)
 * 
 */

// This file is part of Natural Docs, which is Copyright © 2003-2012 Greg Valure.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;
using GregValure.NaturalDocs.Engine.Symbols;
using GregValure.NaturalDocs.Engine.Tokenization;


namespace GregValure.NaturalDocs.Engine.Languages
	{

	public class ParentElement : Element
		{

		// Group: Functions
		// __________________________________________________________________________


		/* Constructor: ParentElement
		 */
		public ParentElement (TokenIterator iterator, Flags flags) : base (iterator, flags)
			{  
			CommonInit();
			}


		/* Constructor: ParentElement
		 */
		public ParentElement (LineIterator lineIterator, Flags flags) : base (lineIterator, flags)
			{  
			CommonInit();
			}


		/* Constructor: ParentElement
		 */
		public ParentElement (int lineNumber, int charNumber, Flags flags) : base (lineNumber, charNumber, flags)
			{  
			CommonInit();
			}


		/* Function: CommonInit
		 * Common initialization done from all the constructors.
		 */
		private void CommonInit ()
			{
			parentAccessLevel = AccessLevel.Unknown;
			defaultChildAccessLevel = AccessLevel.Unknown;
			defaultChildLanguageID = 0;
			childContextString = new ContextString();
			childContextStringSet = false;

			endingLineNumber = -1;
			endingCharNumber = -1;
			}



		// Group: Properties
		// __________________________________________________________________________


		/* Property: ParentAccessLevel
		 * The access level of the element if it's set, or <AccessLevel.Unknown> if not.  This is used for when the parent's
		 * level influences the children.  For example, a class marked "internal" can have public members, but those members are
		 * really internal from a global perspective.
		 */
		public AccessLevel ParentAccessLevel
			{
			get
				{  return parentAccessLevel;  }
			set
				{  parentAccessLevel = value;  }
			}

		/* Property: DefaultChildAccessLevel
		 * The access level of this element's children if not otherwise specified.
		 */
		public AccessLevel DefaultChildAccessLevel
			{
			get
				{  return defaultChildAccessLevel;  }
			set
				{  defaultChildAccessLevel = value;  }
			}

		/* Property: DefaultChildLanguageID
		 * The language ID that applies to all children, or zero if it's not set.
		 */
		public int DefaultChildLanguageID
			{
			get
				{  return defaultChildLanguageID;  }
			set
				{  defaultChildLanguageID = value;  }
			}

		/* Property: ChildContextString
		 * The <ContextString> all children should use, if set.  Use <ChildContextStringSet> to see if this value is relevant, as
		 * null can be a valid value.  Setting this value will automatically set <ChildContextStringSet> to true.
		 */
		public ContextString ChildContextString
			{
			get
				{  return childContextString;  }
			set
				{  
				childContextString = value;  
				childContextStringSet = true;
				}
			}

		/* Property: ChildContextStringSet
		 * Whethe <ChildContextString> was set for this element, and thus whether its value is relevant.
		 */
		public bool ChildContextStringSet
			{
			get
				{  return childContextStringSet;  }
			set
				{  childContextStringSet = value;  }
			}


		/* Property: EndingLineNumber
		 * The line number where the parent's influence ends, or -1 if it hasn't been set yet.  The first line number is one, 
		 * not zero.
		 */
		public int EndingLineNumber
			{
			get
				{  return endingLineNumber;  }
			set
				{  endingLineNumber = value;  }
			}


		/* Property: EndingCharNumber
		 * The character number where the parent's influence ends, or -1 if it hasn't been set yet.  The first character number 
		 * is one, not zero, and is relative to the line, not the file.
		 */
		public int EndingCharNumber
			{
			get
				{  return endingCharNumber;  }
			set
				{  endingCharNumber = value;  }
			}



		// Group: Variables
		// __________________________________________________________________________


		/* var: parentAccessLevel
		 * The access level of the parent, or <AccessLevel.Unknown> if it's not set.
		 */
		protected AccessLevel parentAccessLevel;

		/* var: defaultChildAccessLevel
		 * The default access level of all children.
		 */
		protected AccessLevel defaultChildAccessLevel;

		/* var: defaultChildLanguageID
		 * The default language ID of all children, or zero if it's not set.
		 */
		protected int defaultChildLanguageID;

		/* var: childContextString
		 * The <ContextString> to be used by children.  Since null is a valid value, check <childContextStringSet> to see whether
		 * it's changed by this element or not.
		 */
		protected ContextString childContextString;

		/* var: childContextStringSet
		 * Whether <childContextString> is set for the current element.
		 */
		protected bool childContextStringSet;

		/* var: endingLineNumber
		 * The line number where the parent's influence ends, or -1 if it hasn't been set yet.
		 */
		protected int endingLineNumber;

		/* var: endingCharNumber
		 * The character number where the parent's influence ends, or -1 if it hasn't been set yet.  The first character is one, not 
		 * zero, and is relative to <endingLineNumber>, not the file.
		 */
		protected int endingCharNumber;

		}
	}