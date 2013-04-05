/* 
 * Class: GregValure.NaturalDocs.Engine.Comments.Components.TagStack
 * ____________________________________________________________________________
 * 
 * A class to handle tag systems like HTML and XML that need to be translated to NDMarkup but might not be valid.  This stack
 * tracks tags that are opened and allows you to handle tags that are closed out of order or not closed at all.
 * 
 */

// This file is part of Natural Docs, which is Copyright © 2003-2013 Greg Valure.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;
using System.Text;


namespace GregValure.NaturalDocs.Engine.Comments.Components
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
		 * Adds an opening tag to the stack.  If the tag causes NDMarkup to be added to the output, also pass the NDMarkup that should 
		 * be added when it's closed.
		 */
		public void OpenTag (string nativeTagType, string closingNDMarkup = null)
			{
			if (stack == null)
				{  stack = new List<Entry>();  }

			Entry entry = new Entry();
			entry.NativeTagType = nativeTagType;
			entry.ClosingNDMarkup = closingNDMarkup;
			stack.Add(entry);
			}


		/* Function: CloseTag
		 * Closes a tag that was added to the stack.  If a StringBuilder was passed and closing NDMarkup was included with
		 * <OpenTag()> it will be appended to the output.  Any nested tags will be closed automatically to handle invalid markup 
		 * such as "<i>text<b>text</i>".  If the opening tag doesn't appear on the stack this will have no effect.
		 */
		public void CloseTag (string nativeTagType, StringBuilder output = null)
			{
			int matchIndex = FindTag(nativeTagType);

			if (matchIndex != -1)
				{  CloseTag(matchIndex, output);  }
			}


		/* Function: CloseTag
		 * Closes the tag at the passed index.  If a StringBuilder was passed and closing NDMarkup was included with <OpenTag()>
		 * it will be appended to the output.  Any nested tags will be closed automatically to handle invalid markup such as
		 * "<i>text<b>text</i>".
		 */
		public void CloseTag (int index, StringBuilder output = null)
			{
			if (stack == null || index >= stack.Count)
				{  return;  }
			
			for (int i = stack.Count - 1; i >= index; i--)
				{
				if (stack[i].ClosingNDMarkup != null && output != null)
					{  output.Append(stack[i].ClosingNDMarkup);  }

				stack.RemoveAt(i);
				}
			}


		/* Function: CloseAllTags
		 * Closes any open tags left on the stack and adds their NDMarkup to the output.
		 */
		public void CloseAllTags (StringBuilder output)
			{
			CloseTag(0, output);
			}


		/* Function: FindTag
		 * Returns the index of the last (innermost) matching tag on the stack, or -1 if it isn't present.
		 */
		public int FindTag (string tagType)
			{
			if (stack == null)
				{  return -1;  }

			for (int i = stack.Count - 1; i >= 0; i--)
				{
				if (stack[i].NativeTagType == tagType)
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
		 */
		public bool IsEmpty
			{
			get
				{
				return (stack == null || stack.Count == 0);
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
		 * Struct: GregValure.NaturalDocs.Engine.Comments.Components.TagStack.Entry
		 * ___________________________________________________________________________
		 */
		private struct Entry
			{
			public string NativeTagType;
			public string ClosingNDMarkup;
			}
		}
	}