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
			children = null;

			parentAccessLevel = AccessLevel.Unknown;
			defaultChildAccessLevel = AccessLevel.Unknown;
			defaultChildLanguageID = 0;
			childContextString = new ContextString();
			childContextStringSet = false;
			}


		/* Function: AddChild
		 * Adds a child to this element.  You cannot mix embedded and non-embedded children.
		 */
		public void AddChild (Element child)
			{
			if (children == null)
				{  children = new List<Element>();  }

			children.Add(child);
			child.Parent = this;

			#if DEBUG
			bool hasEmbedded = false;
			bool hasNonEmbedded = false;

			for (int i = 0; i < children.Count; i++)
				{
				if (children[i].Topic != null)
					{
					if (children[i].Topic.IsEmbedded)
						{  hasEmbedded = true;  }
					else
						{  hasNonEmbedded = true;  }
					}
				}

			if (hasEmbedded && hasNonEmbedded)
				{  throw new Exception ("You cannot add both embedded and non-embedded elements to the same parent.");  }
			#endif
			}



		// Group: Properties
		// __________________________________________________________________________


		/* Property: Children
		 * The list of children this element has, or null if none.
		 */
		public List<Element> Children
			{
			get
				{  return children;  }
			}


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


		/* Property: AllElements
		 * Returns an enumerator that can iterate through all <Elements> in the hierarchy.  It will return the current element,
		 * then all its children and their children in a depth-first ordering.
		 */
		public IEnumerable<Element> AllElements
			{
			get
				{
				yield return this;

				if (children != null)
					{
					foreach (Element child in children)
						{
						if (child is ParentElement)
							{
							foreach (Element grandchild in (child as ParentElement).AllElements)
								{  yield return grandchild;  }
							}
						else
							{  yield return child;  } 
						}
					}
				}
			}



		// Group: Variables
		// __________________________________________________________________________


		/* var: children
		 * The list of children this element has, or null if none.
		 */
		protected List<Element> children;

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

		}
	}