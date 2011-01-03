/* 
 * Class: GregValure.NaturalDocs.Engine.Config.InputEntry
 * ____________________________________________________________________________
 * 
 * A base class for <Enties> handling input.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2011 Greg Valure.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;


namespace GregValure.NaturalDocs.Engine.Config
	{
	public class InputEntry : Entry
		{
		
		// Group: Functions
		// __________________________________________________________________________
		
		
		protected InputEntry (Files.InputType newInputType) : base ()
			{
			inputType = newInputType;
			name = null;
			number = 0;
			}

			
		/* Function: GenerateDefaultName
		 * Sets <Name> to a default value based on other properties like the folder name.  All file sources that can have <InputType>
		 * as <Files.InputType.Source> must override this function.
		 */
		virtual public void GenerateDefaultName()
			{
			if (InputType == Files.InputType.Source)
				{  throw new NotImplementedException();  }
			}
		
		
		
		// Group: Properties
		// __________________________________________________________________________
		
		
		/* Property: InputType
		 * The type of input this entry provides.
		 */
		public Files.InputType InputType
			{
			get
				{  return inputType;  }
			}
			
			
		/* Property: Name
		 * The name of the entry if its <InputType> is <Files.InputType.Source>.  Will be null for other types or if it hasn't been set.
		 */
		public string Name
			{
			get
				{  return name;  }
			set
				{
				if (inputType == Files.InputType.Source)
					{  name = value;  }
				else
					{  throw new NotSupportedException();  }
				}
			}
			
		
		/* Property: Number
		 * The number of the input entry, or zero if it hasn't been set.
		 */
		public int Number
			{
			get
				{  return number;  }
			set
				{  number = value;  }
			}
			
			

		// Group: Variables
		// __________________________________________________________________________		
		
		protected Files.InputType inputType;
		protected string name;
		protected int number;
		
		}
	}