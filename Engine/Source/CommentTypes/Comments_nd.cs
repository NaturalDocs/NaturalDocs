/* 
 * Class: CodeClear.NaturalDocs.Engine.CommentTypes.Comments_nd
 * ____________________________________________________________________________
 * 
 * A class to handle loading and saving <Comments.nd>.
 * 
 * 
 * Threading: Not Thread Safe
 * 
 *		The parser object may be reused, but multiple threads cannot use it at the same time.
 *		
 * 
 * File: Comments.nd
 * 
 *		A binary file which stores the combined results of the two versions of <Comments.txt> as of the last run, as well as storing
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
 *			> [String: Comment Type Name]
 *			> [[Comment Type Attributes]]
 *			> ...
 *			> [String: null]
 *			
 *			The file then encodes each comment type by its name string, followed by its attributes, and repeats until it reaches a null
 *			string instead of a new name string.
 *			
 *			> Comment Type Attributes:
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
 *			IndexWithID is the identifier of the comment type to index with and is only present if Index is set to 
 *			<CommentTypes.IndexValue.IndexWith>.
 *			
 *			> [String: Singular Keyword]
 *			> [Int32: Comment Type ID]
 *			> ...
 *			> [String: null]
 *			
 *			Next is a list of all the defined singular keywords and the IDs of the types they are mapped to.  They occur in pairs
 *			until a null string appears in place of the keyword.
 *			
 *			> [String: Plural Keyword]
 *			> [Int32: Comment Type ID]
 *			> ...
 *			> [String: null]
 *			
 *			Next is a similar list of plural keywords.
 *			
 *			> [String: Ignored Keyword]
 *			> ...
 *			> [String: null]
 *			
 *			Next is a similar list of ignored keywords, only the comment type ID is omitted.
 *			
 *		Revisions:
 *		
 *			2.0:
 *			
 *				- The file is introduced.
 *		
 */

// This file is part of Natural Docs, which is Copyright © 2003-2017 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;
using CodeClear.NaturalDocs.Engine.Collections;


namespace CodeClear.NaturalDocs.Engine.CommentTypes
	{
	public class Comments_nd
		{
		
		// Group: Functions
		// __________________________________________________________________________
		
		
		/* Constructor: Comments_nd
		 */
		public Comments_nd ()
			{
			}


		/* Function: Load
		 * Loads the information in <Comments.nd>, which is the computed comment settings from the last time Natural Docs was run.
		 * Returns whether it was successful.  If not all the out parameters will still return objects, they will just be empty.  
		 */
		public bool Load (Path filename,
								 out List<CommentType> binaryCommentTypes, 
								 out List<Tag> binaryTags,
								 out List<KeyValuePair<string, int>> binarySingularKeywords,
								 out List<KeyValuePair<string, int>> binaryPluralKeywords, 
								 out List<string> binaryIgnoredKeywords)
			{
			binaryCommentTypes = new List<CommentType>();
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
						

					// [String: Comment Type Name]
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
						
					string commentTypeName = file.ReadString();
					IDObjects.NumberSet commentTypeIDs = new IDObjects.NumberSet();
					
					while (commentTypeName != null)
						{
						CommentType commentType = new CommentType(commentTypeName);
						
						commentType.ID = file.ReadInt32();
						commentType.DisplayName = file.ReadString();
						commentType.PluralDisplayName = file.ReadString();
						commentType.SimpleIdentifier = file.ReadString();

						// We don't have to validate the enum and flag values because they're only used to compare to the config file 
						// versions, which are validated.  If these are invalid they'll just show up as changed.

						commentType.Index = (CommentType.IndexValue)file.ReadByte();

						if (commentType.Index == CommentType.IndexValue.IndexWith)
							{  commentType.IndexWith = file.ReadInt32();  }

						commentType.Scope = (CommentType.ScopeValue)file.ReadByte();
						commentType.BreakLists = (file.ReadByte() != 0);
						commentType.Flags.AllConfigurationProperties = (CommentTypeFlags.FlagValues)file.ReadUInt16();

						binaryCommentTypes.Add(commentType);
						commentTypeIDs.Add(commentType.ID);
						
						commentTypeName = file.ReadString();
						}
						
					// Check the Index With values after they're all entered in.
					foreach (CommentType commentType in binaryCommentTypes)
						{
						if (commentType.Index == CommentType.IndexValue.IndexWith && !commentTypeIDs.Contains(commentType.IndexWith))
							{  result = false;  }
						}

				
					// [String: Singular Keyword]
					// [Int32: Comment Type ID]
					// ...
					// [String: null]

					string keyword = file.ReadString();
					
					while (keyword != null)
						{
						int id = file.ReadInt32();
						
						binarySingularKeywords.Add( new KeyValuePair<string,int>(keyword, id) );
						if (!commentTypeIDs.Contains(id))
							{  result = false;  }
						
						keyword = file.ReadString();
						}	

				
					// [String: Plural Keyword]
					// [Int32: Comment Type ID]
					// ...
					// [String: null]

					keyword = file.ReadString();
					
					while (keyword != null)
						{
						int id = file.ReadInt32();
						
						binaryPluralKeywords.Add( new KeyValuePair<string, int>(keyword, id) );
						if (!commentTypeIDs.Contains(id))
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
				binaryCommentTypes.Clear();
				
				binarySingularKeywords.Clear();
				binaryPluralKeywords.Clear();
				binaryIgnoredKeywords.Clear();
				}
				
			return result;
			}


		/* Function: Save
		 * Saves the current computed comment types into <Comments.nd>.  Throws an exception if unsuccessful.
		 */
		public void Save (Path filename, IDObjects.Manager<CommentType> commentTypes, IDObjects.Manager<Tag> tags,
								StringTable<CommentType> singularKeywords, StringTable<CommentType> pluralKeywords,
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
				

				// [String: Comment Type Name]
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

				foreach (CommentType commentType in commentTypes)
					{
					file.WriteString( commentType.Name );
					file.WriteInt32( commentType.ID );
					file.WriteString( commentType.DisplayName );
					file.WriteString( commentType.PluralDisplayName );
					file.WriteString( commentType.SimpleIdentifier );
					file.WriteByte( (byte)commentType.Index );
					
					if (commentType.Index == CommentType.IndexValue.IndexWith)
						{  file.WriteInt32( commentType.IndexWith );  }

					file.WriteByte( (byte)commentType.Scope );
					file.WriteByte( (byte)(commentType.BreakLists ? 1 : 0) );
					file.WriteUInt16( (ushort)commentType.Flags.AllConfigurationProperties );
					}
					
				file.WriteString(null);
				
				
				// [String: Singular Keyword]
				// [Int32: Comment Type ID]
				// ...
				// [String: null]

				foreach (KeyValuePair<string, CommentType> pair in singularKeywords)
					{
					file.WriteString( pair.Key );
					file.WriteInt32( pair.Value.ID );
					}
					
				file.WriteString(null);
				
				
				// [String: Plural Keyword]
				// [Int32: Comment Type ID]
				// ...
				// [String: null]

				foreach (KeyValuePair<string, CommentType> pair in pluralKeywords)
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