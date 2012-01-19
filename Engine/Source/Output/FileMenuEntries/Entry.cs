/* 
 * Class: GregValure.NaturalDocs.Engine.Output.FileMenuEntries.Entry
 * ____________________________________________________________________________
 * 
 * A base class for entries in <FileMenu>.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2012 Greg Valure.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;


namespace GregValure.NaturalDocs.Engine.Output.FileMenuEntries
	{
	abstract public class Entry : IComparable<Entry>
		{

		// Group: Functions
		// __________________________________________________________________________

		public Entry ()
			{
			parent = null;
			outputData = null;
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

		/* Property: OutputData
		 * An arbitrary object that can be attached to each entry to aid in generating output.
		 */
		public object OutputData
			{
			get
				{  return outputData;  }
			set
				{  outputData = value;  }
			}

		/* Property: SortString
		 * Returns the string that should be used to sort this entry in a list.
		 */
		public abstract string SortString
			{  get;  }


		// Group: Variables
		// __________________________________________________________________________

		protected object outputData;
		protected Container parent;

		}
	}