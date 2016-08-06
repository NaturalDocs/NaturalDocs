/* 
 * Class: CodeClear.NaturalDocs.Engine.TopicTypes.Topics_nd
 * ____________________________________________________________________________
 * 
 * A class to handle loading and saving <Topics.nd>.
 * 
 * 
 * Threading: Not Thread Safe
 * 
 *		The parser object may be reused, but multiple threads cannot use it at the same time.
 *		
 * 
 * File: Topics.nd
 * 
 *		A binary file which stores the combined results of the two versions of <Topics.txt> as of the last run, as well as storing
 *		the IDs of each type so they maintain their consistency between runs.
 *		
 *		Format:
 *		
 *			> [[Binary Header]]
 *		
 *			The file starts with the standard binary file header as managed by <BinaryFile>.
 *			
 *			> [String: Tag Name]
 *			> [Int32: ID]
 *			> ...
 *			> [String: null]
 *			
 *			The file then has pairs of tag names and IDs until it reaches a null string.
 *			
 *			> [String: Topic Type Name]
 *			> [[Topic Type Attributes]]
 *			> ...
 *			> [String: null]
 *			
 *			The file then encodes each topic type by its name string, followed by its attributes, and repeats until it reaches a null
 *			string instead of a new name string.
 *			
 *			> Topic Type Attributes:
 *			> [Int32: ID]
 *			> [String: Display Name]
 *			> [String: Plural Display Name]
 *			> [String: Simple Identifier]
 *			> [Byte: Index]
 *			> [Int32: Index With ID]?
 *			> [Byte: Scope]
 *			> [Byte: Break Lists]
 *			> [UInt16: Flags]
 *			
 *			The attributes include strings for the display and plural display names.  These are the computed strings, so if they
 *			weren't defined they'll still be here via whatever inheritance rules are in play.  If it's defined by the locale, it's the 
 *			resulting string that was retrieved from it.
 *			
 *			IndexWithID is the identifier of the topic type to index with and is only present if Index is set to 
 *			<TopicTypes.IndexValue.IndexWith>.
 *			
 *			> [String: Singular Keyword]
 *			> [Int32: Topic Type ID]
 *			> ...
 *			> [String: null]
 *			
 *			Next is a list of all the defined singular keywords and the IDs of the types they are mapped to.  They occur in pairs
 *			until a null string appears in place of the keyword.
 *			
 *			> [String: Plural Keyword]
 *			> [Int32: Topic Type ID]
 *			> ...
 *			> [String: null]
 *			
 *			Next is a similar list of plural keywords.
 *			
 *			> [String: Ignored Keyword]
 *			> ...
 *			> [String: null]
 *			
 *			Next is a similar list of ignored keywords, only the topic type ID is omitted.
 *			
 *		Revisions:
 *		
 *			2.0:
 *			
 *				- The file is introduced.
 *		
 */

// This file is part of Natural Docs, which is Copyright © 2003-2016 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;
using CodeClear.NaturalDocs.Engine.Collections;


