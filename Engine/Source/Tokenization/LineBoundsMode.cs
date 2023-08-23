/*
 * Enum: CodeClear.NaturalDocs.Engine.Tokenization.LineBoundsMode
 * ____________________________________________________________________________
 *
 * An option that determines what should be considered the beginning and end of a line.
 *
 * Everything - The entire line, including all whitespace and the line break.
 * ExcludeWhitespace - The line content, excluding leading and trailing whitespace and line breaks.
 * CommentContent - The comment content, which is the line minus leading and trailing whitespace,
 *										  line breaks, and tokens marked <CommentParsingType.CommentSymbol> and
 *										  <CommentParsingType.CommentDecoration>.
 *
 */

// This file is part of Natural Docs, which is Copyright © 2003-2023 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;


namespace CodeClear.NaturalDocs.Engine.Tokenization
	{
	public enum LineBoundsMode
		{  Everything, ExcludeWhitespace, CommentContent  };
	}
