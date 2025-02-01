/*
 * Class: CodeClear.NaturalDocs.Engine.ClassView
 * ____________________________________________________________________________
 *
 * A static class that merges all the <Topics> from a class into one coherent list, even if they come from multiple files.
 *
 *
 * Threading: Thread Safe
 *
 *		This class can be used by more than one thread at a time.
 *
 */

// This file is part of Natural Docs, which is Copyright © 2003-2025 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using CodeClear.NaturalDocs.Engine.Languages;
using CodeClear.NaturalDocs.Engine.Symbols;
using CodeClear.NaturalDocs.Engine.Topics;


namespace CodeClear.NaturalDocs.Engine
	{
	public static class ClassView
		{

		// Group: Functions
		// __________________________________________________________________________


		/* Function: Merge
		 *
		 * Takes a list of <Topics> that come from the same class but multiple source files and rearranges them into a
		 * single coherent list.  Some topics may be removed or merged with others.  The original topic list will be changed.
		 *
		 * Each file's topics should appear consecutively in the list and ideally in source order.  The order of the files is not
		 * important but should ideally be consistent from one run to the next.
		 *
		 * It's possible for this function to reduce the number of topics to zero.  For example, if defining classes with a list
		 * topic, the list topic itself will be removed.  You should be able to handle this and treat it as if the topic list had
		 * no content.
		 */
		public static void Merge (ref List<Topic> topics, Engine.Instance engineInstance)
			{
			try
				{
				var files = engineInstance.Files;
				var commentTypes = engineInstance.CommentTypes;


				// Filter out any list topics that define members of a hierarchy.  If someone documents classes as part of a list,
				// we only want pages for the individual members, not the list topic.

				for (int i = 0; i < topics.Count; /* no auto-increment */)
					{
					bool remove = false;

					if (topics[i].IsList)
						{
						var commentType = commentTypes.FromID(topics[i].CommentTypeID);

						if (commentType.InHierarchy)
							{  remove = true;  }
						}

					if (remove)
						{  topics.RemoveAt(i);  }
					else
						{  i++;  }
					}

				if (topics.Count == 0)
					{  return;  }


				// Validate that they're all from the same class and that all of a file's topics are consecutive.

				#if DEBUG
				int classID = topics[0].ClassID;
				ClassString classString = topics[0].ClassString;

				if (classID == 0)
					{  throw new Exception("All topics passed to Merge() must have a class ID set.");  }

				int currentFileID = topics[0].FileID;
				IDObjects.NumberSet previousFileIDs = new IDObjects.NumberSet();

				for (int i = 1; i < topics.Count; i++)
					{
					if (topics[i].ClassID != classID || topics[i].ClassString != classString)
						{  throw new Exception("All topics passed to Merge() must have the same class string and ID.");  }

					if (topics[i].FileID != currentFileID)
						{
						if (previousFileIDs.Contains(topics[i].FileID))
							{  throw new Exception("Merge() requires all topics that share a file ID be consecutive.");  }

						previousFileIDs.Add(currentFileID);
						currentFileID = topics[i].FileID;
						}
					}
				#endif


				// See if there's multiple source files by comparing the first and last topics' file IDs.  If there's only one source file we'll be
				// able to skip some steps.

				bool multipleSourceFiles = (topics[0].FileID != topics[topics.Count-1].FileID);

				List<Topic> remainingTopics = null;

				if (multipleSourceFiles)
					{
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

					remainingTopics = sortedTopics;
					sortedTopics = null;  // for safety


					// Find the best topic to serve as the class definition.

					Topic bestDefinition = remainingTopics[0];
					int bestDefinitionIndex = 0;

					for (int i = 1; i < remainingTopics.Count; i++)
						{
						Topic topic = remainingTopics[i];

						if (topic.DefinesClass && engineInstance.Links.IsBetterClassDefinition(bestDefinition, topic))
							{
							bestDefinition = topic;
							bestDefinitionIndex = i;
							}
						}


					// Copy the best definition in and everything that follows it in the file.  That will serve as the base for merging.

					int bestDefinitionTopicCount = 1;

					for (int i = bestDefinitionIndex + 1; i < remainingTopics.Count && remainingTopics[i].FileID == bestDefinition.FileID; i++)
						{  bestDefinitionTopicCount++;  }

					topics.AddRange( remainingTopics.GetRange(bestDefinitionIndex, bestDefinitionTopicCount) );
					remainingTopics.RemoveRange(bestDefinitionIndex, bestDefinitionTopicCount);

					} // if multipleSourceFiles


				// Make sure the first topic isn't embedded so that classes documented in lists still appear correctly.

				if (topics[0].IsEmbedded)
					{
					topics[0] = topics[0].Duplicate();
					topics[0].IsEmbedded = false;
					}


				// Delete all the other topics that define the class.  We don't need them anymore.

				for (int i = 1; i < topics.Count; /* don't auto increment */)
					{
					if (topics[i].DefinesClass)
						{  topics.RemoveAt(i);  }
					else
						{  i++;  }
					}

				if (multipleSourceFiles)
					{
					for (int i = 0; i < remainingTopics.Count; /* don't auto increment */)
						{
						if (remainingTopics[i].DefinesClass)
							{  remainingTopics.RemoveAt(i);  }
						else
							{  i++;  }
						}


					// Now merge the remaining topics into the main list.

					// We loop through this process one file at a time in case some topics have to be merged that aren't present in the
					// base we chose.  For example, File A has FunctionA but not FunctionZ.  File B and File C both have FunctionZ and
					// they need to be merged with each other.  If we only did one pass comparing all the remaining topics to the base
					// we wouldn't see that.

					while (remainingTopics.Count > 0)
						{
						int fileID = remainingTopics[0].FileID;


						// First pick out and merge duplicates.  This is used for things like combining header and source definitions in C++.

						for (int remainingTopicIndex = 0;
							  remainingTopicIndex < remainingTopics.Count && remainingTopics[remainingTopicIndex].FileID == fileID;
							  /* no auto-increment */)
							{
							var remainingTopic = remainingTopics[remainingTopicIndex];

							// We're ignoring group topics for now.  They stay in remainingTopics.
							if (remainingTopic.IsGroup)
								{
								remainingTopicIndex++;
								continue;
								}

							int embeddedTopicCount = CountEmbeddedTopics(remainingTopics, remainingTopicIndex);


							// If we're merging enums, the one with the most embedded topics (documented values) wins.  In practice one
							// should be documented and one shouldn't be, so this should usually be any number versus zero.

							if (remainingTopic.IsEnum)
								{
								int duplicateIndex = FindDuplicateTopic(remainingTopic, topics, engineInstance);

								if (duplicateIndex == -1)
									{  remainingTopicIndex += 1 + embeddedTopicCount;  }
								else
									{
									int duplicateEmbeddedTopicCount = CountEmbeddedTopics(topics, duplicateIndex);

									if (embeddedTopicCount > duplicateEmbeddedTopicCount ||
										 ( embeddedTopicCount == duplicateEmbeddedTopicCount &&
											engineInstance.Links.IsBetterTopicDefinition(remainingTopic, topics[duplicateIndex]) == false ) )
										{
										topics.RemoveRange(duplicateIndex, 1 + duplicateEmbeddedTopicCount);
										topics.InsertRange(duplicateIndex, remainingTopics.GetRange(remainingTopicIndex, 1 + embeddedTopicCount));
										}

									remainingTopics.RemoveRange(remainingTopicIndex, 1 + embeddedTopicCount);
									}
								}


							// If it's not an enum and it's a standalone topic, the one with the best score wins.

							else if (embeddedTopicCount == 0)
								{
								int duplicateIndex = FindDuplicateTopic(remainingTopic, topics, engineInstance);

								if (duplicateIndex == -1)
									{  remainingTopicIndex++;  }
								else if (engineInstance.Links.IsBetterTopicDefinition(remainingTopic, topics[duplicateIndex]) == false)
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


							// If it's not an enum and we're at a list topic, leave it for now.  We only want to remove it if EVERY member has
							// a better definition, and those definitions can be in different files, so wait until the list is fully combined.

							else
								{  remainingTopicIndex += 1 + embeddedTopicCount;  }

							}


						// Generate groups from the topic lists.

						// Start at 1 to skip the class topic.
						var topicGroups = GetTopicGroups(topics, startingIndex: 1);

						var remainingTopicGroups = GetTopicGroups(remainingTopics, limitToFileID: fileID);


						// Now merge groups.

						int remainingGroupIndex = 0;
						while (remainingGroupIndex < remainingTopicGroups.Groups.Count)
							{
							var remainingGroup = remainingTopicGroups.Groups[remainingGroupIndex];
							bool merged = false;

							// If the group is empty because all its members were merged as duplicates, just delete it.
							if (remainingGroup.IsEmpty)
								{
								remainingTopicGroups.RemoveGroupAndTopics(remainingGroupIndex);
								merged = true;
								}

							// If it matches the title of an existing group, move its members to the end of the existing group.
							else if (remainingGroup.Title != null)
								{
								for (int groupIndex = 0; groupIndex < topicGroups.Groups.Count; groupIndex++)
									{
									if (topicGroups.Groups[groupIndex].Title == remainingGroup.Title)
										{
										remainingTopicGroups.MergeGroupInto(remainingGroupIndex, topicGroups, groupIndex);
										merged = true;
										break;
										}
									}

								// If the group had a title but didn't match one on the other list, insert it after the last group of the same
								// dominant type so function groups stay with other function groups, variable groups stay with other variable
								// groups, etc.
								if (merged == false)
									{
									int bestMatchIndex = -1;

									// Walk the list backwards because we want it to be after the last group of the type, not the first.
									for (int i = topicGroups.Groups.Count - 1; i >= 0; i--)
										{
										if (topicGroups.Groups[i].DominantTypeID == remainingGroup.DominantTypeID)
											{
											bestMatchIndex = i;
											break;
											}
										}

									if (bestMatchIndex == -1)
										{
										// Just add the group to the end if nothing matches.
										remainingTopicGroups.MoveGroupTo(remainingGroupIndex, topicGroups);
										}
									else
										{
										remainingTopicGroups.MoveGroupTo(remainingGroupIndex, topicGroups, bestMatchIndex + 1);
										}

									merged = true;
									}
								}

							if (!merged)
								{  remainingGroupIndex++;  }
							}


						// Now we're left with topics that are not in titled groups, meaning the file itself had no group topics or there were
						// topics that appeared before the first one.  See if the base contains any titled groups.

						bool hasGroupsWithTitles = false;

						foreach (var group in topicGroups.Groups)
							{
							if (group.Title != null)
								{
								hasGroupsWithTitles = true;
								break;
								}
							}


						// If there's no titles we can just append the remaining topics as is.

						if (hasGroupsWithTitles == false)
							{
							int fileIDLimit = 0;

							while (fileIDLimit < remainingTopics.Count && remainingTopics[fileIDLimit].FileID == fileID)
								{  fileIDLimit++;  }

							if (fileIDLimit > 0)
								{
								topics.AddRange( remainingTopics.GetRange(0, fileIDLimit) );
								remainingTopics.RemoveRange(0, fileIDLimit);
								}
							}


						// If there are titled groups, see if we can add them to the end of existing groups.  However, only do
						// this if TitleMatchesType is set.  It's okay to put random functions into the group "Functions" but
						// not into something more specific.  If there aren't appropriate groups to do this with, create new ones.

						else
							{
							while (remainingTopics.Count > 0 && remainingTopics[0].FileID == fileID)
								{
								int type = remainingTopics[0].CommentTypeID;
								int matchingGroupIndex = -1;

								for (int i = topicGroups.Groups.Count - 1; i >= 0; i--)
									{
									if (topicGroups.Groups[i].DominantTypeID == type &&
										 topicGroups.Groups[i].TitleMatchesType)
										{
										matchingGroupIndex = i;
										break;
										}
									}

								// Create a new group if there's no existing one we can use.
								if (matchingGroupIndex == -1)
									{
									Topic generatedTopic = new Topic(engineInstance.CommentTypes);
									generatedTopic.TopicID = 0;
									generatedTopic.Title = engineInstance.CommentTypes.FromID(type).PluralDisplayName;
									generatedTopic.Symbol = SymbolString.FromPlainText_NoParameters(generatedTopic.Title);
									generatedTopic.ClassString = topics[0].ClassString;
									generatedTopic.ClassID = topics[0].ClassID;
									generatedTopic.CommentTypeID = engineInstance.CommentTypes.IDFromKeyword("group", topics[0].LanguageID);
									generatedTopic.FileID = topics[0].FileID;
									generatedTopic.LanguageID = topics[0].LanguageID;

									// In case there's nothing that defines the "group" keyword.
									if (generatedTopic.CommentTypeID != 0)
										{
										topicGroups.Topics.Add(generatedTopic);
										topicGroups.CreateGroup(topicGroups.Topics.Count - 1, 1);
										}

									matchingGroupIndex = topicGroups.Groups.Count - 1;
									}

								do
									{
									int topicsToMove = 1 + CountEmbeddedTopics(remainingTopics, 0);

									while (topicsToMove > 0)
										{
										topicGroups.AppendToGroup(matchingGroupIndex, remainingTopics[0]);
										remainingTopics.RemoveAt(0);
										topicsToMove--;
										}
									}
								while (remainingTopics.Count > 0 && remainingTopics[0].CommentTypeID == type);
								}
							}
						}


					// Now that everything's merged into one list, make another pass to merge list topics.

					for (int topicIndex = 0; topicIndex < topics.Count; /* no auto-increment */)
						{
						var topic = topics[topicIndex];

						// Ignore group topics
						if (topic.IsGroup)
							{
							topicIndex++;
							continue;
							}

						int embeddedTopicCount = CountEmbeddedTopics(topics, topicIndex);

						// Ignore single topics and enums.  Enums have embedded topics but we already handled them earlier.
						if (embeddedTopicCount == 0 || topic.IsEnum)
							{
							topicIndex += 1 + embeddedTopicCount;
							continue;
							}


						// If we're here we're at a list topic.  Compare its members with every other member in the list.  Remove standalone
						// topics if the list contains a better definition, but only remove the list if EVERY member has a better definition
						// somewhere else.  If only some do we'll leave in the whole thing and have duplicates instead of trying to pluck out
						// individual embedded topics.

						bool embeddedContainsBetterDefinitions = false;
						bool embeddedContainsNonDuplicates = false;

						for (int embeddedTopicIndex = topicIndex + 1;
							  embeddedTopicIndex < topicIndex + 1 + embeddedTopicCount;
							  embeddedTopicIndex++)
							{
							var embeddedTopic = topics[embeddedTopicIndex];
							var embeddedTopicLanguage = engineInstance.Languages.FromID(embeddedTopic.LanguageID);
							var foundDuplicate = false;

							for (int potentialDuplicateTopicIndex = 0; potentialDuplicateTopicIndex < topics.Count; /* no auto-increment */)
								{
								/* Skip ones in the list topic */
								if (potentialDuplicateTopicIndex == topicIndex)
									{
									potentialDuplicateTopicIndex += 1 + embeddedTopicCount;
									continue;
									}

								var potentialDuplicateTopic = topics[potentialDuplicateTopicIndex];

								if (embeddedTopicLanguage.Parser.IsSameCodeElement(embeddedTopic, potentialDuplicateTopic))
									{
									foundDuplicate = true;

									// If the current embedded topic is the better definition
									if (engineInstance.Links.IsBetterTopicDefinition(potentialDuplicateTopic, embeddedTopic))
										{
										embeddedContainsBetterDefinitions = true;

										// If the duplicate is also embedded, leave it alone.  Either the duplicate is going to be allowed to exist
										// because neither list can be completely removed, or it will be removed later when its own list is checked
										// for duplicates.
										if (potentialDuplicateTopic.IsEmbedded)
											{
											potentialDuplicateTopicIndex++;
											}

										// If the duplicate is not embedded we can remove it.
										else
											{
											topics.RemoveAt(potentialDuplicateTopicIndex);

											if (potentialDuplicateTopicIndex < topicIndex)
												{
												topicIndex--;
												embeddedTopicIndex--;
												}
											}
										}

									// If the potential duplicate is the better definition.  We don't need to do anything here because we're just
									// looking to see if all of them have better definitions elsewhere, which can be determined by whether this
									// group contains any better definitions or non-duplicates.
									else
										{  potentialDuplicateTopicIndex++;  }
									}

								// Not the same code element
								else
									{  potentialDuplicateTopicIndex++;  }
								}

							if (!foundDuplicate)
								{  embeddedContainsNonDuplicates = true;  }
							}


						// Now that we've checked every embedded topic against every other topic, remove the entire list only if EVERY
						// member has a better definition somewhere else, which is the same as saying it doesn't contain any better
						// topic definitions or non-duplicates.

						if (embeddedContainsBetterDefinitions == false && embeddedContainsNonDuplicates == false)
							{
							topics.RemoveRange(topicIndex, 1 + embeddedTopicCount);
							}
						else
							{
							topicIndex += 1 + embeddedTopicCount;
							}
						}

					}  // if multipleSourceFiles


				// Now that everything's merged, delete any empty groups.  We do this on the main group list for consistency,
				// since we were doing it on the remaining group list during merging.  Also, there may be new empty groups after
				// merging the list topics.

				// Start at 1 to skip the class topic.
				var groupedTopics = GetTopicGroups(topics, startingIndex: 1);

				for (int i = 0; i < groupedTopics.Groups.Count; /* don't auto increment */)
					{
					if (groupedTopics.Groups[i].IsEmpty)
						{  groupedTopics.RemoveGroupAndTopics(i);  }
					else
						{  i++;  }
					}
				}

			catch (Exception e)
				{
				// Build a message to show the class we crashed on
				if (topics != null && topics.Count >= 1 && topics[0].ClassString != null)
					{
					var topic = topics[0];

					int hierarchyID = topic.ClassString.HierarchyID;
					var hierarchy = (hierarchyID == 0 ? null : engineInstance.Hierarchies.FromID(hierarchyID));
					string hierarchyName = (hierarchy == null ? "hierarchy " +  hierarchyID : hierarchy.Name.ToLower(CultureInfo.InvariantCulture));

					bool includeLanguage = (hierarchy == null ? true : hierarchy.IsLanguageSpecific);

					StringBuilder task = new StringBuilder("Building class view for ");

					if (includeLanguage)
						{
						var language = (topic.LanguageID == 0 ? null : engineInstance.Languages.FromID(topic.LanguageID));
						string languageName = (language == null ? "language " + topic.LanguageID : language.Name);

						task.Append(languageName);
						task.Append(' ');
						}

					task.Append(hierarchyName);
					task.Append(' ');

					// Class name
					task.Append(topic.ClassString.Symbol.FormatWithSeparator('.'));

					e.AddNaturalDocsTask(task.ToString());
					}

				throw;
				}

			}



		// Group: Support Functions
		// __________________________________________________________________________


		/* Function: FindDuplicateTopic
		 * Returns the index of a topic that defines the same code element as the passed one, or -1 if there isn't
		 * any.  Topics are considered duplicates if <Language.IsSameCodeElement> returns true.
		 */
		private static int FindDuplicateTopic (Topic topic, List<Topic> listToSearch, Engine.Instance engineInstance)
			{
			Language language = engineInstance.Languages.FromID(topic.LanguageID);

			for (int i = 0; i < listToSearch.Count; i++)
				{
				if (language.Parser.IsSameCodeElement(topic, listToSearch[i]))
					{  return i;  }
				}

			return -1;
			}


		/* Function: CountEmbeddedTopics
		 * Returns the number of embedded topics that follow the one at list index.
		 */
		private static int CountEmbeddedTopics (List<Topic> topicList, int index)
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
		private static GroupedTopics GetTopicGroups (List<Topic> topics, int startingIndex = 0, int limitToFileID = 0)
			{
			GroupedTopics groupedTopics = new GroupedTopics(topics);

			int i = startingIndex;
			while (i < topics.Count &&
					  (limitToFileID == 0 || topics[i].FileID == limitToFileID))
				{
				int groupStart = i;
				int groupCount = 1;

				i++;

				while (i < topics.Count &&
						  (limitToFileID == 0 || topics[i].FileID == limitToFileID) &&
						  topics[i].IsGroup == false)
					{
					groupCount++;
					i++;
					}

				groupedTopics.CreateGroup(groupStart, groupCount);
				}

			return groupedTopics;
			}

		}
	}