namespace CodeClear.NaturalDocs.Engine.TopicTypes
	{
	public class Topics_nd
		{
		
		// Group: Functions
		// __________________________________________________________________________
		
		
		/* Constructor: Topics_nd
		 */
		public Topics_nd ()
			{
			}


		/* Function: Load
		 * Loads the information in <Topics.nd>, which is the computed topic settings from the last time Natural Docs was run.
		 * Returns whether it was successful.  If not all the out parameters will still return objects, they will just be empty.  
		 */
		public bool Load (Path filename,
								 out List<TopicType> binaryTopicTypes, 
								 out List<Tag> binaryTags,
								 out List<KeyValuePair<string, int>> binarySingularKeywords,
								 out List<KeyValuePair<string, int>> binaryPluralKeywords, 
								 out List<string> binaryIgnoredKeywords)
			{
			binaryTopicTypes = new List<TopicType>();
			binaryTags = new List<Tag>();
			
			binarySingularKeywords = new List<KeyValuePair<string,int>>();
			binaryPluralKeywords = new List<KeyValuePair<string,int>>();
			binaryIgnoredKeywords = new List<string>();
			
			BinaryFile file = new BinaryFile();
			bool result = true;
			
			try
				{
				if (file.OpenForReading(filename, "2.0") == false)
					{
					result = false;
					}
				else
					{
					
					// [String: Tag Name]
					// [Int32: ID]
					// ...
					// [String: null]
					
					string tagName = file.ReadString();
					
					while (tagName != null)
						{
						Tag tag = new Tag(tagName);
						tag.ID = file.ReadInt32();
						binaryTags.Add(tag);

						tagName = file.ReadString();
						}
						

					// [String: Topic Type Name]
					// [Int32: ID]
					// [String: Display Name]
					// [String: Plural Display Name]
					// [String: Simple Identifier]
					// [Byte: Index]
					// [Int32: Index With ID]?
					// [Byte: Scope]
					// [Byte: Break Lists]
					// [UInt16: Flags]
					// ...
					// [String: null]
						
					string topicTypeName = file.ReadString();
					IDObjects.NumberSet topicTypeIDs = new IDObjects.NumberSet();
					
					while (topicTypeName != null)
						{
						TopicType topicType = new TopicType(topicTypeName);
						
						topicType.ID = file.ReadInt32();
						topicType.DisplayName = file.ReadString();
						topicType.PluralDisplayName = file.ReadString();
						topicType.SimpleIdentifier = file.ReadString();

						// We don't have to validate the enum and flag values because they're only used to compare to the config file 
						// versions, which are validated.  If these are invalid they'll just show up as changed.

						topicType.Index = (TopicType.IndexValue)file.ReadByte();

						if (topicType.Index == TopicType.IndexValue.IndexWith)
							{  topicType.IndexWith = file.ReadInt32();  }

						topicType.Scope = (TopicType.ScopeValue)file.ReadByte();
						topicType.BreakLists = (file.ReadByte() != 0);
						topicType.Flags.AllConfigurationProperties = (TopicTypeFlags.FlagValues)file.ReadUInt16();

						binaryTopicTypes.Add(topicType);
						topicTypeIDs.Add(topicType.ID);
						
						topicTypeName = file.ReadString();
						}
						
					// Check the Index With values after they're all entered in.
					foreach (TopicType topicType in binaryTopicTypes)
						{
						if (topicType.Index == TopicType.IndexValue.IndexWith && !topicTypeIDs.Contains(topicType.IndexWith))
							{  result = false;  }
						}

				
					// [String: Singular Keyword]
					// [Int32: Topic Type ID]
					// ...
					// [String: null]

					string keyword = file.ReadString();
					
					while (keyword != null)
						{
						int id = file.ReadInt32();
						
						binarySingularKeywords.Add( new KeyValuePair<string,int>(keyword, id) );
						if (!topicTypeIDs.Contains(id))
							{  result = false;  }
						
						keyword = file.ReadString();
						}	

				
					// [String: Plural Keyword]
					// [Int32: Topic Type ID]
					// ...
					// [String: null]

					keyword = file.ReadString();
					
					while (keyword != null)
						{
						int id = file.ReadInt32();
						
						binaryPluralKeywords.Add( new KeyValuePair<string, int>(keyword, id) );
						if (!topicTypeIDs.Contains(id))
							{  result = false;  }
						
						keyword = file.ReadString();
						}						

				
					// [String: Ignored Keyword]
					// ...
					// [String: null]

					keyword = file.ReadString();
					
					while (keyword != null)
						{
						binaryIgnoredKeywords.Add(keyword);
						
						keyword = file.ReadString();
						}						
					}
				}
			catch
				{
				result = false;
				}
			finally
				{
				file.Close();
				}
				
			if (result == false)
				{
				// Reset all the objects to empty versions.
				binaryTopicTypes.Clear();
				
				binarySingularKeywords.Clear();
				binaryPluralKeywords.Clear();
				binaryIgnoredKeywords.Clear();
				}
				
			return result;
			}


		/* Function: Save
		 * Saves the current computed topic types into <Topics.nd>.  Throws an exception if unsuccessful.
		 */
		public void Save (Path filename, IDObjects.Manager<TopicType> topicTypes, IDObjects.Manager<Tag> tags,
								StringTable<TopicType> singularKeywords, StringTable<TopicType> pluralKeywords,
								StringSet ignoredKeywords)
			{
			BinaryFile file = new BinaryFile();
			file.OpenForWriting(filename);

			try
				{

				// [String: Tag Name]
				// [Int32: ID]
				// ...
				// [String: null]
				
				foreach (Tag tag in tags)
					{
					file.WriteString(tag.Name);
					file.WriteInt32(tag.ID);
					}
					
				file.WriteString(null);
				

				// [String: Topic Type Name]
				// [Int32: ID]
				// [String: Display Name]
				// [String: Plural Display Name]
				// [String: Simple Identifier]
				// [Byte: Index]
				// [Int32: Index With ID]?
				// [Byte: Scope]
				// [Byte: Break Lists]
				// [UInt16: Flags]
				// ...
				// [String: null]

				foreach (TopicType topicType in topicTypes)
					{
					file.WriteString( topicType.Name );
					file.WriteInt32( topicType.ID );
					file.WriteString( topicType.DisplayName );
					file.WriteString( topicType.PluralDisplayName );
					file.WriteString( topicType.SimpleIdentifier );
					file.WriteByte( (byte)topicType.Index );
					
					if (topicType.Index == TopicType.IndexValue.IndexWith)
						{  file.WriteInt32( topicType.IndexWith );  }

					file.WriteByte( (byte)topicType.Scope );
					file.WriteByte( (byte)(topicType.BreakLists ? 1 : 0) );
					file.WriteUInt16( (ushort)topicType.Flags.AllConfigurationProperties );
					}
					
				file.WriteString(null);
				
				
				// [String: Singular Keyword]
				// [Int32: Topic Type ID]
				// ...
				// [String: null]

				foreach (KeyValuePair<string, TopicType> pair in singularKeywords)
					{
					file.WriteString( pair.Key );
					file.WriteInt32( pair.Value.ID );
					}
					
				file.WriteString(null);
				
				
				// [String: Plural Keyword]
				// [Int32: Topic Type ID]
				// ...
				// [String: null]

				foreach (KeyValuePair<string, TopicType> pair in pluralKeywords)
					{
					file.WriteString( pair.Key );
					file.WriteInt32( pair.Value.ID );
					}
					
				file.WriteString(null);
				
				
				// [String: Ignored Keyword]
				// ...
				// [String: null]

				foreach (string keyword in ignoredKeywords)
					{
					file.WriteString( keyword );
					}
					
				file.WriteString(null);
				}
				
			finally
				{
				file.Close();
				}
			}
		 
		}
	}