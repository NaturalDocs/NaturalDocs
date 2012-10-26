/* 
 * Class: GregValure.NaturalDocs.Engine.Output.Builders.HTML
 * ____________________________________________________________________________
 * 
 */

// This file is part of Natural Docs, which is Copyright © 2003-2012 Greg Valure.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;
using System.Text;
using GregValure.NaturalDocs.Engine.Collections;
using GregValure.NaturalDocs.Engine.Languages;
using GregValure.NaturalDocs.Engine.Links;
using GregValure.NaturalDocs.Engine.Symbols;
using GregValure.NaturalDocs.Engine.Topics;
using GregValure.NaturalDocs.Engine.TopicTypes;


namespace GregValure.NaturalDocs.Engine.Output.Builders
	{
	public partial class HTML
		{

		// Group: Functions
		// __________________________________________________________________________


		/* Function: BuildClassFile
		 * Builds an output file based on a class.  The accessor should NOT hold a lock on the database.  This will also
		 * build the metadata files.
		 */
		protected void BuildClassFile (int classID, CodeDB.Accessor accessor, CancelDelegate cancelDelegate)
			{
			#if DEBUG
			if (accessor.LockHeld != CodeDB.Accessor.LockType.None)
				{  throw new Exception ("Shouldn't call BuildClassFile() when the accessor already holds a database lock.");  }
			#endif

			accessor.GetReadOnlyLock();
			bool haveDBLock = true;

			try
				{
				ClassString classString = accessor.GetClassByID(classID);
				List<Topic> topics = accessor.GetTopicsInClass(classID, cancelDelegate);
				
				if (cancelDelegate())
					{  return;  }
				
					
				// Delete the file if there are no topics.

				if (topics.Count == 0)
				   {
				   accessor.ReleaseLock();
				   haveDBLock = false;
					
				   DeleteOutputFileIfExists(Class_OutputFile(classString));
				   DeleteOutputFileIfExists(Class_ToolTipsFile(classString));
				   DeleteOutputFileIfExists(Class_SummaryFile(classString));
				   DeleteOutputFileIfExists(Class_SummaryToolTipsFile(classString));

				   lock (writeLock)
				      {
				      if (classFilesWithContent.Remove(classID) == true)
				         {  buildFlags |= BuildFlags.BuildMenu;  }
				      }
				   }

				
				// Build the file if it has topics
				
				else
				   {

				   // Get links and their targets

					// We can't skip looking up classes and contexts here.  Later code will be trying to compare generated 
					// links to the ones in this list and that requires them having all their properties.
				   IList<Link> links = accessor.GetLinksInClass(classID, cancelDelegate);

				   if (cancelDelegate())
				      {  return;  }

				   IDObjects.SparseNumberSet linkTargetIDs = new IDObjects.SparseNumberSet();

				   foreach (Link link in links)
				      {
				      if (link.IsResolved)
				         {  linkTargetIDs.Add(link.TargetTopicID);  }
				      }

				   IList<Topic> linkTargets = accessor.GetTopicsByID(linkTargetIDs, cancelDelegate);

				   if (cancelDelegate())
				      {  return;  }

					// We also need to get any links appearing inside the link targets.  Wut?  When you have a resolved link, 
					// a tooltip shows up when you hover over it.  The tooltip is built from the link targets we just retrieved.  
					// However, if the summary appearing in the tooltip contains any Natural Docs links, we need to know if
					// they're resolved and how to know what text to show (originaltext, named links, etc.)  Links don't store
					// which topic they appear in, but they do store the file, so gather the file IDs of the link targets that
					// have Natural Docs links in the summaries and get all the links in those files.

					// Links also store which class they appear in, so why not do this by class instead of by file?  Because a 
					// link could be to something global, and the global scope could potentially have a whole hell of a lot of 
					// content, depending on the project and language.  While there can also be some epically long files, the
					// chances of that are less on average so we stick with doing this by file.

					IDObjects.SparseNumberSet inceptionFileIDs = new IDObjects.SparseNumberSet();

					foreach (Topic linkTarget in linkTargets)
					   {
					   if (linkTarget.Summary != null && linkTarget.Summary.IndexOf("<link type=\"naturaldocs\"") != -1)
					      {  inceptionFileIDs.Add(linkTarget.FileID);  }
					   }

					IList<Link> inceptionLinks = null;
					
					if (!inceptionFileIDs.IsEmpty)
					   {  
					   // Can't skip looking up classes and contexts here either.
					   inceptionLinks = accessor.GetNaturalDocsLinksInFiles(inceptionFileIDs, cancelDelegate);  
					   }

				   if (cancelDelegate())
				      {  return;  }

				   accessor.ReleaseLock();
				   haveDBLock = false;


				   // Build the HTML for the list of topics

					MergeClassTopics(topics);

				   StringBuilder html = new StringBuilder("\r\n\r\n");
				   HTMLTopic topicBuilder = new HTMLTopic(this);

				   // We don't put embedded topics in the output, so we need to find the last non-embedded one so
				   // that the "last" CSS tag is correctly applied.
				   int lastNonEmbeddedTopic = topics.Count - 1;
				   while (lastNonEmbeddedTopic > 0 && topics[lastNonEmbeddedTopic].IsEmbedded == true)
				      {  lastNonEmbeddedTopic--;  }

				   for (int i = 0; i <= lastNonEmbeddedTopic; i++)
				      {  
				      string extraClass = null;

				      if (i == 0)
				         {  extraClass = "first";  }
				      else if (i == lastNonEmbeddedTopic)
				         {  extraClass = "last";  }

				      if (topics[i].IsEmbedded == false)
				         {
							// xxx needs to prefer class links if possible, since these will go to files
				         topicBuilder.Build(topics[i], links, linkTargets, html, topics, i + 1, extraClass);  
				         html.Append("\r\n\r\n");
				         }
				      }
							

				   // Build the full HTML files

				   Path outputPath = Class_OutputFile(classString);
				   string title = topics[0].Symbol.LastSegment;

				   BuildFile(outputPath, title, html.ToString(), PageType.Content);


				   // Build the tooltips file

				   using (System.IO.StreamWriter file = CreateTextFileAndPath(Class_ToolTipsFile(classString)))
				      {
				      file.Write("NDContentPage.OnToolTipsLoaded({");

				      #if DONT_SHRINK_FILES
				         file.WriteLine();
				      #endif

				      for (int i = 0; i < linkTargets.Count; i++)
				         {
				         Topic topic = linkTargets[i];
				         string toolTipHTML = topicBuilder.BuildToolTip(topic, inceptionLinks);

				         if (toolTipHTML != null)
				            {
				            #if DONT_SHRINK_FILES
				               file.Write("   ");
				            #endif

				            file.Write(topic.TopicID);
				            file.Write(":\"");
				            file.Write(toolTipHTML.StringEscape());
				            file.Write('"');

				            if (i != linkTargets.Count - 1)
				               {  file.Write(',');  }

					           #if DONT_SHRINK_FILES
				               file.WriteLine();
								#endif
								}
							}

						#if DONT_SHRINK_FILES
							file.Write("   ");
						#endif
				      file.Write("});");
				      }

				   // Build summary and summary tooltips metadata files

				   HTMLSummary summaryBuilder = new HTMLSummary(this);
				   summaryBuilder.Build(topics, links, title, 
				                               Class_OutputFileHashPath(classString), Class_SummaryFile(classString), 
														 Class_SummaryToolTipsFile(classString));

				   lock (writeLock)
				      {
				      if (classFilesWithContent.Add(classID) == true)
				         {  buildFlags |= BuildFlags.BuildMenu;  }
				      }
				   }
				}
				
			finally
				{ 
				if (haveDBLock)
					{  accessor.ReleaseLock();  }
				}
			}



		// Group: Merging Functions
		// __________________________________________________________________________


		/* Function: MergeClassTopics
		 * Takes a list of <Topics> that come from the same class but multiple source files and combines them into a
		 * single coherent list.  It assumes all topics from a single file will be consecutive, but otherwise the groups of
		 * topics can be in any order.
		 */
		public void MergeClassTopics (List<Topic> topics)
			{
			if (topics.Count == 0)
				{  return;  }

			#if DEBUG
			// Validate that they're all from the same class and that all of a file's topics are consecutive.
			ClassString classString = topics[0].ClassString;
			int currentFileID = topics[0].FileID;
			IDObjects.NumberSet previousFileIDs = new IDObjects.NumberSet();

			for (int i = 1; i < topics.Count; i++)
				{
				if (topics[i].ClassString != classString)
					{  throw new Exception("All topics passed to MergeClassTopics() must have the same class string.");  }

				if (topics[i].FileID != currentFileID)
					{
					if (previousFileIDs.Contains(topics[i].FileID))
						{  throw new Exception("MergeClassTopics() requires all topics that share a file ID be consecutive.");  }

					previousFileIDs.Add(currentFileID);
					currentFileID = topics[i].FileID;
					}
				}
			#endif

			// If the first and last topic have the same file ID, that means the entire list does and we can return it as is.
			if (topics[0].FileID == topics[topics.Count-1].FileID)
				{  return;  }

			var files = Engine.Instance.Files;
			var topicTypes = Engine.Instance.TopicTypes;
			var enumTopicTypeID = topicTypes.IDFromKeyword("enum");
			var groupTopicTypeID = topicTypes.IDFromKeyword("group");


			// First we have to sort the topic list by file name.  This ensures that the merge occurs consistently no matter
			// what order the files in the list are in or how the file IDs were assigned.

			List<Topic> sortedTopics = new List<Topic>(topics.Count);

			do
				{
				var lowestFile = files.FromID(topics[0].FileID);
				var lowestFileIndex = 0;
				var lastCheckedID = lowestFile.ID;

				for (int i = 1; i < topics.Count; i++)
					{
					if (topics[i].FileID != lastCheckedID)
						{
						var file = files.FromID(topics[i].FileID);

						if (Path.Compare(file.FileName, lowestFile.FileName) < 0)
							{  
							lowestFile = file;  
							lowestFileIndex = i;
							}

						lastCheckedID = file.ID;
						}
					}

				int count = 0;
				for (int i = lowestFileIndex; i < topics.Count && topics[i].FileID == lowestFile.ID; i++)
					{  count++;  }

				sortedTopics.AddRange( topics.GetRange(lowestFileIndex, count) );
				topics.RemoveRange(lowestFileIndex, count);
				}
			while (topics.Count > 0);


			// The topics are all in sortedTopics now, and "topics" is empty.  For clarity going forward, let's rename sortedTopics
			// to remainingTopics, since we have to move them back into topics now.

			List<Topic> remainingTopics = sortedTopics;
			sortedTopics = null;  // for safety

		
			// Find the best topic to serve as the class topic.

			int bestClassIndex = 0;
			long bestClassScore = 0;

			for (int i = 0; i < remainingTopics.Count; i++)
				{
				Topic topic = remainingTopics[i];

				if (Instance.TopicTypes.FromID(topic.TopicTypeID).Scope == TopicType.ScopeValue.Start)
					{
					long score = CodeDB.Manager.ScoreTopic(topic);

					if (score > bestClassScore)
						{
						bestClassIndex = i;
						bestClassScore = score;
						}
					}
				}


			// Copy the best topic in and everything that follows it in the file.  That will serve as the base for merging.

			int bestClassFileID = remainingTopics[bestClassIndex].FileID;
			int bestClassTopicCount = 1;

			for (int i = bestClassIndex + 1; i < remainingTopics.Count && remainingTopics[i].FileID == bestClassFileID; i++)
				{  bestClassTopicCount++;  }

			topics.AddRange( remainingTopics.GetRange(bestClassIndex, bestClassTopicCount) );
			remainingTopics.RemoveRange(bestClassIndex, bestClassTopicCount);


			// Delete all the other topics that define the class.  We don't need them anymore.

			int j = 0;
			while (j < remainingTopics.Count)
				{
				if (topicTypes.FromID(remainingTopics[j].TopicTypeID).Scope == TopicType.ScopeValue.Start)
					{  remainingTopics.RemoveAt(j);  }
				else
					{  j++;  }
				}


			// Merge any duplicate topics into the list.  This is used for things like header vs. source definitions in C++.

			// First we go through the primary topic list to handle removing list topics and merging individual topics into list
			// topics in the remaining topic list.  Everything else will be handled when iterating through the remaining topic list.

			int topicIndex = 0;
			while (topicIndex < topics.Count)
				{
				var topic = topics[topicIndex];

				// Ignore group topics
				if (topic.TopicTypeID == groupTopicTypeID)
					{  
					topicIndex++;  
					continue;
					}

				int embeddedTopicCount = CountEmbeddedTopics(topics, topicIndex);


				// We don't need to worry about enums until we do remaining topics.

				if (topic.TopicTypeID == enumTopicTypeID)
					{  topicIndex += 1 + embeddedTopicCount;  }


				// If it's not an enum and it's a standalone topic see if it will merge with an embedded topic in the remaining topic
				// list.  We don't have to worry about merging with standalone topics until we do the remaining topic list.

				else if (embeddedTopicCount == 0)
					{
					int duplicateIndex = FindDuplicateTopic(topic, remainingTopics);

					if (duplicateIndex == -1)
						{  topicIndex++;  }
					else if (remainingTopics[duplicateIndex].IsEmbedded &&
								  CodeDB.Manager.ScoreTopic(topic) < CodeDB.Manager.ScoreTopic(remainingTopics[duplicateIndex]))
						{  topics.RemoveAt(topicIndex);  }
					else
						{  topicIndex++;  }
					}


				// If it's not an enum and we're at a list topic, only remove it if EVERY member has a better definition in the other
				// list.  We can't pluck them out individually.  If even one is documented here that isn't documented elsewhere we
				// keep the entire thing in even if that leads to some duplicates.

				else
					{
					bool allHaveBetterMatches = true;

					for (int i = 0; i < embeddedTopicCount; i++)
						{
						Topic embeddedTopic = topics[topicIndex + 1 + i];
						int duplicateIndex = FindDuplicateTopic(embeddedTopic, remainingTopics);

						if (duplicateIndex == -1 ||
							 CodeDB.Manager.ScoreTopic(embeddedTopic) > CodeDB.Manager.ScoreTopic(remainingTopics[duplicateIndex]))
							{
							allHaveBetterMatches = false;
							break;
							}
						}

					if (allHaveBetterMatches)
						{  topics.RemoveRange(topicIndex, 1 + embeddedTopicCount);  }
					else
						{  topicIndex += 1 + embeddedTopicCount;  }
					}
				}


			// Now do a more comprehensive merge of the remaining topics into the primary topic list.

			int remainingTopicIndex = 0;
			while (remainingTopicIndex < remainingTopics.Count)
				{
				var remainingTopic = remainingTopics[remainingTopicIndex];

				// Ignore group topics
				if (remainingTopic.TopicTypeID == groupTopicTypeID)
					{  
					remainingTopicIndex++;  
					continue;
					}

				int embeddedTopicCount = CountEmbeddedTopics(remainingTopics, remainingTopicIndex);


				// If we're merging enums, the one with the most embedded topics (documented values) wins.  In practice one
				// should be documented and one shouldn't be, so this will be any number versus zero.

				if (remainingTopic.TopicTypeID == enumTopicTypeID)
					{
					int duplicateIndex = FindDuplicateTopic(remainingTopic, topics);

					if (duplicateIndex == -1)
						{  remainingTopicIndex += 1 + embeddedTopicCount;  }
					else
						{
						int duplicateEmbeddedTopicCount = CountEmbeddedTopics(topics, duplicateIndex);

						if (embeddedTopicCount > duplicateEmbeddedTopicCount ||
							 ( embeddedTopicCount == duplicateEmbeddedTopicCount &&
								CodeDB.Manager.ScoreTopic(remainingTopic) > CodeDB.Manager.ScoreTopic(topics[duplicateIndex]) ) )
							{
							topics.RemoveRange(duplicateIndex, 1 + duplicateEmbeddedTopicCount);
							topics.InsertRange(duplicateIndex, remainingTopics.GetRange(remainingTopicIndex, 1 + embeddedTopicCount));
							}

						remainingTopics.RemoveRange(remainingTopicIndex, 1 + embeddedTopicCount);
						}
					}


				// If it's not an enum and it's a standalone topic the one with the best score wins.

				else if (embeddedTopicCount == 0)
					{
					int duplicateIndex = FindDuplicateTopic(remainingTopic, topics);

					if (duplicateIndex == -1)
						{  remainingTopicIndex++;  }
					else if (CodeDB.Manager.ScoreTopic(remainingTopic) > CodeDB.Manager.ScoreTopic(topics[duplicateIndex]))
						{  
						if (topics[duplicateIndex].IsEmbedded)
							{  
							// Just leave them both in
							remainingTopicIndex++;  
							}
						else
							{
							topics[duplicateIndex] = remainingTopic;  
							remainingTopics.RemoveAt(remainingTopicIndex);
							}
						}
					else
						{  remainingTopics.RemoveAt(remainingTopicIndex);  }
					}


				// If it's not an enum and we're at a list topic, only remove it if EVERY member has a better definition in the other
				// list.  We can't pluck them out individually.  If even one is documented here that isn't documented elsewhere we
				// keep the entire thing in even if that leads to some duplicates.

				else
					{
					bool allHaveBetterMatches = true;

					for (int i = 0; i < embeddedTopicCount; i++)
						{
						Topic embeddedTopic = remainingTopics[remainingTopicIndex + 1 + i];
						int duplicateIndex = FindDuplicateTopic(embeddedTopic, topics);

						if (duplicateIndex == -1 ||
							 CodeDB.Manager.ScoreTopic(embeddedTopic) > CodeDB.Manager.ScoreTopic(topics[duplicateIndex]))
							{
							allHaveBetterMatches = false;
							break;
							}
						}

					if (allHaveBetterMatches)
						{  remainingTopics.RemoveRange(remainingTopicIndex, 1 + embeddedTopicCount);  }
					else
						{  remainingTopicIndex += 1 + embeddedTopicCount;  }
					}
				}


			// Generate groups from the topic lists.

			// Start at 1 to skip the class topic.
			// Don't group by file ID because topics from other files may have been combined into the list.
			var groupedTopics = GetTopicGroups(topics, 1, false);

			var groupedRemainingTopics = GetTopicGroups(remainingTopics);


			// Delete any empty groups.  We do this on the main group list too for consistency.

			j = 0;
			while (j < groupedTopics.Groups.Count)
				{
				if (groupedTopics.Groups[j].IsEmpty)
					{  groupedTopics.RemoveGroupAndTopics(j);  }
				else
					{  j++;  }
				}

			j = 0;
			while (j < groupedRemainingTopics.Groups.Count)
				{
				if (groupedRemainingTopics.Groups[j].IsEmpty)
					{  groupedRemainingTopics.RemoveGroupAndTopics(j);  }
				else
					{  j++;  }
				}


			// Now merge groups.  If any remaining groups match the title of an existing group, move its members to
			// the end of the existing group.

			int remainingGroupIndex = 0;
			while (remainingGroupIndex < groupedRemainingTopics.Groups.Count)
				{
				var remainingGroup = groupedRemainingTopics.Groups[remainingGroupIndex];
				bool merged = false;

				if (remainingGroup.Title != null)
					{
					for (int groupIndex = 0; groupIndex < groupedTopics.Groups.Count; groupIndex++)
						{
						if (groupedTopics.Groups[groupIndex].Title == remainingGroup.Title)
							{
							groupedRemainingTopics.MergeGroupInto(remainingGroupIndex, groupedTopics, groupIndex);
							merged = true;
							break;
							}
						}
					}

				if (merged == false)
					{  remainingGroupIndex++;  }
				}


			// Move any groups with titles that didn't match to the other list.  We insert it after the last group
			// of the same dominant type so function groups stay with other function groups, variable groups stay
			// with other variable groups, etc.

			remainingGroupIndex = 0;
			while (remainingGroupIndex < groupedRemainingTopics.Groups.Count)
				{
				var remainingGroup = groupedRemainingTopics.Groups[remainingGroupIndex];

				if (remainingGroup.Title != null)
					{
					int bestMatchIndex = -1;

					// Walk the list backwards because we want it to be after the last group of the type, not the first.
					for (int i = groupedTopics.Groups.Count - 1; i >= 0; i--)
						{
						if (groupedTopics.Groups[i].DominantTypeID == remainingGroup.DominantTypeID)
							{
							bestMatchIndex = i;
							break;
							}
						}

					if (bestMatchIndex == -1)
						{  
						// Just add them to the end if nothing matches.
						groupedRemainingTopics.MoveGroupTo(remainingGroupIndex, groupedTopics);  
						}
					else
						{  groupedRemainingTopics.MoveGroupTo(remainingGroupIndex, groupedTopics, bestMatchIndex + 1);  }
					}
				else
					{  remainingGroupIndex++;  }
				}


			// Now we're left with topics that are not in groups.  See if the list contains any titled groups at all.

			bool groupsWithTitles = false;

			foreach (var group in groupedTopics.Groups)
				{
				if (group.Title != null)
					{
					groupsWithTitles = true;
					break;
					}
				}


			// If there's no titles we can just append the remaining topics as is.

			if (groupsWithTitles == false)
				{
				groupedTopics.Topics.AddRange(groupedRemainingTopics.Topics);
				}


			// If there are titled groups, see if we can add them to the end of existing groups.  However, only do
			// this if TitleMatchesType is set.  It's okay to put random functions into the group "Functions" but
			// not into something more specific.  If there aren't appropriate groups to do this with, create new ones.

			else
				{
				// We don't care about the remaining groups anymore so we can just work directly on the topics.
				remainingTopics = groupedRemainingTopics.Topics;
				groupedRemainingTopics = null;  // for safety

				while (remainingTopics.Count > 0)
					{
					int type = remainingTopics[0].TopicTypeID;
					int matchingGroupIndex = -1;

					for (int i = groupedTopics.Groups.Count - 1; i >= 0; i--)
						{
						if (groupedTopics.Groups[i].DominantTypeID == type && 
							 groupedTopics.Groups[i].TitleMatchesType)
							{  
							matchingGroupIndex = i;
							break;
							}
						}

					// Create a new group if there's no existing one we can use.
					if (matchingGroupIndex == -1)
						{
						Topic generatedTopic = new Topic();
						generatedTopic.TopicID = 0;
						generatedTopic.Title = Engine.Instance.TopicTypes.FromID(type).PluralDisplayName;
						generatedTopic.Symbol = SymbolString.FromPlainText_ParenthesesAlreadyRemoved(generatedTopic.Title);
						generatedTopic.ClassString = topics[0].ClassString;
						generatedTopic.ClassID = topics[0].ClassID;
						generatedTopic.TopicTypeID = Engine.Instance.TopicTypes.IDFromKeyword("group");
						generatedTopic.FileID = topics[0].FileID;
						generatedTopic.LanguageID = topics[0].LanguageID;

						groupedTopics.Topics.Add(generatedTopic);
						groupedTopics.CreateGroup(groupedTopics.Topics.Count - 1, 1);

						matchingGroupIndex = groupedTopics.Groups.Count - 1;
						}

					j = 0;
					while (j < remainingTopics.Count)
						{
						var remainingTopic = remainingTopics[j];
						if (remainingTopic.TopicTypeID == type)
							{
							groupedTopics.AppendToGroup(matchingGroupIndex, remainingTopic);
							remainingTopics.RemoveAt(j);
							}
						else
							{  j++;  }
						}
					}
				}
			}


		/* Function: FindDuplicateTopic
		 * Returns the index of a topic that defines the same code element as the passed one, or -1 if there isn't
		 * any.  Topics are considered duplicates if <Language.IsSameCodeElement> returns true.
		 */
		protected int FindDuplicateTopic (Topic topic, IList<Topic> listToSearch)
			{
			Language language = Engine.Instance.Languages.FromID(topic.LanguageID);

			for (int i = 0; i < listToSearch.Count; i++)
				{
				if (language.IsSameCodeElement(topic, listToSearch[i]))
					{  return i;  }
				}

			return -1;
			}


		/* Function: CountEmbeddedTopics
		 * Returns the number of embedded topics that follow the one at list index.
		 */
		protected int CountEmbeddedTopics (IList<Topic> topicList, int index)
			{
			#if DEBUG
			if (topicList[index].IsEmbedded)
				{  throw new Exception("The topic at the index passed to CountEmbeddedTopics() must not itself be embedded.");  }
			#endif

			int endOfEmbeddedTopics = index + 1;

			while (endOfEmbeddedTopics < topicList.Count && topicList[endOfEmbeddedTopics].IsEmbedded)
				{  endOfEmbeddedTopics++;  }

			return endOfEmbeddedTopics - index - 1;
			}


		/* Function: GetTopicGroups
		 * Returns a list of <TopicGroups> for the passed <Topics>.
		 */
		protected GroupedTopics GetTopicGroups (List<Topic> topics, int startingIndex = 0, bool groupByFileID = true)
			{
			GroupedTopics groupedTopics = new GroupedTopics(topics);

			int groupTopicTypeID = Engine.Instance.TopicTypes.IDFromKeyword("group");

			int i = startingIndex;
			while (i < topics.Count)
				{
				int fileID = topics[i].FileID;
				int groupStart = i;
				int groupCount = 1;

				i++;

				while (i < topics.Count && 
							(topics[i].FileID == fileID || !groupByFileID) && 
							topics[i].TopicTypeID != groupTopicTypeID)
					{
					groupCount++;
					i++;
					}

				groupedTopics.CreateGroup(groupStart, groupCount);
				}

			return groupedTopics;
			}



		// Group: Path Functions
		// __________________________________________________________________________


		/* Function: Class_OutputFolder
		 * 
		 * Returns the output folder for class files, optionally for the passed language and partial symbol within it.
		 * 
		 * - If language isn't specified, it returns the output folder for all class files.
		 * - If language is specified but the symbol is not, it returns the output folder for all class files of that language.
		 * - If language and partial symbol are specified, it returns the output folder for that symbol.
		 */
		public Path Class_OutputFolder (Language language = null, SymbolString partialSymbol = default(SymbolString))
			{
			StringBuilder result = new StringBuilder(OutputFolder);
			result.Append("/classes");  

			if (language != null)
				{
				result.Append('/');
				result.Append(language.SimpleIdentifier);
					
				if (partialSymbol != null)
					{
					result.Append('/');
					string pathString = partialSymbol.FormatWithSeparator('/');
					result.Append(SanitizePath(pathString));
					}
				}

			return result.ToString();
			}


		/* Function: Class_OutputFolderHashPath
		 * Returns the hash path of the output folder for class files, optionally for the passed language and partial symbol 
		 * within.  The hash path will always include a trailing symbol so that the file name can simply be concatenated.
		 * 
		 * - If language isn't specified, it returns null since there is no common prefix for all class paths.
		 * - If language is specified but the symbol is not, it returns the prefix for all class paths of that language.
		 * - If language and partial symbol are specified, it returns the hash path for that symbol.
		 */
		public string Class_OutputFolderHashPath (Language language = null, SymbolString partialSymbol = default(SymbolString))
			{
			if (language == null)
				{  return null;  }

			StringBuilder result = new StringBuilder();

			result.Append(language.SimpleIdentifier);
			result.Append("Class:");

			if (partialSymbol != null)
				{
				string memberOperator = language.MemberOperator;

				// We only support :: and . in hash paths.  Default to . for anything else.
				if (memberOperator != "::")
					{  memberOperator = ".";  }

				string pathString = partialSymbol.FormatWithSeparator(memberOperator);
				result.Append(SanitizePath(pathString));
				result.Append(memberOperator);
				}

			return result.ToString();
			}


		/* Function: Class_OutputFile
		 * Returns the path of the output file generated for the passed class.
		 */
		public Path Class_OutputFile (ClassString classString)
			{
			var language = Engine.Instance.Languages.FromID(classString.LanguageID);

			return Class_OutputFolder(language, classString.Symbol.WithoutLastSegment) + '/' + 
						Class_OutputFileNameOnly(classString);
			}


		/* Function: Class_OutputFileHashPath
		 * Returns the hash path of the passed class.
		 */
		public string Class_OutputFileHashPath (ClassString classString)
			{
			var language = Engine.Instance.Languages.FromID(classString.LanguageID);

			// OutputFolderHashPath already includes the trailing separator so we can just concatenate them.
			return Class_OutputFolderHashPath(language, classString.Symbol.WithoutLastSegment) +
						Class_OutputFileNameOnlyHashPath(classString);
			}


		/* Function: Class_OutputFileNameOnly
		 * Returns the output file name of the passed class.  Any scope attached to it will be ignored and not included in 
		 * the result.
		 */
		public Path Class_OutputFileNameOnly (ClassString classString)
			{
			string nameString = classString.Symbol.LastSegment;
			return SanitizePath(nameString, true) + ".html";
			}


		/* Function: Class_OutputFileNameOnlyHashPath
		 * Returns the hash path of the passed class.  Any scope attached to it will be ignored and not included in the result.
		 */
		public string Class_OutputFileNameOnlyHashPath (ClassString classString)
			{
			string nameString = classString.Symbol.LastSegment;
			return SanitizePath(nameString);
			}


		/* Function: Class_ToolTipsFile
		 * Returns the tooltips file path of the output file generated for the passed class.
		 */
		public Path Class_ToolTipsFile (ClassString classString)
			{
			var language = Engine.Instance.Languages.FromID(classString.LanguageID);

			return Class_OutputFolder(language, classString.Symbol.WithoutLastSegment) + '/' + 
						Class_ToolTipsFileNameOnly(classString);
			}


		/* Function: Class_ToolTipsFileNameOnly
		 * Returns the tooltips file name of the passed class.  Any scope attached to it will be ignored and not included 
		 * in the result.
		 */
		public Path Class_ToolTipsFileNameOnly (ClassString classString)
			{
			string nameString = classString.Symbol.LastSegment;
			return SanitizePath(nameString, true) + "-ToolTips.js";
			}


		/* Function: Class_SummaryFile
		 * Returns the summary file path of the passed class.
		 */
		public Path Class_SummaryFile (ClassString classString)
			{
			var language = Engine.Instance.Languages.FromID(classString.LanguageID);

			return Class_OutputFolder(language, classString.Symbol.WithoutLastSegment) + '/' + 
						Class_SummaryFileNameOnly(classString);
			}


		/* Function: Class_SummaryFileNameOnly
		 * Returns the summary file name of the class.  Any scope attached to it will be ignored and not included in the result.
		 */
		public Path Class_SummaryFileNameOnly (ClassString classString)
			{
			string nameString = classString.Symbol.LastSegment;
			return SanitizePath(nameString, true) + "-Summary.js";
			}


		/* Function: Class_SummaryToolTipsFile
		 * Returns the summary tooltips file path of the passed class.
		 */
		public Path Class_SummaryToolTipsFile (ClassString classString)
			{
			var language = Engine.Instance.Languages.FromID(classString.LanguageID);

			return Class_OutputFolder(language, classString.Symbol.WithoutLastSegment) + '/' + 
						Class_SummaryToolTipsFileNameOnly(classString);
			}


		/* Function: Class_SummaryToolTipsFileNameOnly
		 * Returns the summary tooltips file name of the passed class.  Any scope attached to it will be ignored and not 
		 * included in the result.
		 */
		public Path Class_SummaryToolTipsFileNameOnly (ClassString classString)
			{
			string nameString = classString.Symbol.LastSegment;
			return SanitizePath(nameString, true) + "-SummaryToolTips.js";
			}

		}
	}

