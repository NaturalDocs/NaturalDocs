/* 
 * Class: GregValure.NaturalDocs.Engine.Output.Style
 * ____________________________________________________________________________
 * 
 * The base class for all output styles.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2013 Greg Valure.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;


namespace GregValure.NaturalDocs.Engine.Output
	{
	abstract public class Style
		{

		/* Function: Contains
		 * Returns whether this style contains the passed file.
		 */
		abstract public bool Contains (Path file);

		/* Function: MakeRelative
		 * Converts the passed filename to one relative to this style.  If this style doesn't contain the file, it will return null.
		 */
		abstract public Path MakeRelative (Path file);

		/* Function: IsSameFundamentalStyle
		 * Returns whether this style is fundamentally the same as the passed one, meaning any identifying properties will be the
		 * same (i.e. both referencing the same style folder) but secondary properties may be different.
		 */
		public virtual bool IsSameFundamentalStyle (Style other)
			{
			return false;
			}

		}
	}