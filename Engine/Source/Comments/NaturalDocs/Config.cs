/* 
 * Class: CodeClear.NaturalDocs.Engine.Comments.NaturalDocs.Config
 * ____________________________________________________________________________
 * 
 * A class representing a complete configuration for the Natural Docs parser stored in <Parser.txt>.
 * 
 * 
 * Multithreading: Not Thread Safe, Supports Multiple Readers
 * 
 *		This object doesn't have any locking built in, and so it is up to the class managing it to provide thread safety if needed.
 *		However, it does support multiple concurrent readers.  This means it can be used in read-only mode with no locking or
 *		in read/write mode with a ReaderWriterLock.
 *		
 */

// This file is part of Natural Docs, which is Copyright © 2003-2021 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;
using CodeClear.NaturalDocs.Engine.Collections;


namespace CodeClear.NaturalDocs.Engine.Comments.NaturalDocs
	{
	public class Config
		{
		
		// Group: Functions
		// __________________________________________________________________________
		
		
		/* Constructor: Config
		 */
		public Config ()
			{
			startBlockKeywords = new StringSet(KeySettingsForSets);
			endBlockKeywords = new StringSet(KeySettingsForSets);
			seeImageKeywords = new StringSet(KeySettingsForSets);
			atLinkKeywords = new StringSet(KeySettingsForSets);
			urlProtocols = new StringSet(KeySettingsForSets);
			acceptableLinkSuffixes = new StringSet(KeySettingsForSets);

			blockTypes = new StringTable<NaturalDocs.Parser.BlockType>(KeySettingsForTables);
			specialHeadings = new StringTable<NaturalDocs.Parser.HeadingType>(KeySettingsForTables);
			accessLevel = new StringTable<Languages.AccessLevel>(KeySettingsForTables);
		
			pluralConversions = new List<KeyValuePair<string, string>>();
			possessiveConversions = new List<KeyValuePair<string, string>>();
			}



		// Group: Operators
		// __________________________________________________________________________


		/* Function: operator ==
		 * Returns whether the two configurations are exactly equal in all settings.
		 */
		public static bool operator== (Comments.NaturalDocs.Config config1, Comments.NaturalDocs.Config config2)
			{
			if ((object)config1 == null && (object)config2 == null)
				{  return true;  }
			else if ((object)config1 == null || (object)config2 == null)
				{  return false;  }
			else
				{
				if (config1.startBlockKeywords != config2.startBlockKeywords ||
					config1.endBlockKeywords != config2.endBlockKeywords ||
					config1.seeImageKeywords != config2.seeImageKeywords ||
					config1.atLinkKeywords != config2.atLinkKeywords ||
					config1.urlProtocols != config2.urlProtocols ||
					config1.acceptableLinkSuffixes != config2.acceptableLinkSuffixes ||
					config1.blockTypes != config2.blockTypes ||
					config1.specialHeadings != config2.specialHeadings ||
					config1.accessLevel != config2.accessLevel ||
					config1.pluralConversions.Count != config2.pluralConversions.Count ||
					config1.possessiveConversions.Count != config2.possessiveConversions.Count)
					{  return false;  }

				for (int i = 0; i < config1.pluralConversions.Count; i++)
					{
					if (config1.pluralConversions[i].Key != config2.pluralConversions[i].Key ||
						config1.pluralConversions[i].Value != config2.pluralConversions[i].Value)
						{  return false;  }
					}

				for (int i = 0; i < config1.possessiveConversions.Count; i++)
					{
					if (config1.possessiveConversions[i].Key != config2.possessiveConversions[i].Key ||
						config1.possessiveConversions[i].Value != config2.possessiveConversions[i].Value)
						{  return false;  }
					}

				return true;
				}
			}
			
		
		/* Function: operator !=
		 * Returns whether any of the settings of the two configurations are different.
		 */
		public static bool operator!= (Comments.NaturalDocs.Config config1, Comments.NaturalDocs.Config config2)
			{
			return !(config1 == config2);
			}
			
		
		public override bool Equals (object o)
			{
			if (o is Comments.NaturalDocs.Config)
				{  return (this == (Comments.NaturalDocs.Config)o);  }
			else
				{  return false;  }
			}


		public override int GetHashCode ()
			{
			return pluralConversions.Count;
			}



		// Group: Properties
		// __________________________________________________________________________


		/* Property: StartBlockKeywords
		 * The first word for lines like "(start code)".
		 */
		public StringSet StartBlockKeywords
			{
			get
				{  return startBlockKeywords;  }
			}

		/* Property: EndBlockKeywords
		 * 	The first word for lines like "(end code)" or just "(end)".
		 */
		public StringSet EndBlockKeywords
			{
			get
				{  return endBlockKeywords;  }
			}

		/* Property: SeeImageKeywords
		 * The first word for lines like "(see image.jpg)".
		 */
		public StringSet SeeImageKeywords
			{
			get
				{  return seeImageKeywords;  }
			}

		/* Property: AtLinkKeywords
		 * The middle word for lines like "<reference at http://www.website.com>".
		 */
		public StringSet AtLinkKeywords
			{
			get
				{  return atLinkKeywords;  }
			}

		/* Property: URLProtocols
		 * The protocol strings in external URLs like http.
		 */
		public StringSet URLProtocols
			{
			get
				{  return urlProtocols;  }
			}

		/* Property: AcceptableLinkSuffixes
		 * The s after links like "<object>s".
		 */
		public StringSet AcceptableLinkSuffixes
			{
			get
				{  return acceptableLinkSuffixes;  }
			}

		/* Property: BlockTypes
		 * The second word for lines like "(start code)" or the only word for lines like "(code)".
		 */
		public StringTable<NaturalDocs.Parser.BlockType> BlockTypes
			{
			get
				{  return blockTypes;  }
			}
		
		/* Property: SpecialHeadings
		 * Headings that have special behavior associated with them.
		 */
		public StringTable<NaturalDocs.Parser.HeadingType> SpecialHeadings
			{
			get
				{  return specialHeadings;  }
			}
		
		/* Property: AccessLevel
		 * Modifiers that can be placed before a Natural Docs keyword to set the access level if it is not specified
		 * in the code itself.
		 */
		public StringTable<Languages.AccessLevel> AccessLevel
			{
			get
				{  return accessLevel;  }
			}
		
		/* Property: PluralConversions
		 * A series of endings where the words ending with the key can have it replaced by the value to form
		 * a possible singular form.  There may be multiple combinations that can be applied to a word, and 
		 * not all of them will be valid.  "Leaves" converts to "Leave", "Leav", "Leaf", and "Leafe".  All that 
		 * matters however is that the valid form be present in the possibilities.
		 */
		public List<KeyValuePair<string, string>> PluralConversions
			{
			get
				{  return pluralConversions;  }
			}

		/* Property: PossessiveConversions
		 * A series of endings where the words ending with the key can have it replaced by the value to
		 * form a possible non-possessive form.
		 */
		public List<KeyValuePair<string, string>> PossessiveConversions
			{
			get
				{  return possessiveConversions;  }
			}



		// Group: Variables
		// __________________________________________________________________________


		/* var: startBlockKeywords
		 * The first word for lines like "(start code)".
		 */
		protected StringSet startBlockKeywords;

		/* var: endBlockKeywords
		 * 	The first word for lines like "(end code)" or just "(end)".
		 */
		protected StringSet endBlockKeywords;

		/* var: seeImageKeywords
		 * The first word for lines like "(see image.jpg)".
		 */
		protected StringSet seeImageKeywords;

		/* var: atLinkKeywords
		 * The middle word for lines like "<reference at http://www.website.com>".
		 */
		protected StringSet atLinkKeywords;

		/* var: urlProtocols
		 * The protocol strings in external URLs like http.
		 */
		protected StringSet urlProtocols;

		/* var: acceptableLinkSuffixes
		 * The s after links like "<object>s".
		 */
		protected StringSet acceptableLinkSuffixes;

		/* var: blockTypes
		 * The second word for lines like "(start code)" or the only word for lines like "(code)".
		 */
		protected StringTable<NaturalDocs.Parser.BlockType> blockTypes;
		
		/* var: specialHeadings
		 * Headings that have special behavior associated with them.
		 */
		protected StringTable<NaturalDocs.Parser.HeadingType> specialHeadings;
		
		/* var: accessLevel
		 * Modifiers that can be placed before a Natural Docs keyword to set the access level if it is not specified
		 * in the code itself.
		 */
		protected StringTable<Languages.AccessLevel> accessLevel;
		
		/* var: pluralConversions
		 * A series of endings where the words ending with the key can have it replaced by the value to form
		 * a possible singular form.  There may be multiple combinations that can be applied to a word, and 
		 * not all of them will be valid.  "Leaves" converts to "Leave", "Leav", "Leaf", and "Leafe".  All that 
		 * matters however is that the valid form be present in the possibilities.
		 */
		protected List<KeyValuePair<string, string>> pluralConversions;

		/* var: possessiveConversions
		 * A series of endings where the words ending with the key can have it replaced by the value to
		 * form a possible non-possessive form.
		 */
		protected List<KeyValuePair<string, string>> possessiveConversions;



		// Group: Constants
		// __________________________________________________________________________
		
		
		public const KeySettings KeySettingsForSets = KeySettings.IgnoreCase | KeySettings.NormalizeUnicode;
		public const KeySettings KeySettingsForTables = KeySettings.IgnoreCase | KeySettings.NormalizeUnicode;

		}
	}