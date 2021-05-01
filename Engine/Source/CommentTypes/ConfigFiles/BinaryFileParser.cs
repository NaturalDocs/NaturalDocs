/* 
 * Class: CodeClear.NaturalDocs.Engine.CommentTypes.ConfigFiles.BinaryFileParser
 * ____________________________________________________________________________
 * 
 * A class to handle loading and saving <Comments.nd>.
 * 
 * 
 * Multithreading: Not Thread Safe
 * 
 *		The parser object may be reused, but multiple threads cannot use it at the same time.
 *		
 */

// This file is part of Natural Docs, which is Copyright © 2003-2021 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;


namespace CodeClear.NaturalDocs.Engine.CommentTypes.ConfigFiles
	{
	public class BinaryFileParser
		{
		
		// Group: Functions
		// __________________________________________________________________________
		
		
		/* Constructor: BinaryFileParser
		 */
		public BinaryFileParser ()
			{
			}


		/* Function: Load
		 * Loads the information in <Comments.nd> into a <Config> object, returning whether it was successful.  If it was not
		 * config will be null.
		 */
		public bool Load (Path filename, out Config config)
			{
			BinaryFile file = new BinaryFile();
			
			try
				{
				if (file.OpenForReading(filename, "2.2") == false)
					{
					config = null;
					return false;
					}
				else
					{
					config = new Config();
					
					// [String: Tag Name]
					// [Int32: ID]
					// ...
					// [String: null]
					
					string tagName = file.ReadString();
					
					while (tagName != null)
						{
						Tag tag = new Tag(tagName);
						tag.ID = file.ReadInt32();
						config.AddTag(tag);

						tagName = file.ReadString();
						}
						

					// [String: Comment Type Name]
					// [Int32: ID]
					// [String: Display Name]
					// [String: Plural Display Name]
					// [String: Simple Identifier]
					// [Byte: Scope]
					// [Int32: Hierarchy ID or 0 if none]
					// [Byte: Flags]
					// ...
					// [String: null]
						
					string commentTypeName = file.ReadString();
					
					while (commentTypeName != null)
						{
						CommentType commentType = new CommentType(commentTypeName);
						
						commentType.ID = file.ReadInt32();
						commentType.DisplayName = file.ReadString();
						commentType.PluralDisplayName = file.ReadString();
						commentType.SimpleIdentifier = file.ReadString();

						// We don't have to validate the scope and flag values because they're only used to compare to the text file 
						// versions, which are validated.  If these are invalid they'll just show up as changed.

						commentType.Scope = (CommentType.ScopeValue)file.ReadByte();

						int hierarchyID = file.ReadInt32();
						if (hierarchyID != 0)
							{  commentType.HierarchyID = hierarchyID;  }

						commentType.Flags = (CommentType.FlagValue)file.ReadByte();

						config.AddCommentType(commentType);
						
						commentTypeName = file.ReadString();
						}

						
					// [String: Keyword]
					// [Byte: Plural (0 or 1)]
					// [Int32: Comment Type ID]
					// [Int32: Language ID or 0 if agnostic]
					// ...
					// [String: null]

					string keywordName = file.ReadString();

					while (keywordName != null)
						{
						var keywordDefinition = new KeywordDefinition(keywordName);

						keywordDefinition.Plural = (file.ReadByte() != 0);
						keywordDefinition.CommentTypeID = file.ReadInt32();

						int languageID = file.ReadInt32();
						if (languageID != 0)
							{  keywordDefinition.LanguageID = languageID;  }

						config.AddKeywordDefinition(keywordDefinition);

						keywordName = file.ReadString();
						}
					}
				}
			catch
				{  
				config = null;
				return false;
				}
			finally
				{  
				file.Close();  
				}
				
			return true;
			}


		/* Function: Save
		 * Saves the passed <Config> into <Comments.nd>.  Throws an exception if unsuccessful.
		 */
		public void Save (Path filename, Config config)
			{
			BinaryFile file = new BinaryFile();
			file.OpenForWriting(filename);

			try
				{

				// [String: Tag Name]
				// [Int32: ID]
				// ...
				// [String: null]
				
				foreach (var tag in config.Tags)
					{
					file.WriteString(tag.Name);
					file.WriteInt32(tag.ID);
					}
					
				file.WriteString(null);
				

				// [String: Comment Type Name]
				// [Int32: ID]
				// [String: Display Name]
				// [String: Plural Display Name]
				// [String: Simple Identifier]
				// [Byte: Scope]
				// [Int32: Hierarchy ID or 0 if none]
				// [Byte: Flags]
				// ...
				// [String: null]

				foreach (var commentType in config.CommentTypes)
					{
					file.WriteString( commentType.Name );
					file.WriteInt32( commentType.ID );
					file.WriteString( commentType.DisplayName );
					file.WriteString( commentType.PluralDisplayName );
					file.WriteString( commentType.SimpleIdentifier );
					file.WriteByte( (byte)commentType.Scope );
					file.WriteInt32( commentType.HierarchyID );
					file.WriteByte( (byte)commentType.Flags );
					}
					
				file.WriteString(null);
				
				
				// [String: Keyword]
				// [Byte: Plural (0 or 1)]
				// [Int32: Comment Type ID]
				// [Int32: Language ID or 0 if agnostic]
				// ...
				// [String: null]

				foreach (var keywordDefinition in config.KeywordDefinitions)
					{
					file.WriteString( keywordDefinition.Keyword );
					file.WriteByte( (byte)(keywordDefinition.Plural ? 1 : 0) );
					file.WriteInt32( keywordDefinition.CommentTypeID );
					file.WriteInt32( (keywordDefinition.IsLanguageSpecific ? keywordDefinition.LanguageID : 0) );
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