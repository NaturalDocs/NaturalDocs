/* 
 * Class: CodeClear.NaturalDocs.Engine.CodeDB.Manager
 */

// This file is part of Natural Docs, which is Copyright © 2003-2021 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;


namespace CodeClear.NaturalDocs.Engine.CodeDB
	{
	public partial class Manager
		{		
		
		/* Function: CreateDatabase
		 * Assumes the database is completely empty, not just of data but of table definitions too, and initializes it.
		 * Also initializes the system variables so you don't have to call <LoadSystemVariables()> afterwards.
		 */
		protected void CreateDatabase ()
			{
			connection.Execute("CREATE TABLE System (Version TEXT NOT NULL, " +
																			"UsedTopicIDs TEXT NOT NULL, " +
																			"UsedLinkIDs TEXT NOT NULL, " +
																			"UsedImageLinkIDs TEXT NOT NULL, " +
																			"UsedClassIDs TEXT NOT NULL, " +
																			"UsedContextIDs TEXT NOT NULL )");
			
			connection.Execute("INSERT INTO System (Version, UsedTopicIDs, UsedLinkIDs, UsedImageLinkIDs, UsedClassIDs, UsedContextIDs) " +
										"VALUES (?,?,?,?,?,?)", 
										Engine.Instance.VersionString, IDObjects.NumberSet.EmptySetString, IDObjects.NumberSet.EmptySetString,
										IDObjects.NumberSet.EmptySetString, IDObjects.NumberSet.EmptySetString, IDObjects.NumberSet.EmptySetString);
			usedTopicIDs.Clear();
			usedLinkIDs.Clear();
			usedClassIDs.Clear();
			usedContextIDs.Clear();
			
			
			connection.Execute("CREATE TABLE Topics (TopicID INTEGER PRIMARY KEY NOT NULL, " +
																		  "Title TEXT NOT NULL, " +
																		  "Body TEXT, " +
																		  "Summary TEXT, " +
																		  "Prototype TEXT, " +
																		  "Symbol TEXT NOT NULL, " +
																		  "SymbolDefinitionNumber INTEGER NOT NULL, " +
																		  "ClassID INTEGER NOT NULL, " +
																		  "DefinesClass INTEGER NOT NULL, " +
																		  "IsList INTEGER NOT NULL, " +
																		  "IsEmbedded INTEGER NOT NULL, " +
																		  "EndingSymbol TEXT NOT NULL, " +
																		  "CommentTypeID INTEGER NOT NULL, " +
																		  "DeclaredAccessLevel INTEGER NOT NULL, " +
																		  "EffectiveAccessLevel INTEGER NOT NULL, " +
																		  "Tags TEXT, " +
																		  "FileID INTEGER NOT NULL, " +
																		  "FilePosition INTEGER NOT NULL, " +
																		  "CommentLineNumber INTEGER NOT NULL, " +
																		  "CodeLineNumber INTEGER NOT NULL, " +
																		  "LanguageID INTEGER NOT NULL, " +
																		  "PrototypeContextID INTEGER NOT NULL, " +
																		  "BodyContextID INTEGER NOT NULL )");
																	   
			connection.Execute("CREATE INDEX TopicsByFile ON Topics (FileID, FilePosition)");
			connection.Execute("CREATE INDEX TopicsByClass ON Topics (ClassID, FileID, FilePosition)");
			connection.Execute("CREATE INDEX TopicsByClassDefinition ON Topics (ClassID, DefinesClass)");
			connection.Execute("CREATE INDEX TopicsByEndingSymbol ON Topics (EndingSymbol)");


			connection.Execute("CREATE TABLE Links (LinkID INTEGER PRIMARY KEY NOT NULL, " +
																		"Type INTEGER NOT NULL, " +
																		"TextOrSymbol TEXT NOT NULL, " +
																		"ContextID INTEGER NOT NULL, " +
																		"FileID INTEGER NOT NULL, " +
																		"ClassID INTEGER NOT NULL, " +
																		"LanguageID INTEGER NOT NULL, " +
																		"EndingSymbol TEXT NOT NULL, " +
																		"TargetTopicID INTEGER NOT NULL, " +
																		"TargetClassID INTEGER NOT NULL, " +
																		"TargetScore INTEGER NOT NULL )");
																	   
			connection.Execute("CREATE INDEX LinksByFileAndType ON Links (FileID, Type)");
			connection.Execute("CREATE INDEX LinksByClass ON Links (ClassID, Type)");
			connection.Execute("CREATE INDEX LinksByEndingSymbols ON Links (EndingSymbol)");
			connection.Execute("CREATE INDEX LinksByTargetTopicID ON Links (TargetTopicID)");
			connection.Execute("CREATE INDEX LinksByTargetClassID ON Links (TargetClassID, Type)");


			connection.Execute("CREATE TABLE AlternateLinkEndingSymbols (LinkID INTEGER NOT NULL, " +
																										 "EndingSymbol TEXT NOT NULL, " +
																										 "PRIMARY KEY (LinkID, EndingSymbol) )");
																	   
			connection.Execute("CREATE INDEX AlternateLinkEndingSymbolsBySymbol ON AlternateLinkEndingSymbols (EndingSymbol)");


			connection.Execute("CREATE TABLE ImageLinks (ImageLinkID INTEGER PRIMARY KEY NOT NULL, " +
																				"OriginalText TEXT NOT NULL, " +
																				"Path TEXT NOT NULL, " +
																				"FileName TEXT NOT NULL, " +
																				"FileID INTEGER NOT NULL, " +
																				"ClassID INTEGER NOT NULL, " +
																				"TargetFileID INTEGER NOT NULL, " +
																				"TargetScore INTEGER NOT NULL )");

			connection.Execute("CREATE INDEX ImageLinksByFileID ON ImageLinks (FileID)");
			connection.Execute("CREATE INDEX ImageLinksByClassID ON ImageLinks (ClassID)");
			connection.Execute("CREATE INDEX ImageLinksByFileName ON ImageLinks (FileName)");
			connection.Execute("CREATE INDEX ImageLinksByTargetFileID ON ImageLinks (TargetFileID)");


			connection.Execute("CREATE TABLE Classes (ClassID INTEGER PRIMARY KEY NOT NULL, " +
																			"ClassString TEXT, " +
																			"LookupKey TEXT NOT NULL, " +
																			"ReferenceCount INTEGER NOT NULL )");
																	   
			connection.Execute("CREATE INDEX ClassesByLookupKey ON Classes (LookupKey)");


			connection.Execute("CREATE TABLE Contexts (ContextID INTEGER PRIMARY KEY NOT NULL, " +
																			  "ContextString TEXT NOT NULL, " +
																			  "ReferenceCount INTEGER NOT NULL )");
																	   
			connection.Execute("CREATE INDEX ContextsByContextString ON Contexts (ContextString)");
			}


		/* Function: ResetDatabase
		 * Removes all data from the database and creates a fresh set of tables.  Also initializes the system variables so you don't 
		 * have to call <LoadSystemVariables()> afterwards.
		 */
		protected void ResetDatabase ()
			{
			connection.Execute("DROP TABLE System");
			connection.Execute("DROP TABLE Topics");
			connection.Execute("DROP TABLE Links");
			connection.Execute("DROP TABLE AlternateLinkEndingSymbols");
			connection.Execute("DROP TABLE ImageLinks");
			connection.Execute("DROP TABLE Classes");
			connection.Execute("DROP TABLE Contexts");

			CreateDatabase();
			}


		/* Function: GetVersion
		 * Retrieves the database version.
		 */
		protected Version GetVersion ()
			{
			using (SQLite.Query query = connection.Query("SELECT Version FROM System"))
				{
				query.Step();			
				return new Version( query.StringColumn(0) );
				}
			}
			
			
		/* Function: LoadSystemVariables
		 * 
		 * Retrieves various system variables from the database.  This currently includes:
		 * 
		 *		- <UsedTopicIDs>
		 *		- <UsedLinkIDs>
		 *		- <UsedImageLinkIDs>
		 *		- <UsedClassIDs>
		 *		- <UsedContextIDs>
		 */
		protected void LoadSystemVariables ()
			{
			using (SQLite.Query query = connection.Query("SELECT UsedTopicIDs, UsedLinkIDs, UsedimageLinkIDs, UsedClassIDs, UsedContextIDs " +
																			   "from System"))
				{
				query.Step();
				
				usedTopicIDs.SetTo( query.NextStringColumn() );
				usedLinkIDs.SetTo( query.NextStringColumn() );
				usedImageLinkIDs.SetTo( query.NextStringColumn() );
				usedClassIDs.SetTo( query.NextStringColumn() );
				usedContextIDs.SetTo( query.NextStringColumn() );
				}
			}
			
			
		/* Function: SaveSystemVariablesAndVersion
		 * Saves various system variables to the database, as well as setting the version variable to the current version.
		 */
		protected void SaveSystemVariablesAndVersion ()
			{
			connection.Execute("UPDATE System SET Version=?, UsedTopicIDs=?, UsedLinkIDs=?, UsedImageLinkIDs=?, UsedClassIDs=?, UsedContextIDs=?", 
										 Engine.Instance.VersionString, usedTopicIDs.ToString(), usedLinkIDs.ToString(), usedImageLinkIDs.ToString(),
										 usedClassIDs.ToString(), usedContextIDs.ToString());
			}
			
		}
	}