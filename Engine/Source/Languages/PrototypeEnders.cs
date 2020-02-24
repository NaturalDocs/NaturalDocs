/* 
 * Class: CodeClear.NaturalDocs.Engine.Languages.PrototypeEnders
 * ____________________________________________________________________________
 * 
 * A simple class to hold information about what can end a prototype.
 * 
 */

// This file is part of Natural Docs, which is Copyright © 2003-2020 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;


namespace CodeClear.NaturalDocs.Engine.Languages
	{

	public class PrototypeEnders
		{
		/* Constructor: PrototypeEnders
		 */
		public PrototypeEnders ()
			{
			IncludeLineBreaks = false;
			Symbols = null;
			}

		/* Constructor: PrototypeEnders
		 * Creates the object with the passed symbols and line break flag.  The symbols array should not include "\n".  Use the
		 * ender strings constructor to convert arrays with "\n" automatically.
		 */
		public PrototypeEnders (string[] symbols, bool includeLineBreaks)
			{
			Symbols = symbols;
			IncludeLineBreaks = includeLineBreaks;
			}

		/* Constructor: PrototypeEnders
		 * Creates the object with the passed ender strings, automatically converting them to <Symbols> and the 
		 * <IncludeLineBreaks> flag.
		 */
		public PrototypeEnders (string[] enderStrings)
			{
			if (enderStrings == null || enderStrings.Length == 0)
				{
				Symbols = null;
				IncludeLineBreaks = false;
				return;
				}

			int lengthWithoutLineBreaks = 0;

			foreach (string enderString in enderStrings)
				{
				if (enderString != "\\n" && enderString != "\\N")
					{  lengthWithoutLineBreaks++;  }
				}

			if (enderStrings.Length == lengthWithoutLineBreaks)
				{
				Symbols = enderStrings;
				IncludeLineBreaks = false;
				}
			else if (lengthWithoutLineBreaks == 0)
				{
				Symbols = null;
				IncludeLineBreaks = true;
				}
			else
				{
				Symbols = new string[lengthWithoutLineBreaks];
				int symbolIndex = 0;

				foreach (string enderString in enderStrings)
					{
					if (enderString != "\\n" && enderString != "\\N")
						{
						Symbols[symbolIndex] = enderString;
						symbolIndex++;
						}
					}

				IncludeLineBreaks = true;
				}
			}

		/* Property: IncludeLineBreaks
		 * Whether line breaks end prototypes.
		 */
		public bool IncludeLineBreaks;

		/* Property: Symbols
		 * An array of symbol strings that end prototypes, or null if none.  Line breaks are not included.
		 */
		public string[] Symbols;
		}
	}