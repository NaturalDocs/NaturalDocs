/* 
 * Class: GregValure.NaturalDocs.Engine.CodeDB.Manager
 */

// This file is part of Natural Docs, which is Copyright © 2003-2011 Greg Valure.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;


namespace GregValure.NaturalDocs.Engine.CodeDB
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
																		"UsedEndingSymbolIDs TEXT NOT NULL )");
			
			connection.Execute("INSERT INTO System (Version, UsedTopicIDs, UsedEndingSymbolIDs) VALUES (?,?,?)", 
										Engine.Instance.VersionString, IDObjects.NumberSet.EmptySetString, IDObjects.NumberSet.EmptySetString);
			usedTopicIDs.Clear();
			usedEndingSymbolIDs.Clear();
			
			
			connection.Execute("CREATE TABLE Topics (TopicID INTEGER PRIMARY KEY NOT NULL, " +  // Automatic index
																		   "FileID INTEGER NOT NULL, " +
																		   "LanguageID INTEGER NOT NULL, " +
																		   "CommentLineNumber INTEGER NOT NULL, " +
																		   "CodeLineNumber INTEGER NOT NULL, " +
																		   "Title TEXT NOT NULL, " +
																		   "Body TEXT, " +
																			"Summary TEXT, " +
																			"Prototype TEXT, " +
																		   "Symbol TEXT NOT NULL, " +
																			"Parameters TEXT, " +
																		   "EndingSymbolID INTEGER NOT NULL, " +
																		   "TopicTypeID INTEGER NOT NULL, " +
																		   "AccessLevel INTEGER NOT NULL, " +
																		   "Tags TEXT )");
																	   
			connection.Execute("CREATE INDEX TopicsFileCommentIndex ON Topics (FileID, CommentLineNumber)");
			connection.Execute("CREATE INDEX TopicsEndingSymbolID ON Topics (EndingSymbolID)");
			
			
			connection.Execute("CREATE TABLE EndingSymbols (EndingSymbolID INTEGER PRIMARY KEY NOT NULL, " +  // Automatic index
																				 "EndingSymbol TEXT UNIQUE NOT NULL )");  // Automatic index
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
		 *		- <UsedEndingSymbolIDs>
		 */
		protected void LoadSystemVariables ()
			{
			using (SQLite.Query query = connection.Query("SELECT UsedTopicIDs, UsedEndingSymbolIDs from System"))
				{
				query.Step();
				
				usedTopicIDs.FromString( query.StringColumn(0) );
				usedEndingSymbolIDs.FromString( query.StringColumn(1) );
				}
			}
			
			
		/* Function: SaveSystemVariablesAndVersion
		 * Saves various system variables to the database, as well as setting the version variable to the current version.
		 */
		protected void SaveSystemVariablesAndVersion ()
			{
			connection.Execute("UPDATE System SET Version=?, UsedTopicIDs=?, UsedEndingSymbolIDs=?",
										Engine.Instance.VersionString, usedTopicIDs.ToString(), usedEndingSymbolIDs.ToString());
			}
			

		/* Function: Cleanup
		 * 
		 * Cleans up any stray data associated with the database, assuming all documentation is up to date.  You can pass a
		 * <CancelDelegate> if you'd like to interrupt this process early.
		 * 
		 * This goes through all the ending symbol IDs used by topics which have been deleted to see if any of them are no
		 * longer in use and thus can be deleted as well.
		 */
		public void Cleanup (CancelDelegate cancelDelegate)
			{
			bool writeLock = false;
			databaseLock.GetReadPossibleWriteLock(false);
			
			try
				{
				if (cancelDelegate() || endingSymbolIDsToCheckForDeletion.IsEmpty)
					{  return;  }
					
				List<int> endingSymbolIDsToRemove = new List<int>();
				
				using (SQLite.Query query = connection.Query("SELECT TopicID from Topics where EndingSymbolID = ? LIMIT 1"))
					{
					foreach (int endingSymbolID in endingSymbolIDsToCheckForDeletion)
						{
						if (cancelDelegate())
							{  return;  }
						
						query.BindValues(endingSymbolID);
						
						if (query.Step() == false)
							{  endingSymbolIDsToRemove.Add(endingSymbolID);  }
							
						query.Reset(true);
						}
					}
					
				if (endingSymbolIDsToRemove.Count > 0)
					{
					databaseLock.UpgradeToReadWriteLock(false);
					writeLock = true;
					
					using (SQLite.Query query = connection.Query("DELETE FROM EndingSymbols WHERE EndingSymbolID = ?"))
						{
						foreach (int endingSymbolID in endingSymbolIDsToRemove)
							{
							if (cancelDelegate())
								{  return;  }
							
							query.BindValues(endingSymbolID);
							query.Step();
							query.Reset(true);
							
							endingSymbolIDsToCheckForDeletion.Remove(endingSymbolID);  
							}
						}
					}
				
				}
			finally
				{
				if (writeLock)
					{  databaseLock.ReleaseReadWriteLock(false);  }
				else
					{  databaseLock.ReleaseReadPossibleWriteLock(false);  }
				}
			}
			
			
		}
	}