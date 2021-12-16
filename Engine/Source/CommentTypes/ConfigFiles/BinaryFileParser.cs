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
			BinaryFile binaryFile = new BinaryFile();
			
			try
				{
				if (binaryFile.OpenForReading(filename) == false)
					{
					config = null;
					return false;
					}
				else if (binaryFile.Version.IsAtLeastRelease("2.2") == false &&
						   binaryFile.Version.IsSamePreRelease(Engine.Instance.Version) == false)
					{
					binaryFile.Close();
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
					
					string tagName = binaryFile.ReadString();
					
					while (tagName != null)
						{
						Tag tag = new Tag(tagName);
						tag.ID = binaryFile.ReadInt32();
						config.AddTag(tag);

						tagName = binaryFile.ReadString();
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
						
					string commentTypeName = binaryFile.ReadString();
					
					while (commentTypeName != null)
						{
						CommentType commentType = new CommentType(commentTypeName);
						
						commentType.ID = binaryFile.ReadInt32();
						commentType.DisplayName = binaryFile.ReadString();
						commentType.PluralDisplayName = binaryFile.ReadString();
						commentType.SimpleIdentifier = binaryFile.ReadString();

						// We don't have to validate the scope and flag values because they're only used to compare to the text file 
						// versions, which are validated.  If these are invalid they'll just show up as changed.

						commentType.Scope = (CommentType.ScopeValue)binaryFile.ReadByte();

						int hierarchyID = binaryFile.ReadInt32();
						if (hierarchyID != 0)
							{  commentType.HierarchyID = hierarchyID;  }

						commentType.Flags = (CommentType.FlagValue)binaryFile.ReadByte();

						config.AddCommentType(commentType);
						
						commentTypeName = binaryFile.ReadString();
						}

						
					// [String: Keyword]
					// [Byte: Plural (0 or 1)]
					// [Int32: Comment Type ID]
					// [Int32: Language ID or 0 if agnostic]
					// ...
					// [String: null]

					string keywordName = binaryFile.ReadString();

					while (keywordName != null)
						{
						var keywordDefinition = new KeywordDefinition(keywordName);

						keywordDefinition.Plural = (binaryFile.ReadByte() != 0);
						keywordDefinition.CommentTypeID = binaryFile.ReadInt32();

						int languageID = binaryFile.ReadInt32();
						if (languageID != 0)
							{  keywordDefinition.LanguageID = languageID;  }

						config.AddKeywordDefinition(keywordDefinition);

						keywordName = binaryFile.ReadString();
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
				if (binaryFile.IsOpen)
					{  binaryFile.Close();  }
				}
				
			return true;
			}


		/* Function: Save
		 * Saves the passed <Config> into <Comments.nd>.  Throws an exception if unsuccessful.
		 */
		public void Save (Path filename, Config config)
			{
			BinaryFile binaryFile = new BinaryFile();
			binaryFile.OpenForWriting(filename);

			try
				{

				// [String: Tag Name]
				// [Int32: ID]
				// ...
				// [String: null]
				
				foreach (var tag in config.Tags)
					{
					binaryFile.WriteString(tag.Name);
					binaryFile.WriteInt32(tag.ID);
					}
					
				binaryFile.WriteString(null);
				

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
					binaryFile.WriteString( commentType.Name );
					binaryFile.WriteInt32( commentType.ID );
					binaryFile.WriteString( commentType.DisplayName );
					binaryFile.WriteString( commentType.PluralDisplayName );
					binaryFile.WriteString( commentType.SimpleIdentifier );
					binaryFile.WriteByte( (byte)commentType.Scope );
					binaryFile.WriteInt32( commentType.HierarchyID );
					binaryFile.WriteByte( (byte)commentType.Flags );
					}
					
				binaryFile.WriteString(null);
				
				
				// [String: Keyword]
				// [Byte: Plural (0 or 1)]
				// [Int32: Comment Type ID]
				// [Int32: Language ID or 0 if agnostic]
				// ...
				// [String: null]

				foreach (var keywordDefinition in config.KeywordDefinitions)
					{
					binaryFile.WriteString( keywordDefinition.Keyword );
					binaryFile.WriteByte( (byte)(keywordDefinition.Plural ? 1 : 0) );
					binaryFile.WriteInt32( keywordDefinition.CommentTypeID );
					binaryFile.WriteInt32( (keywordDefinition.IsLanguageSpecific ? keywordDefinition.LanguageID : 0) );
					}

				binaryFile.WriteString(null);
				}
				
			finally
				{
				binaryFile.Close();
				}
			}
		 
		}
	}