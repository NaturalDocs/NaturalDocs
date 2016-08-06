/* 
 * Enum: CodeClear.NaturalDocs.Engine.Comments.Type
 * ____________________________________________________________________________
 * 
 * What type of comment it is.  Note that this refers to the comment symbols, not the actual content.
 * 
 * Plain - The comment uses plain comment symbols.
 * Javadoc - The comment uses Javadoc comment symbols.
 * XML - The comment uses XML comment symbols.
 * 
 */

// This file is part of Natural Docs, which is Copyright © 2003-2016 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;


namespace CodeClear.NaturalDocs.Engine.Comments
	{
	public enum Type
		{  Plain, Javadoc, XML  }
	}