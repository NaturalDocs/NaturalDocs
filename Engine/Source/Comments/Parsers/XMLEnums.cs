
// This file is part of Natural Docs, which is Copyright © 2003-2012 Greg Valure.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;


namespace GregValure.NaturalDocs.Engine.Comments.Parsers
	{

	/* enum: XMLElementType
		* 
		* OutOfBounds - The iterator has moved past the end of the string.
		* Text - The iterator is on a text segment.
		* Tag - The iterator is on a tag.
		* EntityChar - The iterator is on an entity char like &lt;.
		* LineBreak - The iterator is on a single line break.
		*/
	public enum XMLElementType : byte
		{  OutOfBounds, Text, Tag, EntityChar, LineBreak  }

	/* enum: XMLTagForm
		* 
		* Opening - An opening tag.
		* Closing - A closing tag.
		* Standalone - A standalone tag.
		*/
	public enum XMLTagForm : byte
		{  Opening, Closing, Standalone  }

	}