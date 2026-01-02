/*
 * Class: CodeClear.NaturalDocs.Engine.Config.RepositorySubstitutions
 * ____________________________________________________________________________
 *
 * Constants for dealing with substitution points in repository source URL templates.
 *
 */

// This file is part of Natural Docs, which is Copyright © 2003-2025 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;


namespace CodeClear.NaturalDocs.Engine.Config
	{
	public static class RepositorySubstitutions
		{

		/* Constant: FilePath
		 * A string representing the place in a repository source URL template which should be replaced with the
		 * source file's relative path.  It's a single character but almost all substitution functions require strings so
		 * the constant is a string as well.
		 */
		public const string FilePath = "\x1F";

		/* Constant: LineNumber
		 * A string representing the place in a repository source URL template which should be replaced with the
		 * source file line number.  It's a single character but almost all substitution functions require strings so the
		 * constant is a string as well.
		 */
		public const string LineNumber = "\x1E";

		/* Constant: Branch
		 * A string representing the place in a repository source URL template which should be replaced with the
		 * branch name.  It's a single character but almost all substitution functions require strings so the constant
		 * is a string as well.
		 */
		public const string Branch = "\x1D";

		/* Constant: ProjectURL
		 * A string representing the place in a repository source URL template which should be replaced with the
		 * source file's repository project URL.  It's a single character but almost all substitution functions require
		 * strings so the constant is a string as well.
		 */
		public const string ProjectURL = "\x1C";

		}
	}
