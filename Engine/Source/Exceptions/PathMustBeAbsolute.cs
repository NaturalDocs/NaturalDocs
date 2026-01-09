/*
 * Class: CodeClear.NaturalDocs.Engine.Exceptions.PathMustBeAbsolute
 * ____________________________________________________________________________
 *
 * An exception thrown when a relative path is used somewhere an absolute one is required.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2026 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;


namespace CodeClear.NaturalDocs.Engine.Exceptions
	{
	public class PathMustBeAbsolute : Exception
		{
		public PathMustBeAbsolute (string path)
			: base ("Tried to use relative path \"" + path + "\" where an absolute one was required.")
			{
			}
		}
	}
