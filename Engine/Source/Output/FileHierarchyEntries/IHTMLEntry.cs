/* 
 * Class: GregValure.NaturalDocs.Engine.Output.FileHierarchyEntries.IHTMLEntry
 * ____________________________________________________________________________
 * 
 * An interface for all HTML entries in <HTMLFileHierarchy>.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2011 Greg Valure.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Text;

namespace GregValure.NaturalDocs.Engine.Output.FileHierarchyEntries
	{
	public interface IHTMLEntry
		{

		// Group: Functions
		// __________________________________________________________________________

		/* Function: PrepareJSON
		 * Performs all calculations related to figuring out the JSON tag length.
		 */
		void PrepareJSON (Builders.HTML htmlBuilder);

		/* Function: AppendJSON
		 * Builds the JSON tag and appends it to the passed StringBuilder.  If the tag contains inline members, they 
		 * will be included automatically.
		 */
		void AppendJSON (StringBuilder output);


		// Group: Properties
		// __________________________________________________________________________

		/* Property: JSONTagLength
		 * Returns the length of the JSON tag that would be generated for this entry.  If the entry has members, they
		 * should NOT be included, but any extra markup associated with them should.  For example, the array brackets
		 * and commas for a local folder's members should be included.  This allows another function to get an accurate
		 * count by adding up the tag lengths of the members.
		 */
		int JSONTagLength
			{  get;  }

		}
	}