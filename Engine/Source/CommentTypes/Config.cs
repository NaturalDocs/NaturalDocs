/*
 * Class: CodeClear.NaturalDocs.Engine.CommentTypes.Config
 * ____________________________________________________________________________
 *
 * A class representing a complete configuration after all <Comments.txt> values have been combined.
 *
 *
 * Multithreading: Not Thread Safe, Supports Multiple Readers
 *
 *		This object doesn't have any locking built in, and so it is up to the class managing it to provide thread safety if needed.
 *		However, it does support multiple concurrent readers.  This means it can be used in read-only mode with no locking or
 *		in read/write mode with a ReaderWriterLock.
 *
 */

// This file is part of Natural Docs, which is Copyright © 2003-2025 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;
using CodeClear.NaturalDocs.Engine.Collections;


namespace CodeClear.NaturalDocs.Engine.CommentTypes
	{
	public class Config
		{

		// Group: Functions
		// __________________________________________________________________________


		/* Constructor: Config
		 */
		public Config ()
			{
			commentTypes = new IDObjects.Manager<CommentType>(KeySettingsForCommentTypes, false);
			tags = new IDObjects.Manager<Tag>(KeySettingsForTags, false);

			singleDefinitionKeywords = new StringTable<KeywordDefinition>(KeySettingsForKeywords);
			multiDefinitionKeywords = new StringTable<List<KeywordDefinition>>(KeySettingsForKeywords);
			}



		// Group: Information Functions
		// __________________________________________________________________________


		/* Function: KeywordDefinition
		 * Returns the <KeywordDefinition> which matches the passed name and language ID, or null if none.  If language ID is
		 * zero it will only return language agnostic definitions, whereas if it has a value it will also search the language-specific
		 * definitions and favor those if there are both.
		 */
		public KeywordDefinition KeywordDefinition (string keyword, int languageID)
			{
			KeywordDefinition singleDefinition = singleDefinitionKeywords[keyword];

			if (singleDefinition != null)
				{
				if (singleDefinition.IsLanguageAgnostic || singleDefinition.LanguageID == languageID)
					{  return singleDefinition;  }
				else
					{  return null;  }
				}

			List<KeywordDefinition> definitionList = multiDefinitionKeywords[keyword];

			if (definitionList != null)
				{
				KeywordDefinition agnosticDefinition = null;

				foreach (var definition in definitionList)
					{
					if (definition.IsLanguageSpecific)
						{
						if (languageID == definition.LanguageID)
							{  return definition;  }
						}
					else // definition.IsLanguageAgnostic
						{
						if (languageID == 0)
							{  return definition;  }
						else
							{  agnosticDefinition = definition;  }
						}
					}

				return agnosticDefinition; // will be null if one wasn't found, which is what we want
				}

			return null;
			}


		/* Function: CommentTypeFromKeyword
		 * Returns the <CommentType> associated with the passed keyword and language, or null if none.  The language ID
		 * should be set to the language the keyword appears in, though it can also be zero to only return comment types from
		 * language agnostic keywords.
		 */
		public CommentType CommentTypeFromKeyword (string keyword, int languageID)
			{
			var keywordDefinition = KeywordDefinition(keyword, languageID);

			if (keywordDefinition != null)
				{  return commentTypes[keywordDefinition.CommentTypeID];  }
			else
				{  return null;  }
			}


		/* Function: CommentTypeFromKeyword
		 * Returns the <CommentType> associated with the passed keyword and language, or null if none.  Also returns whether
		 * it was singular or plural.  The language ID should be set to the language the keyword appears in, though it can also be
		 * zero to only return comment types from language agnostic keywords.
		 */
		public CommentType CommentTypeFromKeyword (string keyword, int languageID, out bool isPlural)
			{
			var keywordDefinition = KeywordDefinition(keyword, languageID);

			if (keywordDefinition != null)
				{
				isPlural = keywordDefinition.Plural;
				return commentTypes[keywordDefinition.CommentTypeID];
				}
			else
				{
				isPlural = false;
				return null;
				}
			}

		/* Function: CommentTypeFromName
		 * Returns the <CommentType> associated with the passed name, or null if none.
		 */
		public CommentType CommentTypeFromName (string name)
			{
			return commentTypes[name];
			}

		/* Function: CommentTypeFromID
		 * Returns the <CommentType> associated with the passed ID, or null if none.
		 */
		public CommentType CommentTypeFromID (int commentTypeID)
			{
			return commentTypes[commentTypeID];
			}

		/* Function: UsedCommentTypeIDs
		 * Returns a set of all the used comment type IDs.
		 */
		public IDObjects.NumberSet UsedCommentTypeIDs ()
			{
			return commentTypes.GetUsedIDs();
			}

		/* Function: TagFromName
		 * Returns the <Tag> associated with the passed name, or null if none.
		 */
		public Tag TagFromName (string name)
			{
			return tags[name];
			}

		/* Function: TagFromID
		 * Returns the <Tag> associated with the passed ID, or null if none.
		 */
		public Tag TagFromID (int id)
			{
			return tags[id];
			}

		/* Function: UsedTagIDs
		 * Returns a set of all the used tag IDs.
		 */
		public IDObjects.NumberSet UsedTagIDs ()
			{
			return tags.GetUsedIDs();
			}



		// Group: Action Functions
		// __________________________________________________________________________


		/* Function: AddCommentType
		 */
		public void AddCommentType (CommentType commentType)
			{
			commentTypes.Add(commentType);
			}

		/* Function: AddTag
		 */
		public void AddTag (Tag tag)
			{
			tags.Add(tag);
			}

		/* Function: AddKeywordDefinition
		 * Adds a keyword definition to the configuration.  If a definition already exists for the keyword but with a different
		 * language ID, both definitions will be stored.  If a definition already exists with the same language ID, this one will
		 * overwrite the previous one.
		 */
		public void AddKeywordDefinition (KeywordDefinition keywordDefinition)
			{
			// First check the multi-definition list
			List<KeywordDefinition> multiDefinitionList = multiDefinitionKeywords[keywordDefinition.Keyword];

			if (multiDefinitionList != null)
				{
				// If it's already in the list with this language ID, overwrite it
				for (int i = 0; i < multiDefinitionList.Count; i++)
					{
					if (multiDefinitionList[i].LanguageID == keywordDefinition.LanguageID)
						{
						multiDefinitionList[i] = keywordDefinition;
						return;
						}
					}

				// If it's not, add it
				multiDefinitionList.Add(keywordDefinition);
				return;
				}

			// Now check the single definition list
			KeywordDefinition existingDefinition = singleDefinitionKeywords[keywordDefinition.Keyword];

			if (existingDefinition != null)
				{
				// If it has the same language ID, overwrite it
				if (existingDefinition.LanguageID == keywordDefinition.LanguageID)
					{
					singleDefinitionKeywords[keywordDefinition.Keyword] = keywordDefinition;
					return;
					}

				// Otherwise, transfer both definitions to the multi-definition list
				multiDefinitionList = new List<KeywordDefinition>(2);
				multiDefinitionList.Add(existingDefinition);
				multiDefinitionList.Add(keywordDefinition);
				multiDefinitionKeywords.Add(keywordDefinition.Keyword, multiDefinitionList);

				singleDefinitionKeywords.Remove(keywordDefinition.Keyword);
				return;
				}

			// Otherwise it wasn't defined at all, so add it to the single definition list
			singleDefinitionKeywords.Add(keywordDefinition.Keyword, keywordDefinition);
			}



		// Group: Operators
		// __________________________________________________________________________


		/* Function: operator ==
		 * Returns whether the two configurations are exactly equal in all settings.
		 */
		public static bool operator== (Config config1, Config config2)
			{
			if ((object)config1 == null && (object)config2 == null)
				{  return true;  }
			else if ((object)config1 == null || (object)config2 == null)
				{  return false;  }


			// Comparing the counts is quick, so do that first

			if (config1.commentTypes.Count != config2.commentTypes.Count ||
				config1.tags.Count != config2.tags.Count ||
				config1.singleDefinitionKeywords.Count != config2.singleDefinitionKeywords.Count ||
				config1.multiDefinitionKeywords.Count != config2.multiDefinitionKeywords.Count)
				{  return false;  }


			// Welp, now we have to do a thorough comparison, though it's easier now that we know both sides have the
			// same number of items in each property.  That means we can iterate through each item in config1 to see if
			// it has a match in config2 and treat them as equal if they do.  We don't have to worry about there being an
			// extra item in config2 that this approach would miss.

			foreach (var commentType1 in config1.commentTypes)
				{
				var commentType2 = config2.commentTypes[commentType1.ID];

				if (commentType1 != commentType2)
					{  return false;  }
				}

			foreach (var tag1 in config1.tags)
				{
				var tag2 = config2.tags[tag1.ID];

				if (tag1 != tag2)
					{  return false;  }
				}

			foreach (var keywordDefinition1 in config1.singleDefinitionKeywords.Values)
				{
				var keywordDefinition2 = config2.singleDefinitionKeywords[keywordDefinition1.Keyword];

				if (keywordDefinition1 != keywordDefinition2)
					{  return false;  }
				}

			foreach (var keywordDefinitionListPair1 in config1.multiDefinitionKeywords)
				{
				var keyword1 = keywordDefinitionListPair1.Key;
				var keywordDefinitionList1 = keywordDefinitionListPair1.Value;
				var keywordDefinitionList2 = config2.multiDefinitionKeywords[keyword1];

				if (keywordDefinitionList2 == null ||
					keywordDefinitionList2.Count != keywordDefinitionList1.Count)
					{  return false;  }

				foreach (var keywordDefinition1 in keywordDefinitionList1)
					{
					bool foundMatch = false;

					foreach (var keywordDefinition2 in keywordDefinitionList2)
						{
						if (keywordDefinition1 == keywordDefinition2)
							{
							foundMatch = true;
							break;
							}
						}

					if (!foundMatch)
						{  return false;  }
					}
				}

			return true;
			}


		/* Function: operator !=
		 * Returns whether any of the settings of the two configurations are different.
		 */
		public static bool operator!= (Config config1, Config config2)
			{
			return !(config1 == config2);
			}


		public override bool Equals (object o)
			{
			if (o is Config)
				{  return (this == (Config)o);  }
			else
				{  return false;  }
			}


		public override int GetHashCode ()
			{
			return ( (commentTypes.Count) ^
						(tags.Count << 8) ^
						(singleDefinitionKeywords.Count << 16) ^
						(multiDefinitionKeywords.Count << 24) );
			}



		// Group: Enumerable Properties
		// __________________________________________________________________________


		/* Property: CommentTypes
		 * Returns an enumerator that returns every <CommentType> defined.  This property is usable with foreach.
		 */
		public IEnumerable<CommentType> CommentTypes
			{
			get
				{  return commentTypes;  }
			}


		/* Property: Tags
		 * Returns an enumerator that returns every <Tag> defined.  This property is usable with foreach.
		 */
		public IEnumerable<Tag> Tags
			{
			get
				{  return tags;  }
			}


		/* Property: KeywordDefinitions
		 * Returns an enumerator that returns every <KeywordDefinition> defined.  This property is usable with foreach.
		 */
		public IEnumerable<KeywordDefinition> KeywordDefinitions
			{
			get
				{
				foreach (var definition in singleDefinitionKeywords.Values)
					{  yield return definition;  }

				foreach (var definitionList in multiDefinitionKeywords.Values)
					{
					foreach (var definition in definitionList)
						{  yield return definition;  }
					}
				}
			}



		// Group: Variables
		// __________________________________________________________________________


		/* var: commentTypes
		 * Manages all the <CommentTypes> by their case-insensitive name or ID number.
		 */
		protected IDObjects.Manager<CommentType> commentTypes;

		/* var: tags
		 * Manages all the <Tags> by their case-insensitive name or ID number.
		 */
		protected IDObjects.Manager<Tag> tags;

		/* var: singleDefinitionKeywords
		 * Manages all the keywords which only have a single <KeywordDefinition> by their case-insensitive names.
		 */
		protected StringTable<KeywordDefinition> singleDefinitionKeywords;

		/* var: multiDefinitionKeywords
		 * Manages all the keywords which have multiple <KeywordDefinitions> by their case-insensitive names.
		 */
		protected StringTable<List<KeywordDefinition>> multiDefinitionKeywords;



		// Group: Constants
		// __________________________________________________________________________


		public const KeySettings KeySettingsForKeywords = KeySettings.IgnoreCase | KeySettings.NormalizeUnicode;
		public const KeySettings KeySettingsForCommentTypes = KeySettings.IgnoreCase | KeySettings.NormalizeUnicode;
		public const KeySettings KeySettingsForTags = KeySettings.IgnoreCase | KeySettings.NormalizeUnicode;

		}
	}
