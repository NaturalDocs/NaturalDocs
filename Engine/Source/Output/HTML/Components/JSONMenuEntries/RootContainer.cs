/* 
 * Class: CodeClear.NaturalDocs.Engine.Output.HTML.Components.JSONMenuEntries.RootContainer
 * ____________________________________________________________________________
 * 
 * A base class for root container entries in <JSONMenu>.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2021 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;
using CodeClear.NaturalDocs.Engine.IDObjects;


namespace CodeClear.NaturalDocs.Engine.Output.HTML.Components.JSONMenuEntries
	{
	public class RootContainer : Container
		{

		// Group: Functions
		// __________________________________________________________________________

		
		/* Function: RootContainer
		 */
		public RootContainer (MenuEntries.Container menuContainer) : base (menuContainer)
			{
			dataFileIdentifier = null;
			usedDataFileNumbers = null;
			}



		// Group: Properties
		// __________________________________________________________________________


		/* Property: DataFileIdentifier
		 * The identifier for menu data files, such as "files" and "classes", which gets turned into "files.js" and "classes6.js".  This
		 * will be null if data files haven't been assigned yet.
		 */
		public string DataFileIdentifier
			{
			get
				{  return dataFileIdentifier;  }
			set
				{  dataFileIdentifier = value;  }
			}

		/* Property: UsedDataFileNumbers
		 * A <NumberSet> of all the numbers used for menu data files, or null if data files haven't been assigned yet.  So "files.js",
		 * "files2.js", and "files3.js" will be {1-3}.
		 */
		public NumberSet UsedDataFileNumbers
			{
			get
				{  return usedDataFileNumbers;  }
			set
				{  usedDataFileNumbers = value;  }
			}



		// Group: Variables
		// __________________________________________________________________________


		/* var: dataFileIdentifier
		 * The identifier for menu data files, such as "files" and "classes", which gets turned into "files.js" and "classes6.js".  This
		 * will be null if data files haven't been assigned yet.
		 */
		protected string dataFileIdentifier;

		/* var: usedDataFileNumbers
		 * A <NumberSet> of all the numbers used for menu data files, or null if data files haven't been assigned yet.  So "files.js",
		 * "files2.js", and "files3.js" will be {1-3}.
		 */
		protected NumberSet usedDataFileNumbers;

		}
	}