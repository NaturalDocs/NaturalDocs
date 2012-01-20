/* 
 * Class: GregValure.NaturalDocs.Engine.Languages.CodePoint
 * ____________________________________________________________________________
 * 
 * A class to hold a point of interest found when parsing the source code.  This can be any combination of the
 * following:
 * 
 *		- A <Topic>, either from a comment or from full language parsing.
 *		- A context change, such as entering or exiting a class's scope or adding a "using" statement.
 *		- One or more class parent links.
 *		- A change in the default <AccessLevel>.
 *		
 * So a class topic would introduce a <Topic>, a context change, a change in the default <AccessLevel>, and
 * possibly class parent links.  A regular function topic would introduce a <Topic> and nothing more.  Exiting a
 * class's scope would introduce a context change and a change in the default <AccessLevel>.  A language like
 * Perl would have class parent links independent of class topics ("use parent 'something';") and a language like
 * C++ would have independent <AccessLevel> changes ("public:".)
 * 
 */

// This file is part of Natural Docs, which is Copyright © 2003-2012 Greg Valure.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;
using GregValure.NaturalDocs.Engine.Links;
using GregValure.NaturalDocs.Engine.Symbols;


namespace GregValure.NaturalDocs.Engine.Languages
	{

	public class CodePoint
		{

		// Group: Types
		// __________________________________________________________________________


		/* enum: Flags
		 * 
		 * InComments - The code point occurs via the comments.
		 * InCode - The code point occurs via full language support code parsing.
		 * 
		 * ContextChanged - The context changed at this code point.
		 * DefaultAccessLevelChanged - The default access level changed at this code point.
		 * 
		 * Both <InComments> and <InCode> may be set after merging the comment and code topics.
		 */
		[Flags]
		public enum Flags : byte
			{
			InComments = 0x01,
			InCode = 0x02,
			ContextChanged = 0x04,
			DefaultAccessLevelChanged = 0x08
			}



		// Group: Functions
		// __________________________________________________________________________


		/* Constructor: CodePoint
		 */
		public CodePoint (int charOffset, Flags flags)
			{
			topic = null;
			context = new ContextString();
			defaultAccessLevel = AccessLevel.Unknown;
			classParentLinks = null;

			this.charOffset = charOffset;
			this.flags = flags;

			#if DEBUG
				if (InCode == false && InComments == false)
					{  throw new Exception("CodePoints must be created with Flags.InComments or Flags.InCode set.");  }
			#endif
			}

		/* Constructor: CodePoint
		 */
		public CodePoint (Tokenization.TokenIterator iterator, Flags flags) : this (iterator.RawTextIndex, flags)
			{  }

		/* Constructor: CodePoint
		 */
		public CodePoint (Tokenization.LineIterator iterator, Flags flags) : this (iterator.RawTextIndex, flags)
			{  }

		/* Function: AddClassParentLink
		 * Adds a class parent <Link> to the code point.
		 */
		public void AddClassParentLink (Link link)
			{
			if (classParentLinks == null)
				{  classParentLinks = new List<Link>();  }

			#if DEBUG
				if (link.Type != LinkType.ClassParent)
					{  throw new InvalidOperationException();  }
			#endif

			classParentLinks.Add(link);
			}


		/* Function: AddClassParentLinks
		 * Adds all the class parent <Links> in the passed list to the code point.
		 */
		public void AddClassParentLinks (IList<Link> links)
			{
			if (classParentLinks == null)
				{  classParentLinks = new List<Link>(links.Count);  }

			foreach (Link link in links)
				{
				#if DEBUG
					if (link.Type != LinkType.ClassParent)
						{  throw new InvalidOperationException();  }
				#endif

				classParentLinks.Add(link);
				}
			}


		/* Function: ClearClassParentLinks
		 * Removes all class parent links from this code point.
		 */
		public void ClearClassParentLinks ()
			{
			classParentLinks = null;
			}



		// Group: Properties
		// __________________________________________________________________________


		/* Property: Topic
		 * The <Topic> created by this code point, or null if none.
		 */
		public Topic Topic
			{
			get
				{  return topic;  }
			set
				{  topic = value;  }
			}


		/* Property: Context
		 * The <ContextString> from this code forward.  If <ContextChanged> is false, attempting to read or set this
		 * property will throw an exception.
		 */
		public ContextString Context
			{
			get
				{
				if (ContextChanged == false)
					{  throw new InvalidOperationException();  }

				return context;
				}
			set
				{
				if (ContextChanged == false)
					{  throw new InvalidOperationException();  }

				context = value;
				}
			}


		/* Property: ContextChanged
		 * Whether <Context> was changed at this code point.  If false, the value returned by <Context> is irrelevant.
		 */
		public bool ContextChanged
			{
			get
				{  return ((flags & Flags.ContextChanged) != 0);  }
			set
				{
				if (value == true)
					{  flags |= Flags.ContextChanged;  }
				else
					{
					flags &= ~Flags.ContextChanged;
					context = new ContextString();
					}
				}
			}


		/* Property: DefaultAccessLevel
		 * The default <AccessLevel> from this code point forward.  If <DefaultAccessLevelChanged> is false, 
		 * attempting to read or set this property will cause an exception.
		 */
		public AccessLevel DefaultAccessLevel
			{
			get
				{  
				if (DefaultAccessLevelChanged == false)
					{  throw new InvalidOperationException();  }

				return defaultAccessLevel;  
				}
			set
				{
				if (DefaultAccessLevelChanged == false)
					{  throw new InvalidOperationException();  }

				defaultAccessLevel = value;
				}
			}


		/* Property: DefaultAccessLevelChanged
		 * Whether <DefaultAccessLevel> was changed at this code point.  If false, the value returned by <DefaultAccessLevel>
		 * is irrelevant.
		 */
		public bool DefaultAccessLevelChanged
			{
			get
				{  return ((flags & Flags.DefaultAccessLevelChanged) != 0);  }
			set
				{
				if (value == true)
					{  flags |= Flags.DefaultAccessLevelChanged;  }
				else
					{
					flags &= ~Flags.DefaultAccessLevelChanged;
					defaultAccessLevel = AccessLevel.Unknown;
					}
				}
			}


		/* Property: ClassParentLinks
		 * All the class parent <Links> at this code point, or null if none.
		 */
		public IList<Link> ClassParentLinks
			{
			get
				{  return classParentLinks;  }
			}


		/* Property: CharOffset
		 * The character offset into the source file at which this code point occurs.
		 */
		public int CharOffset
			{
			get
				{  return charOffset;  }
			}


		/* Property: InComments
		 * Whether this code point appears because of comment parsing.  It is possible for both InComments and <InCode>
		 * to be set.
		 */
		public bool InComments
			{
			get
				{  return ((flags & Flags.InComments) != 0);  }
			set
				{
				if (value == true)
					{  flags |= Flags.InComments;  }
				else
					{  flags &= ~Flags.InComments;  }
				}
			}


		/* Property: InCode
		 * Whether this code point appears via full language support code parsing.  It is possible for both InCode and <InComments>
		 * to be set.
		 */
		public bool InCode
			{
			get
				{  return ((flags & Flags.InCode) != 0);  }
			set
				{
				if (value == true)
					{  flags |= Flags.InCode;  }
				else
					{  flags &= ~Flags.InCode;  }
				}
			}



		// Group: Variables
		// __________________________________________________________________________


		/* var: topic
		 * The <Topic> created by this code point, or null if none.
		 */
		protected Topic topic;

		/* var: context
		 * The <ContextString> from the code point forward if <Flags.ContextChanged> is set.
		 */
		protected ContextString context;

		/* var: defaultAccessLevel
		 * The default <AccessLevel> from the code point forward if <Flags.DefaultAccessLevelChanged> is set.
		 */
		protected AccessLevel defaultAccessLevel;

		/* var: classParentLinks
		 * Any class parent links that appear at this code point, or null if none.
		 */
		protected List<Link> classParentLinks;

		/* var: charOffset
		 * The character offset into the source file at which this code point occurs.
		 */
		protected int charOffset;

		/* var: flags
		 */
		protected Flags flags;

		}
	}