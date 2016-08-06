
// This file is part of Natural Docs, which is Copyright © 2003-2016 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;


namespace CodeClear.NaturalDocs.Engine.Comments.Components
	{

	/* enum: JavadocElementType
	 * 
	 * OutOfBounds - The iterator has moved past the end of the string.
	 * Text - The iterator is on a text segment.
	 * HTMLTag - The iterator is on a HTML tag.
	 * HTMLComment - The iterator is on a HTML comment.
	 * JavadocTag - The iterator is on a Javadoc tag.
	 * EntityChar - The iterator is on an entity char like &lt;.
	 * LineBreak - The iterator is on a single line break.
	 * Indent - The iterator is on the indent before the line's content.
	 */
	public enum JavadocElementType : byte
		{  OutOfBounds, Text, HTMLTag, HTMLComment, JavadocTag, EntityChar, LineBreak, Indent  }

	}