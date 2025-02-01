/*
 * Class: CodeClear.NaturalDocs.Engine.Collections.StringToStringTable
 * ____________________________________________________________________________
 *
 * A general lookup table for mapping one string to another.  This is preferable to a Dictionary<string, string> class
 * because
 *
 * - It has a constructor that allows you to initialize it with pairs of strings quickly.
 * - All the reasons <StringTable> is preferable to a Dictionary.
 *
 */

// This file is part of Natural Docs, which is Copyright © 2003-2025 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;


namespace CodeClear.NaturalDocs.Engine.Collections
	{
	public class StringToStringTable : StringTable<string>
		{

		/* Function: StringToStringTable
		 * Creates an empty table.
		 */
		public StringToStringTable (KeySettings keySettings = KeySettings.Literal) : base(keySettings)
			{
			}


		/* Function: StringToStringTable (strings)
		 * Creates a table initialized with the key value pairs in the passed array.
		 */
		public StringToStringTable (KeySettings keySettings, params string[] keyvalues) : base(keySettings)
			{
			if (keyvalues.Length % 2 != 0)
				{  throw new Exceptions.ArrayDidntHaveEvenLength("keyvalues");  }

			int index = 0;
			while (index < keyvalues.Length)
				{
				string key = keyvalues[index];
				string value = keyvalues[index + 1];

				Add(key, value);

				index += 2;
				}
			}

		}
	}
