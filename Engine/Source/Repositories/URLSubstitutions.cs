/*
 * Class: CodeClear.NaturalDocs.Engine.Repositories.URLSubstitutions
 * ____________________________________________________________________________
 *
 * Constants for dealing with substitution points in repository source URL templates.
 *
 */

// This file is part of Natural Docs, which is Copyright © 2003-2026 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;


namespace CodeClear.NaturalDocs.Engine.Repositories
	{
	public static class URLSubstitutions
		{

		/* Constant: FilePath
		 * A character representing the place in a repository source URL template which should be replaced with the
		 * source file's relative path.
		 */
		public const char FilePath = '\x1F';

		/* Constant: FilePathString
		 * <FilePath> as a string to avoid repeated runtime conversions for substitution functions that require it as a
		 * string.
		 */
		public const string FilePathString = "\x1F";

		/* Constant: LineNumber
		 * A character representing the place in a repository source URL template which should be replaced with the
		 * source file line number.
		 */
		public const char LineNumber = '\x1E';

		/* Constant: LineNumberString
		 * <LineNumber> as a string to avoid repeated runtime conversions for substitution functions that require it
		 * as a string.
		 */
		public const string LineNumberString = "\x1E";
		}
	}
