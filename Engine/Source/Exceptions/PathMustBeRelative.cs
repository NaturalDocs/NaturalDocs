/* 
 * Class: CodeClear.NaturalDocs.Engine.Exceptions.PathMustBeRelative
 * ____________________________________________________________________________
 * 
 * An exception thrown when an absolute path is used somewhere a relative one is required.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2021 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;


namespace CodeClear.NaturalDocs.Engine.Exceptions
	{
	public class PathMustBeRelative : Exception
		{
		public PathMustBeRelative (string path)
			: base ("Tried to use absolute path \"" + path + "\" where a relative one was required.")
			{
			}
		}
	}