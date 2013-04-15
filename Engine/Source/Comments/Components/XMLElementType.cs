
// This file is part of Natural Docs, which is Copyright © 2003-2013 Greg Valure.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;


namespace GregValure.NaturalDocs.Engine.Comments.Components
	{

	/* enum: XMLElementType
	 * 
	 * OutOfBounds - The iterator has moved past the end of the string.
	 * Text - The iterator is on a text segment.
	 * Tag - The iterator is on a tag.
	 * EntityChar - The iterator is on an entity char like &lt;.
	 * LineBreak - The iterator is on a single line break.
	 * Indent - The iterator is on the indent before the line's content.
	 */
	public enum XMLElementType : byte
		{  OutOfBounds, Text, Tag, EntityChar, LineBreak, Indent  }

	}