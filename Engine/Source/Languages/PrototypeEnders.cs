/* 
 * Class: CodeClear.NaturalDocs.Engine.Languages.PrototypeEnders
 * ____________________________________________________________________________
 * 
 * A simple class to hold information about what can end a prototype.
 * 
 */

// This file is part of Natural Docs, which is Copyright © 2003-2021 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;


namespace CodeClear.NaturalDocs.Engine.Languages
	{
	public class PrototypeEnders
		{

		// Group: Functions
		// __________________________________________________________________________


		/* Constructor: PrototypeEnders
		 */
		public PrototypeEnders ()
			{
			includeLineBreaks = false;
			symbols = null;
			}

		/* Constructor: PrototypeEnders
		 * Creates the object with the passed symbols and line break flag.  The symbols array should not include "\n".  Use the
		 * ender strings constructor to convert arrays with "\n" automatically.
		 */
		public PrototypeEnders (IList<string> symbols, bool includeLineBreaks)
			{
			this.symbols = new List<string>(symbols.Count);
			this.symbols.AddRange(symbols);

			this.includeLineBreaks = includeLineBreaks;
			}

		/* Constructor: PrototypeEnders
		 * Creates the object with the passed ender strings.  If any strings are "\n" it will automatically set <IncludeLineBreaks>
		 * and be excluded from <Symbols>.
		 */
		public PrototypeEnders (IList<string> enderStrings)
			{
			symbols = null;
			includeLineBreaks = false;

			if (enderStrings == null || enderStrings.Count == 0)
				{  return;  }

			int lengthWithoutLineBreaks = 0;

			foreach (string enderString in enderStrings)
				{
				if (enderString != "\\n" && enderString != "\\N")
					{  lengthWithoutLineBreaks++;  }
				}

			if (enderStrings.Count == lengthWithoutLineBreaks)
				{
				symbols = new List<string>(enderStrings.Count);
				symbols.AddRange(enderStrings);
				includeLineBreaks = false;
				}
			else if (lengthWithoutLineBreaks == 0)
				{
				symbols = null;
				includeLineBreaks = true;
				}
			else
				{
				symbols = new List<string>(lengthWithoutLineBreaks);

				foreach (string enderString in enderStrings)
					{
					if (enderString != "\\n" && enderString != "\\N")
						{  symbols.Add(enderString);  }
					}

				includeLineBreaks = true;
				}
			}



		// Group: Operators
		// __________________________________________________________________________
		
		/* Function: operator ==
		 * Returns whether all the prototype enders are equal.
		 */
		public static bool operator == (PrototypeEnders prototypeEnders1, PrototypeEnders prototypeEnders2)
			{
			if ((object)prototypeEnders1 == null && (object)prototypeEnders2 == null)
				{  return true;  }
			else if ((object)prototypeEnders1 == null || (object)prototypeEnders2 == null)
				{  return false;  }
			if (prototypeEnders1.IncludeLineBreaks != prototypeEnders2.IncludeLineBreaks)
				{  return false;  }

			int symbolCount1 = (prototypeEnders1.HasSymbols ? prototypeEnders1.Symbols.Count : 0);
			int symbolCount2 = (prototypeEnders2.HasSymbols ? prototypeEnders2.Symbols.Count : 0);

			if (symbolCount1 != symbolCount2)
				{  return false;  }

			if (symbolCount1 == 0)
				{  return true;  }

			foreach (var symbol1 in prototypeEnders1.Symbols)
				{
				bool hasMatch = false;

				foreach (var symbol2 in prototypeEnders2.Symbols)
					{
					if (symbol1 == symbol2)
						{
						hasMatch = true;
						break;
						}
					}

				if (!hasMatch)
					{  return false;  }
				}

			return true;
			}
			
		/* Function: operator !=
		 * Returns if any of the prototype enders are different.
		 */
		public static bool operator != (PrototypeEnders prototypeEnders1, PrototypeEnders prototypeEnders2)
			{
			return !(prototypeEnders1 == prototypeEnders2);
			}
			
		public override bool Equals (object o)
			{
			if (o is PrototypeEnders)
				{  return (this == (PrototypeEnders)o);  }
			else
				{  return false;  }
			}

		public override int GetHashCode ()
			{
			if (HasSymbols)
				{  return Symbols[0].GetHashCode();  }
			else
				{  return (IncludeLineBreaks ? 1 : 0);  }
			}



		// Group: Properties
		// __________________________________________________________________________

		/* Property: IncludeLineBreaks
		 * Whether line breaks end prototypes.
		 */
		public bool IncludeLineBreaks
			{
			get
				{  return includeLineBreaks;  }
			}

		/* Property: HasSymbols
		 * Whether there are any <Symbols> that end prototypes.  This may be false if only line breaks end prototypes.
		 */
		public bool HasSymbols
			{
			get
				{  return (symbols != null);  }
			}

		/* Property: Symbols
		 * The symbols which end prototypes, or null if none.  Line breaks are not included here; you must check 
		 * <IncludeLineBreaks> instead.
		 */
		public List<string> Symbols
			{
			get
				{  return symbols;  }
			}



		// Group: Variables
		// __________________________________________________________________________

		/* var: includeLineBreaks
		 * Whether line breaks end prototypes.
		 */
		public bool includeLineBreaks;

		/* var: symbols
		 * A list of symbol strings that end prototypes, or null if none.  Line breaks are not included.
		 */
		public List<string> symbols;
		}
	}