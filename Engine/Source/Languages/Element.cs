/*
 * Class: CodeClear.NaturalDocs.Engine.Languages.Element
 * ____________________________________________________________________________
 *
 * A class to hold an element found when parsing the code or comments.  Every <Topic> will have a corresponding
 * Element, but not every Element will contain a <Topic> as some are just needed to track changes to scope or
 * access levels and will not appear in the output.  These properties are set on <ParentElement> objects.
 *
 */

// This file is part of Natural Docs, which is Copyright © 2003-2025 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using CodeClear.NaturalDocs.Engine.Links;
using CodeClear.NaturalDocs.Engine.Tokenization;
using CodeClear.NaturalDocs.Engine.Topics;


namespace CodeClear.NaturalDocs.Engine.Languages
	{

	[DebuggerDisplay("{DebuggerDisplay,nq}")]
	public class Element
		{

		// Group: Types
		// __________________________________________________________________________


		/* enum: Flags
		 *
		 * InComments - The element comes from the comments.
		 * InCode - The element comes from the code via full language support.
		 *
		 * It's possible for a element to have both <InComments> and <InCode> set after the results are merged.
		 */
		[Flags]
		public enum Flags : byte
			{
			InComments = 0x01,
			InCode = 0x02
			}



		// Group: Functions
		// __________________________________________________________________________


		/* Constructor: Element
		 */
		public Element (TokenIterator iterator, Flags flags) : this (iterator.LineNumber, iterator.CharNumber, flags)
			{  }


		/* Constructor: Element
		 */
		public Element (LineIterator lineIterator, Flags flags) : this (lineIterator.LineNumber, 1, flags)
			{  }


		/* Constructor: Element
		 */
		public Element (int lineNumber, int charNumber, Flags flags)
			{
			topic = null;
			classParentLinks = null;

			filePosition = new FilePosition(lineNumber, charNumber);
			this.flags = flags;
			}



		// Group: Properties
		// __________________________________________________________________________


		/* Property: Topic
		 * The <Topic> created by this element, or null if none.
		 */
		public Topic Topic
			{
			get
				{  return topic;  }
			set
				{  topic = value;  }
			}


		/* Property: ClassParentLinks
		 * A list of class parent <Links> created by this element, or null if none.
		 */
		public List<Link> ClassParentLinks
			{
			get
				{  return classParentLinks;  }
			set
				{  classParentLinks = value;  }
			}


		/* Property: LineNumber
		 * The line number this element appears at.  The first line number is one, not zero.
		 */
		public int LineNumber
			{
			get
				{  return filePosition.LineNumber;  }
			set
				{  filePosition.LineNumber = value;  }
			}


		/* Property: CharNumber
		 * The character number this element appears at.  The first character number is one, not zero, and is relative
		 * to the line, not the file.
		 */
		public int CharNumber
			{
			get
				{  return filePosition.CharNumber;  }
			set
				{  filePosition.CharNumber = value;  }
			}


		/* Property: FilePosition
		 * The file position the element appears at.
		 */
		public FilePosition FilePosition
			{
			get
				{  return filePosition;  }
			set
				{  filePosition = value;  }
			}


		/* Property: InComments
		 * Whether this element appears in the comments.  It is possible for both InComments and <InCode> to be set.
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
		 * Whether this element appears in the code via full language support parsing.  It is possible for both InCode and
		 * <InComments> to be set.
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


		/* Property: DebuggerDisplay
		 * Shows information about the element when debugging Natural Docs.
		 */
		internal string DebuggerDisplay
			{
			get
				{
				StringBuilder debuggerDisplay = new StringBuilder();

				if (this is ParentElement)
					{
					if ((this as ParentElement).IsRootElement)
						{  debuggerDisplay.Append("Root ");  }
					else
						{  debuggerDisplay.Append("Parent ");  }
					}

				if (InCode && InComments)
					{  debuggerDisplay.Append("Code+Comment ");  }
				else if (InCode)
					{  debuggerDisplay.Append("Code ");  }
				else if (InComments)
					{  debuggerDisplay.Append("Comment ");  }

				debuggerDisplay.Append("Element - ");

				if (topic == null)
					{  debuggerDisplay.Append("(no topic)");  }
				else
					{  debuggerDisplay.Append(topic.DebuggerDisplay);  }

				return debuggerDisplay.ToString();
				}
			}



		// Group: Variables
		// __________________________________________________________________________


		/* var: filePosition
		 * The file position the element appears at.
		 */
		protected FilePosition filePosition;

		/* var: flags
		 */
		protected Flags flags;

		/* var: topic
		 * The <Topic> created by this element, or null if none.
		 */
		protected Topic topic;

		/* var: classParentLinks
		 * A list of class parent <Links> created by this element, or null if none.
		 */
		protected List<Link> classParentLinks;

		}
	}
