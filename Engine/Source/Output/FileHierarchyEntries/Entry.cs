/* 
 * Class: GregValure.NaturalDocs.Engine.Output.FileHierarchyEntries.Entry
 * ____________________________________________________________________________
 * 
 * A base class for entries in <FileHierarchy>.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2011 Greg Valure.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;


namespace GregValure.NaturalDocs.Engine.Output.FileHierarchyEntries
	{
	abstract public class Entry : IComparable<Entry>
		{

		// Group: Functions
		// __________________________________________________________________________

		public Entry ()
			{
			parent = null;
			output = null;
			}

		public int CompareTo (Entry other)
			{
			return string.Compare(SortString, other.SortString, true);
			}


		// Group: Properties
		// __________________________________________________________________________

		/* Property: Parent
		 * The parent of this entry, or null if none.
		 */
		public Container Parent
			{
			get
				{  return parent;  }
			set
				{  parent = value;  }
			}

		/* Property: Output
		 * A string for use by the code using the hierarchy.  Can be used to build the output entry by entry, regardless
		 * of the format.
		 */
		public string Output
			{
			get
				{  return output;  }
			set
				{  output = value;  }
			}

		/* Property: SortString
		 * Returns the string that should be used to sort this entry in a list.
		 */
		public abstract string SortString
			{  get;  }


		// Group: Variables
		// __________________________________________________________________________

		protected string output;
		protected Container parent;

		}
	}