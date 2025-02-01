/*
 * Class: CodeClear.NaturalDocs.Engine.Collections.TagStack
 * ____________________________________________________________________________
 *
 * A class to handle tag systems like HTML and XML that might not be valid.  This stack tracks tags that are opened and
 * allows you to handle tags that are closed out of order or not closed at all.
 *
 * The class also supports attaching text to each opened tag which can be appended automatically when that tag is
 * closed.  This aids in converting from one tag system to another and keeping the output valid even if the input is not.
 *
 */

// This file is part of Natural Docs, which is Copyright © 2003-2025 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;
using System.Text;


namespace CodeClear.NaturalDocs.Engine.Collections
	{
	public class TagStack
		{

		// Group: Functions
		// __________________________________________________________________________


		public TagStack ()
			{
			stack = null;
			}


		/* Function: OpenTag
		 * Adds an opening tag to the stack.  If you are using the stack to convert between two tag systems, you can also
		 * include text to be automatically appended when the tag closes.
		 */
		public void OpenTag (string tag, string appendOnClose = null)
			{
			if (stack == null)
				{  stack = new List<Entry>();  }

			Entry entry = new Entry();
			entry.Tag = tag;
			entry.AppendOnClose = appendOnClose;
			stack.Add(entry);
			}


		/* Function: CloseTag
		 * Closes a tag that was added to the stack.  If a StringBuilder is passed, the appendOnClose text for every tag closed
		 * will be appended to it in order.  Any nested tags will be closed automatically to handle invalid markup such as
		 * "<i>text<b>text</i>".  If the passed tag doesn't appear on the stack this will have no effect.
		 */
		public void CloseTag (string tag, StringBuilder appendClosedTagsTo = null)
			{
			int matchIndex = FindTag(tag);

			if (matchIndex != -1)
				{  CloseTag(matchIndex, appendClosedTagsTo);  }
			}


		/* Function: CloseTag
		 * Closes the tag at the passed index.  If a StringBuilder is passed, the appendOnClose text for every tag closed will be
		 * appended to it in order.  Any nested tags will be closed automatically to handle invalid markup such as
		 * "<i>text<b>text</i>".
		 */
		public void CloseTag (int index, StringBuilder appendClosedTagsTo = null)
			{
			if (stack == null || index >= stack.Count)
				{  return;  }

			for (int i = stack.Count - 1; i >= index; i--)
				{
				if (stack[i].AppendOnClose != null && appendClosedTagsTo != null)
					{  appendClosedTagsTo.Append(stack[i].AppendOnClose);  }

				stack.RemoveAt(i);
				}
			}


		/* Function: CloseAllTags
		 * Closes any open tags left on the stack.  If a StringBuilder is passed, the appendOnClose text for every tag closed will be
		 * appended to it in order.
		 */
		public void CloseAllTags (StringBuilder appendClosedTagsTo = null)
			{
			CloseTag(0, appendClosedTagsTo);
			}


		/* Function: FindTag
		 * Returns the index of the last (innermost) matching tag on the stack, or -1 if it isn't present.
		 */
		public int FindTag (string tag)
			{
			if (stack == null)
				{  return -1;  }

			for (int i = stack.Count - 1; i >= 0; i--)
				{
				if (stack[i].Tag == tag)
					{  return i;  }
				}

			return -1;
			}



		// Group: Properties
		// __________________________________________________________________________


		/* Property: Count
		 * The number of open tags on the stack.
		 */
		public int Count
			{
			get
				{
				if (stack == null)
					{  return 0;  }
				else
					{  return stack.Count;  }
				}
			}


		/* Property: IsEmpty
		 * Whether there are any open tags on the stack.
		 */
		public bool IsEmpty
			{
			get
				{
				return (stack == null || stack.Count == 0);
				}
			}


		/* Operator: this
		 * An index operator to retrieve individual tags in the stack.
		 */
		public string this [int index]
			{
			get
				{
				if (stack == null || index >= stack.Count || index < 0)
					{  throw new IndexOutOfRangeException();  }
				else
					{  return stack[index].Tag;  }
				}
			}



		// Group: Variables
		// __________________________________________________________________________


		/* var: stack
		 * The tag stack implemented as a list since we need to be able to look through the members for closing tags.
		 */
		private List<Entry> stack;



		/* ___________________________________________________________________________
		 *
		 * Struct: CodeClear.NaturalDocs.Engine.Comments.Components.TagStack.Entry
		 * ___________________________________________________________________________
		 */
		private struct Entry
			{
			public string Tag;
			public string AppendOnClose;
			}
		}
	}
