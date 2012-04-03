/* 
 * Class: GregValure.NaturalDocs.Engine.CodeDB.Manager
 * ____________________________________________________________________________
 * 
 * A class to manage information about various aspects of the code and its documentation.
 * 
 * 
 * Topic: Usage
 * 
 *		- Register any change watching objects you desire with <AddChangeWatcher()>.
 * 
 *		- Call <Engine.Instance.Start()> which will start this module.
 *		
 *		- Call <GetAccessor()> or <GetPriorityAccessor()> to create objects which will be used to manipulate the database.
 *		  Each thread must have their own.
 *		  
 *		- The change watchers will receive notifications of any modifications the accessors perform.  They can be added and
 *		  removed while the module is running.
 *		  
 *		- Each <Accessor> must be disposed before disposing of the database manager.
 *		
 *		- Disposing of the manager will automatically call <Cleanup()>, though if you have some idle time in which the 
 *		  documentation is completely updated you may call it ahead of time.
 *		  
 * 
 * Multithreading: Thread Safety Notes
 * 
 *		> DatabaseLock -> ChangeWatchers
 * 
 *		Externally, this class is thread safe so long as each thread uses its own <Accessor>.
 *		
 *		For the <Accessor> implementation, all uses of the database connection must be managed by <DatabaseLock>.  
 *		<UsedTopicIDs> and <UsedContextIDs> are only relevant when making changes to the database, so they are 
 *		managed by <DatabaseLock> as well.
 *		
 *		The change watchers, on the other hand, have their own lock since they may be accessed independently.  You may 
 *		attempt to acquire the list with <LockChangeWatchers()> while holding <DatabaseLock>, but not vice versa.
 *		
 * 
 * Topic: Used IDs and Transactions
 * 
 *		At the moment, ID tracking number sets such as <UsedTopicIDs> don't support transactions correctly.  If you were to
 *		add a topic to the database as part of a transaction and then roll it back instead of committing it, the IDs would still
 *		be marked as used.  This has the potential to eat up all the available IDs if a database is used over a long period of time 
 *		without a full rebuild ever being performed.
 *		
 *		This is not being fixed, however, because it's assumed that rolling back transactions never happens in Natural Docs as 
 *		part of a normal path of execution.  Transactions are used mostly for performance and just as good practice in case
 *		this assumption should change in the future.  The only time it should occur is if the program crashes and it's triggered
 *		automatically.  However, in this case the database will be completely rebuilt on the next execution anyway so we don't 
 *		need to worry about it.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2012 Greg Valure.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;
using GregValure.NaturalDocs.Engine.Collections;
using GregValure.NaturalDocs.Engine.Languages;
using GregValure.NaturalDocs.Engine.Links;
using GregValure.NaturalDocs.Engine.Symbols;


namespace GregValure.NaturalDocs.Engine.CodeDB
	{
	public partial class Manager : IDisposable
		{
		
		// Group: Functions
		// __________________________________________________________________________
		
		
		/* Function: Manager
		 */
		public Manager ()
			{
			connection = null;
			databaseLock = new Lock();

			usedTopicIDs = new IDObjects.NumberSet();
			usedLinkIDs = new IDObjects.NumberSet();
			usedContextIDs = new IDObjects.NumberSet();

			linksToResolve = new IDObjects.NumberSet();
			newTopicsByEndingSymbol = new SafeDictionary<Symbols.EndingSymbol, IDObjects.SparseNumberSet>();
			contextReferenceCache = new ContextReferenceCache();
			
			changeWatchers = new List<IChangeWatcher>();
			}
			
			
		/* Function: AddChangeWatcher
		 * Adds an object to be notified about changes to the database.  This can be called both before and after
		 * <Start()>.
		 */
		public void AddChangeWatcher (IChangeWatcher watcher)
			{
			lock (changeWatchers)
				{
				changeWatchers.Add(watcher);
				}
			}
			
			
		/* Function: AddPriorityChangeWatcher
		 * Adds an object to be notified about changes to the database.  Ones added with this function will receive
		 * change notifications before ones that aren't.  This can be called both before and after <Start()>.
		 */
		public void AddPriorityChangeWatcher (IChangeWatcher watcher)
			{
			lock (changeWatchers)
				{
				changeWatchers.Insert(0, watcher);
				}
			}
			
			
		/* Function: RemoveChangeWatcher
		 * Removes a watcher so that they're no longer notified of changes to the database.  It doesn't matter which
		 * function you used to add it with.  This can be called both before and after <Start()>.
		 */
		public void RemoveChangeWatcher (IChangeWatcher watcher)
			{
			lock (changeWatchers)
				{
				for (int i = 0; i < changeWatchers.Count; i++)
					{
					if ((object)watcher == (object)changeWatchers[i])
						{
						changeWatchers.RemoveAt(i);
						return;
						}
					}
				}
			}
			
			
		/* Function: Start
		 * 
		 * Dependencies:
		 * 
		 *		- <Config.Manager> must be started before using the rest of the class.
		 */
		public bool Start (Errors.ErrorList errors)
			{
			SQLite.API.Result sqliteResult = SQLite.API.Initialize();
			
			if (sqliteResult != SQLite.API.Result.OK)
			    {  throw new SQLite.Exceptions.UnexpectedResult("Could not initialize SQLite.", sqliteResult);  }

			Path databaseFile = Engine.Instance.Config.WorkingDataFolder + "/CodeDB.nd";
			connection = new SQLite.Connection();
			bool success = false;
			
			if (Engine.Instance.Config.ReparseEverything == false)
				{
				try
					{
					connection.Open(databaseFile, false);
					
					Version version = GetVersion();
					
					if (Version.BinaryDataCompatibility(version, Engine.Instance.Version, "2.0") == true)
						{  
						LoadSystemVariables();
						success = true;
						}
					}
				catch { }
				}
			
			if (!success)
				{
				connection.Dispose();
				
				if (System.IO.File.Exists(databaseFile))
					{  System.IO.File.Delete(databaseFile);  }
					
				Engine.Instance.Config.ReparseEverything = true;
					
				connection.Open(databaseFile, true);
				CreateDatabase();
				}
				
			
			#if SHOW_TOPIC_CHANGES
				AddChangeWatcher( new ChangeNotifier() );
			#endif
				
			return true;
			}
			
			
		/* Function: GetAccessor
		 * Creates an <Accessor> for manipulating the database.  Each thread must have its own.
		 */
		public Accessor GetAccessor ()
			{
			return new Accessor(connection.CreateAnotherConnection(), false);
			}
			
			
		/* Function: GetPriorityAccessor
		 * Creates an <Accessor> for manipulating the database which takes priority over other Accessors whenever possible.  This
		 * is useful for interface related threads that should have greater priority than background workers.
		 */
		public Accessor GetPriorityAccessor ()
			{
			return new Accessor(connection.CreateAnotherConnection(), true);
			}


		/* Function: Dispose
		 */
		public void Dispose ()
			{
			if (connection != null)
				{
				if (databaseLock.IsLocked)
					{  throw new Exception("Attempted to dispose of database when there were still locks held.");  }
				
				Cleanup(Delegates.NeverCancel);
				SaveSystemVariablesAndVersion();
					
				connection.Dispose();
				connection = null;

				usedTopicIDs.Clear();
				usedLinkIDs.Clear();
				usedContextIDs.Clear();

				linksToResolve.Clear();
				newTopicsByEndingSymbol.Clear();
				contextReferenceCache.Clear();
				
				SQLite.API.Result shutdownResult = SQLite.API.ShutDown();

				if (shutdownResult != SQLite.API.Result.OK)
					{  throw new SQLite.Exceptions.UnexpectedResult("Could not shut down SQLite.", shutdownResult);  }
				}
			}
			
			
		/* Function: Cleanup
		 * 
		 * Cleans up any stray data associated with the database, assuming all documentation is up to date.  You can pass a
		 * <CancelDelegate> if you'd like to interrupt this process early.
		 * 
		 * <Dispose()> will call this function automatically so it's not strictly necessary to call it manually, though it's good
		 * practice to.  If you have idle time in which the documentation is completely up to date, calling this then instead of
		 * leaving it for <Dispose()> will allow the engine to shut down faster.
		 */
		public void Cleanup (CancelDelegate cancelDelegate)
			{
			FlushContextReferenceCache(cancelDelegate);
			}


		/* Function: FlushContextReferenceCache
		 * Applies any changes waiting in <ContextReferenceCache> to the database.
		 */
		protected void FlushContextReferenceCache (CancelDelegate cancelDelegate)
			{
			using (Accessor accessor = GetAccessor())
				{
				accessor.GetReadPossibleWriteLock();
				accessor.FlushContextReferenceCache(cancelDelegate);
				accessor.ReleaseLock();
				}
			}


		/* Function: ScoreLink
		 * 
		 * Generates a numeric score representing how well the <Topic> serves as a match for the <Link>.  Higher scores are
		 * better, and zero means they don't match at all.
		 * 
		 * If a score has to beat a certain threshold to be relevant, you can pass it to lessen the processing load.  This function 
		 * may be able to tell it can't beat the score early and return without performing later steps.  In these cases it will return 
		 * -1.
		 * 
		 * If scoring a Natural Docs link you must pass a list of alternate interpretations if there are any.  The list doesn't need to
		 * include the literal form.
		 */
		public long ScoreLink (Link link, Topic topic, int minimumScore = 0, List<LinkInterpretation> alternateInterpretations = null)
			{
			// DEPENDENCY: These functions depend on the score's internal format:
			//    - CodeDB.Manager.ScoreInterpretation()
			//    - CodeDB.Manager.GetInterpretationIndex()
			//    - EngineTests.LinkScoring

			// Other than that the score's format should be treated as opaque.  Nothing beyond these functions should try to 
			// interpret the value other than to know that higher is better, zero is impossible, and -1 means we quit early.

			// It's a 64-bit value so we'll assign bits to the different characteristics.  Higher order bits obviously result in higher 
			// numeric values so the characteristics are ordered by priority.

			// Format:
			// 0LCETPPP PPPPPPPP PPPPPPPP PSSSSSSS SSSIIIII IBFFFFFF Rbbbbbbb brrrrrr1

			// 0 - The first bit is zero to make sure the number is positive.

			// L - Whether the topic matches the link's language.
			// C - Whether the topic and link's capitalization match if it matters to the language.
			// E - Whether the text is an exact match with no plural or possessive conversions applied.
			// T - Whether the link parenthesis exactly match the topic title parenthesis
			// P - How well the parameters match.
			// S - How high on the scope list the symbol match is.
			// I - How high on the interpretation list (named/plural/possessive) the match is.
			// B - Whether the topic has a body
			// F - How high on the list of topics that define the same symbol in the same file this is.
			// R - Whether the topic has a prototype.
			// b - The length of the body divided by 16.
			// r - The length of the prototype divided by 16.

			// 1 - The final bit is one to make sure a match will never be zero.


			// For type and class parent links, the topic type MUST have the relevant attribute set to be possible.

			var topicType = Engine.Instance.TopicTypes.FromID(topic.TopicTypeID);

			if ( (link.Type == LinkType.ClassParent && topicType.ClassHierarchy == false) ||
				  (link.Type == LinkType.Type && topicType.VariableType == false) )
				{  return 0;  }


			// 0------- -------- -------- -------- -------- -------- -------- -------1
			// Our baseline.

			long score = 0x0000000000000001;


			// =L------ -------- -------- -------- -------- -------- -------- -------=
			// L - Whether the topic's language matches the link's language.  For type and class parent links this is mandatory.  For
			// Natural Docs links this is the highest priority criteria as links should favor any kind of match within their own language
			// over matches from another.

			if (link.LanguageID == topic.LanguageID)
				{  score |= 0x4000000000000000;  }
			else if (link.Type == LinkType.ClassParent || link.Type == LinkType.Type)
				{  return 0;  }
			else if (minimumScore > 0x3FFFFFFFFFFFFFFF)
				{  return -1;  }


			// ==CE---- -------- -------- -SSSSSSS SSSIIIII I------- -------- -------=
			// Now we have to go through the interpretations to figure out the fields that could change based on them.
			// C and S will be handled by ScoreInterpretation().  E and I will be handled here.

			// C - Whether the topic and link's capitalization match if it matters to the language.  This depends on the
			//			interpretation because it can be affected by how named links are split.
			// E - Whether the text is an exact match with no plural or possessive conversions applied.  Named links are
			//			okay.
			// S - How high on the scope list the symbol match is.
			// I - How high on the interpretation list (named/plural/possessive) the match is.

			long bestInterpretationScore;
			int bestInterpretationIndex;

			if (link.Type == LinkType.NaturalDocs)
				{
				// Test the literal interpretation since we always want it to be index zero.

				string parenthesis;
				bestInterpretationScore = ScoreInterpretation(topic, link, SymbolString.FromPlainText(link.Text, out parenthesis));
				bestInterpretationIndex = 0;

				// Add E if there was a match.
				if (bestInterpretationScore != 0)
					{  bestInterpretationScore |= 0x1000000000000000;  }


				// Test the alternates, filtering out the literal in case it is there too.

				if (alternateInterpretations != null)
					{
					long interpretationScore;
					int interpretationIndex = 1;

					foreach (LinkInterpretation interpretation in alternateInterpretations)
						{
						if (!interpretation.IsLiteral)
							{
							interpretationScore = ScoreInterpretation(topic, link, 
																										SymbolString.FromPlainText_ParenthesisAlreadyRemoved(interpretation.Target));

							if (interpretationScore != 0)
								{
								// Add E if there were no plurals or possessives.  Named links are okay.
								if (interpretation.PluralConversion == false && interpretation.PossessiveConversion == false)
									{  interpretationScore |= 0x1000000000000000;  }

								if (interpretationScore > bestInterpretationScore)
									{  
									bestInterpretationScore = interpretationScore;  
									bestInterpretationIndex = interpretationIndex;
									}
								}

							// We don't increment this for the literals we skip.
							interpretationIndex++;
							}
						}
					}
				}

			else // type or class parent link
				{
				bestInterpretationScore = ScoreInterpretation(topic, link, link.Symbol);
				bestInterpretationIndex = 0;

				// Add E if there was a match.
				if (bestInterpretationScore != 0)
					{  bestInterpretationScore |= 0x1000000000000000;  }
				}

			// If none of the symbol interpretations matched the topic, we're done.
			if (bestInterpretationScore == 0)
				{  return 0;  }

			// Combine C, E, and S into the main score.
			score |= bestInterpretationScore;

			// Calculate I so that lower indexes are higher scores.  Since these are the lowest order bits it's okay to leave
			// this for the end instead of calculating it for every interpretation.
			if (bestInterpretationIndex > 63)
				{  bestInterpretationIndex = 63;  }

			long bestInterpretationBits = 63 - bestInterpretationIndex;
			bestInterpretationBits <<= 23;

			score |= bestInterpretationBits;

			if ((score | 0x0FFFFF80007FFFFF) < minimumScore)
				{  return -1;  }


			// ====TPPP PPPPPPPP PPPPPPPP P======= ======== =------- -------- -------=
			// T - Whether the link parenthesis exactly match the topic title parenthesis.
			// P - How well the parameters match.

			// xxx we'll come back to this
			score |= 0x0FFFFF8000000000;


			// ======== ======== ======== ======== ======== =-FFFFFF -------- -------=
			// F - How high on the list of topics that define the same symbol in the same file this is.

			// xxx we'll come back to this
			score |= 0x00000000003F0000;


			// ======== ======== ======== ======== ======== =B====== -bbbbbbb b------=
			// B - Whether the topic has a body
			// b - The length of the body divided by 16.
			//    0-15 = 0
			//    16-31 = 1
			//    ...
			//		4064-4079 = 254
			//		4080+ = 255

			if (topic.Body != null)
				{
				long bodyBits = topic.Body.Length / 16;

				if (bodyBits > 255)
					{  bodyBits = 255;  }

				bodyBits <<= 7;
				bodyBits |= 0x0000000000400000;

				score |= bodyBits;
				}


			// ======== ======== ======== ======== ======== ======== R======= =rrrrrr=
			// R - Whether the topic has a prototype.
			// r - The length of the prototype divided by 16.
			//    0-15 = 0
			//    16-31 = 1
			//    ...
			//    992-1007 = 62
			//    1008+ = 63

			if (topic.Prototype != null)
				{
				long prototypeBits = topic.Prototype.Length / 16;

				if (prototypeBits > 63)
					{  prototypeBits = 63;  }

				prototypeBits <<= 1;
				prototypeBits |= 0x0000000000008000;

				score |= prototypeBits;
				}


			return score;
			}


		/* Function: ScoreInterpretation
		 * A function used by <ScoreLink()> to determine the C and S fields of the score for the passed interpretation. Only
		 * those fields and the trailing 1 will be set in the returned score.  If the interpretation doesn't match, it will return
		 * zero.
		 */
		private long ScoreInterpretation (Topic topic, Link link, SymbolString interpretation)
			{
			// --C----- -------- -------- -SSSSSSS SSS----- -------- -------- -------1
			// C - Whether the topic and link's capitalization match if it matters to the language.
			// S - How high on the scope list the symbol match is.

			string topicSymbolString = topic.Symbol.ToString();
			string linkSymbolString = interpretation.ToString();


			// First see if a match is possible under any circumstances.  The interpretation has to match the end of the topic
			// symbol or no amount of finagling with the scopes will work.

			if (topicSymbolString.EndsWith(linkSymbolString, StringComparison.CurrentCultureIgnoreCase) == false)
				{  return 0;  }


			// Determine what part of the topic symbol remains to be matched against the link scope, if any.

			int topicScopeIndex = 0;
			int topicScopeLength = 0;

			if (topicSymbolString.Length > linkSymbolString.Length)
				{
				// There has to be a separator before the link symbol to make sure we didn't cut a word in half.
				if (topicSymbolString[topicSymbolString.Length - linkSymbolString.Length - 1] != SymbolString.SeparatorChar)
					{  return 0;  }

				topicScopeLength = topicSymbolString.Length - linkSymbolString.Length - 1;
				}


			// -------- -------- -------- -------- -------- -------- -------- -------1
			// Our baseline.

			long score = 0x0000000000000001;


			// --C----- -------- -------- -------- -------- -------- -------- -------=
			// We may be able to determine C already.

			// C - Whether the topic and link's capitalization match if it matters to the language.
			//		Natural Docs links:
			//			1 - Topic language is case sensitive, case matches
			//			0 - Topic language is case sensitive, case differs
			//			1 - Topic language is case insensitive, case matches
			//			1 - Topic language is case insensitive, case differs
			//		Type/Class Parent links:
			//			We can assume they're the same language
			//			1 - Language is case sensitive, case matches
			//			X - Language is case sensitive, case differs
			//			1 - Language is case insensitive, case matches
			//			1 - Language is case insensitive, case differs

			// xxx For Natural Docs links we want to distinguish between when the topic is code and documentation,
			// as the case differing for documentation comments shouldn't matter regardless of the language setting.

			Language language = Engine.Instance.Languages.FromID(topic.LanguageID);
			bool cDependsOnScope;

			// If the language is case insensitive...
			if (language.CaseSensitive == false)
				{
				// We set the flag no matter what.  We don't have to bother with a comparison.
				score |= 0x2000000000000000;
				cDependsOnScope = false;
				}

			// The language is case sensitive.  If the case matches...
			else if (topicSymbolString.EndsWith(linkSymbolString))
				{
				// C depends on the scope matching case too.  Leave it unset for now.
				cDependsOnScope = true;
				}

			// The language is case sensitive and the case differs.
			else
				{
				// It's a hard requirement for type and class parent links in case sensitive languages.
				if (link.Type == LinkType.Type || link.Type == LinkType.ClassParent)
					{  return 0;  }

				// Otherwise C stays at zero no matter what.
				cDependsOnScope = false;
				}

			
			// Now we need to determine if we can match the remaining scope to anything in the context.

			int scopeListIndex;

			if (topicScopeLength == 0)
				{
				// If there's no remaining scope to match, then we know we already match as a global symbol.  We still need to 
				// figure out the scope list index though.

				if (link.Context.ScopeIsGlobal)
					{  scopeListIndex = 0;  }
				else
					{
					// Conceptually, we had to walk down the entire hierachy to get to global:
					//    Scope A.B.C = A.B.C.Name, A.B.Name, A.Name, Name = Index 3
					// so the scope list index is the number of dividers in the scope plus one.

					int linkScopeIndex, linkScopeLength;
					link.Context.GetRawTextScope(out linkScopeIndex, out linkScopeLength);

					int dividers = link.Context.RawText.Count(SymbolString.SeparatorChar, linkScopeIndex, linkScopeLength);
					scopeListIndex = dividers + 1;
					}

				// Add in C if necessary.
				if (cDependsOnScope)
					{  score |= 0x2000000000000000;  }
				}

			else // there's remaining topic scope to match
				{
				// So the situation is now that we had a partial match earlier, like "Name" matching "A.B.C.Name", leaving "A.B.C" as the
				// remaining topic scope.  We need to see if Link's scope can completely encompass the remaining scope:
				//    Topic A.B.C.Name + Link Name + Link Scope A.B.C = yes
				//    Topic A.B.C.Name + Link Name + Link Scope A.B = no
				//    Topic A.B.C.Name + Link Name + Link Scope A.B.C.D = yes, it can walk up the hierarchy
				//    Topic A.B.C.Name + Link Name + Link Scope A.B.CC = no, can't split a word
				//    Topic A.B.C.Name + Link Name + Link Scope X.Y.Z = no

				string linkContextString = link.Context.RawText;
				int linkScopeIndex, linkScopeLength;
				link.Context.GetRawTextScope(out linkScopeIndex, out linkScopeLength);

				// If the remaining topic scope is a substring or equal to the link scope...
				if (topicScopeLength <= linkScopeLength && 
					 string.Compare(linkContextString, linkScopeIndex, topicSymbolString, topicScopeIndex, topicScopeLength, true) == 0)
					{
					if (topicScopeLength == linkScopeLength)
						{
						// If it's an exact match, this is considered the first entry on our conceptual scope list.
						scopeListIndex = 0;
						}

					else // topicScopeLength < linkScopeLength
						{
						// If the scope was a substring, the next character needs to be a separator so we don't split a word.
						if (linkContextString[topicScopeLength] != SymbolString.SeparatorChar)
							{  return 0;  }

						// The scope list index is the number of separators we trimmed off:
						//    Link scope: A.B.C.D
						//    Remaining topic scope: A.B
						//    Scope list:
						//       0 - A.B.C.D
						//       1 - A.B.C
						//       2 - A.B
						//       3 - A
						//       4 - global
						scopeListIndex = linkContextString.Count(SymbolString.SeparatorChar, linkScopeIndex + topicScopeLength,
																								  linkScopeLength - topicScopeLength);
						}

					if (cDependsOnScope)
						{
						if (string.Compare(linkContextString, linkScopeIndex, topicSymbolString, topicScopeIndex, topicScopeLength, false) == 0)
							{  score |= 0x2000000000000000;  }

						// It's a hard requirement for type and class parent links
						else if (link.Type == LinkType.Type || link.Type == LinkType.ClassParent)
							{  return 0;  }
						}
					}
				else
					{  return 0;  }

				}

			// --=----- -------- -------- -SSSSSSS SSS----- -------- -------- -------=
			// Encode the scope index.  We want lower indexes to have a higher score.

			if (scopeListIndex > 1023)
				{  scopeListIndex = 1023;  }

			long scopeListBits = 1023 - scopeListIndex;
			scopeListBits <<= 29;

			score |= scopeListBits;

			return score;
			}


		/* Function: GetInterpretationIndex
		 * Retrieves the interpretation index from a link score.
		 */
		public int GetInterpretationIndex (long linkScore)
			{
			// -------- -------- -------- -------- ---IIIII I------- -------- --------
			linkScore &= 0x000000001F800000;
			linkScore >>= 23;

			return 63 - (int)linkScore;
			}


			
		// Group: Accessor Properties
		// These properties are internal and are only meant for use by <Accessor>.
		// __________________________________________________________________________
		
		
		/* Property: DatabaseLock
		 * The <CodeDB.Lock> used to manage access to this database.  It covers properties like <UsedTopicIDs> in addition
		 * to the SQLite database itself.
		 */
		internal Lock DatabaseLock
			{
			get
				{  return databaseLock;  }
			}
			
		/* Property: UsedTopicIDs
		 * An <IDObjects.NumberSet> of all the used topic IDs in <CodeDB.Topics>.  Its use is governed by <DatabaseLock>.
		 */
		internal IDObjects.NumberSet UsedTopicIDs
			{
			get
				{  return usedTopicIDs;  }
			}
			
		/* Property: UsedLinkIDs
		 * An <IDObjects.NumberSet> of all the used link IDs in <CodeDB.Links>.  Its use is governed by <DatabaseLock>.
		 */
		internal IDObjects.NumberSet UsedLinkIDs
			{
			get
				{  return usedLinkIDs;  }
			}
			
		/* Property: UsedContextIDs
		 * An <IDObjects.NumberSet> of all the used context IDs in <CodeDB.Contexts>.  Its use is governed by <DatabaseLock>.
		 */
		internal IDObjects.NumberSet UsedContextIDs
			{
			get
				{  return usedContextIDs;  }
			}

		/* Property: LinksToResolve
		 * An <IDObjects.NumberSet> of all the link IDs in <CodeDB.Links> that have changed and need to be resolved again.
		 * Note that this is not the complete set of all unresolved links; some links may have previously resolved to nothing and
		 * there may have been no changes made that could affect them.
		 */
		internal IDObjects.NumberSet LinksToResolve
			{
			get
				{  return linksToResolve;  }
			}

		/* Property: NewTopicsByEndingSymbol
		 * 
		 * Keeps track of all newly created <Topics>.  The keys are the <Symbols.EndingSymbols> the topics use, and the values
		 * are <IDObjects.SparseNumberSets> of all the topic IDs associated with that ending symbol.  This is used for resolving 
		 * links.
		 * 
		 * Rationale:
		 * 
		 *		When a new <Topic> is created, it might serve as a better definition for existing links.  We don't want to reresolve
		 *		the links as soon as the topic is created because there may be multiple topics that affect the same links and we'd 
		 *		be wasting effort.  Instead we store which topics are new and do this after parsing is complete.
		 *		
		 *		We can't store the <Topic> objects themselves because when doing a non-differential run every topic will be new and 
		 *		we'd end up storing the entire documentation structure in memory.  Instead we store the topic IDs and look up the 
		 *		<Topics> again when it's time to resolve links.
		 *		
		 *		We store them by ending symbol instead of in one NumberSet so that we can reresolve links in batches.  Topics that 
		 *		have the same ending symbol will be candidates for the same group of links, so we can query those topics and links
		 *		into memory, reresolve them all at once, and then move on to the next ending symbol.  If we stored a single NumberSet
		 *		of topic IDs we'd have to handle the topics one by one and query for each topic's links separately.
		 */
		internal SafeDictionary<Symbols.EndingSymbol, IDObjects.SparseNumberSet> NewTopicsByEndingSymbol
			{
			get
				{  return newTopicsByEndingSymbol;  }
			}

		/* Property: ContextReferenceCache
		 * A cache of all the reference count changes to <CodeDB.Contexts>.  Its use is governed by <DatabaseLock>.
		 */
		internal ContextReferenceCache ContextReferenceCache
			{
			get
				{  return contextReferenceCache;  }
			}
			
			
			
		// Group: Accessor Functions
		// These functions are internal and are only meant for use by <Accessor>.
		// __________________________________________________________________________
			
			
		/* Function: LockChangeWatchers
		 * Gets the list of objects watching the database for changes, which requires a lock.  The list will never be null.  You can 
		 * attempt to get this lock while holding <DatabaseLock>, but never the other way around.  Release it with
		 * <ReleaseChangeWatchers()>, after which the object can no longer be used in a thread safe manner.
		 */
		internal IList<IChangeWatcher> LockChangeWatchers ()
			{
			System.Threading.Monitor.Enter(changeWatchers);
			
			// The list can only be changed by the functions directly in the module.
			return changeWatchers.AsReadOnly();
			}
			
			
		/* Function: ReleaseChangeWatchers
		 * Releases the lock on the list obtained with <LockChangeWatchers()>.
		 */
		internal void ReleaseChangeWatchers ()
			{
			System.Threading.Monitor.Exit(changeWatchers);
			}
			
			
						
		// Group: Variables
		// __________________________________________________________________________
		
		
		/* var: connection
		 */
		protected SQLite.Connection connection;
		
		/* var: databaseLock
		 */
		protected Lock databaseLock;
		
		/* var: usedTopicIDs
		 */
		protected IDObjects.NumberSet usedTopicIDs;

		/* var: usedLinkIDs
		 */
		protected IDObjects.NumberSet usedLinkIDs;

		/* var: usedContextIDs
		 */
		protected IDObjects.NumberSet usedContextIDs;

		/* var: linksToResolve
		 */
		protected IDObjects.NumberSet linksToResolve;

		/* var: newTopicsByEndingSymbol
		 */
		protected SafeDictionary<Symbols.EndingSymbol, IDObjects.SparseNumberSet> newTopicsByEndingSymbol;

		/* var: contextReferenceCache
		 * A cache of all the reference count changes to be applied to <CodeDB.Contexts>.
		 */
		protected ContextReferenceCache contextReferenceCache;
		
		/* var: changeWatchers
		 * A list of objects that are watching the database for changes.  If there are none, the list will be empty
		 * rather than null.
		 */
		protected List<IChangeWatcher> changeWatchers;
			
		}
	}