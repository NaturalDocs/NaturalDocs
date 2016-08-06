/* 
 * Class: CodeClear.NaturalDocs.Engine.Topics.TopicGroup
 * ____________________________________________________________________________
 * 
 * A class to store information about a group of consecutive <Topics> in a list.
 * 
 * Topic: Restrictions
 * 
 *		- The group can start with a group topic, but it's not required.  However, it must not contain any 
 *		   group topics beyond that.
 *		- All topics must be in the same class.
 *		- It cannot contain topics that start a scope.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2016 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;
using CodeClear.NaturalDocs.Engine.Collections;
using CodeClear.NaturalDocs.Engine.TopicTypes;


namespace CodeClear.NaturalDocs.Engine.Topics
	{
	public class TopicGroup
		{

		// Group: Functions
		// __________________________________________________________________________


		/* Constructor: TopicGroup
		 * Creates a new topic group from the passed section of the list.  Make sure all topics in the group follow
		 * the <Restrictions>.
		 */
		public TopicGroup (List<Topic> topics, int startingIndex, int count)
			{
			this.topics = topics;
			this.startingIndex = startingIndex;
			this.count = count;

			if (topics[startingIndex].IsGroup)
				{  this.header = topics[startingIndex];  }

			this.recalculateProperties = true;

			Validate();
			}


		/* Function: MembersChanged
		 * Call whenever you change the members of the group.
		 */
		public void MembersChanged ()
			{
			recalculateProperties = true;
			}


		/* Function: ToString
		 * This is only available to aid debugging in Visual Studio.  When you have a list of objects it puts the result of
		 * ToString() next to each one.  You should not rely on it for anything else.
		 */
		override public string ToString ()
			{
			if (header != null)
				{  return header.Title;  }
			else
				{  return "(no header)";  }
			}



		// Group: Protected Functions
		// __________________________________________________________________________


		/* Function: Validate
		 * Makes sure all the members follow the <Restrictions>.  Throws an exception if not.
		 */
		protected void Validate ()
			{
			#if DEBUG

			if (count == 0)
				{  return;  }
		
			var classString = topics[startingIndex].ClassString;

			for (int i = startingIndex; i < startingIndex + count; i++)
				{
				if (topics[i].ClassString != classString)
					{  throw new Exception("All topics in a TopicGroup must be part of the same class.");  }
				if (topics[i].IsGroup && i != startingIndex)
					{  throw new Exception("Only the first topic in a TopicGroup may be a group topic.");  }
				if (Engine.Instance.TopicTypes.FromID(topics[i].TopicTypeID).Scope == TopicType.ScopeValue.Start)
					{  throw new Exception("TopicGroups cannot contain topics that start a scope.");  }
				}

			#endif
			}
		

		/* Function: CalculateProperties
		 * Recalculates <DominantTypeID>, <MixedTypes>, and <TitleMatchesType>.
		 */
		protected void CalculateProperties ()
			{
			// Calculate dominantTypeID and mixedTypes

			int i = startingIndex;

			if (header != null)
				{  i++;  }

			if (i >= startingIndex + count)
				{
				dominantTypeID = 0;
				mixedTypes = false;
				}
			else
				{
				// See if we can work our way through the group with only one topic type.

				Topic firstTopic = topics[i];
				int typeCount = 1;
				i++;

				while (i < startingIndex + count)
					{
					if (topics[i].TopicTypeID == firstTopic.TopicTypeID)
						{
						i++;
						typeCount++;
						}
					// We allow enum constants through without counting as a different type
					else if (firstTopic.IsEnum && topics[i].IsEmbedded)
						{
						i++;
						}
					else
						{  break;  }
					}

				if (i >= startingIndex + count)
					{
					dominantTypeID = firstTopic.TopicTypeID;
					mixedTypes = false;
					}
				else
					{			
					// Oh well.  Gather the counts for each type.  At least we avoided creating and using a dictionary for
					// the groups that don't need it.

					SafeDictionary<int, int> typeCounts = new SafeDictionary<int,int>();
					typeCounts[firstTopic.TopicTypeID] = typeCount;

					bool lastNonEmbeddedWasEnum = firstTopic.IsEnum;

					while (i < startingIndex + count)
						{
						Topic topic = topics[i];

						if (topic.IsEmbedded == false)
							{  lastNonEmbeddedWasEnum = topic.IsEnum;  }

						// Only count embedded topics towards the dominant one if they're not enum constants.
						if (topic.IsEmbedded == false || lastNonEmbeddedWasEnum == false)
							{
							if (typeCounts.ContainsKey(topic.TopicTypeID))
								{  typeCounts[topic.TopicTypeID]++;  }
							else
								{  typeCounts[topic.TopicTypeID] = 1;  }
							}

						i++;
						}

					int highestCount = 0;

					foreach (KeyValuePair<int,int> pair in typeCounts)
						{
						if (pair.Value > highestCount)
							{
							dominantTypeID = pair.Key;
							highestCount = pair.Value;
							}
						}

					mixedTypes = true;
					}
				}


			// Calculate titleMatchesType

			if (header == null || dominantTypeID == 0)
				{  titleMatchesType = false;  }
			else
				{
				var type = Engine.Instance.TopicTypes.FromID(dominantTypeID);

				titleMatchesType = (header.Title == type.Name ||
													 header.Title == type.DisplayName ||
													 header.Title == type.PluralDisplayName);
				}

			recalculateProperties = false;
			}



		// Group: Properties
		// __________________________________________________________________________


		/* Property: Header
		 * The group topic, if one exists.  Null otherwise.
		 */
		public Topic Header
			{
			get
				{  return header;  }
			}

		/* Property: Title
		 * The title of the group, if one exists.  Null otherwise.
		 */
		public string Title
			{
			get
				{
				if (header == null)
					{  return null;  }
				else
					{  return header.Title;  }
				}
			}

		/* Property: StartingIndex
		 * The index of the first member of the group.  If you change this in a way that changes which topics are
		 * part of the group (as opposed to changing it to reflect shifting indexes due to changes in the topic list)
		 * you must also call <MembersChanged()>.
		 */
		public int StartingIndex
			{
			get
				{  return startingIndex;  }
			internal set
				{  startingIndex = value;  }
			}

		/* Property: Count
		 * The number of members in the group.  If you change this, you must also call <MembersChanged()>.
		 */
		public int Count
			{
			get
				{  return count;  }
			internal set
				{  count = value;  }
			}

		/* Property: IsEmpty
		 * Whether the group has any topics other than the header.
		 */
		public bool IsEmpty
			{
			get
				{
				if (header == null)
					{  return (count == 0);  }
				else
					{  return (count == 1);  }
				}
			}

		/* Property: MixedTypes
		 * Whether this group has more than one topic type ID.
		 */
		public bool MixedTypes
			{
			get
				{  
				if (recalculateProperties)
					{  CalculateProperties();  }

				return mixedTypes;  
				}
			}

		/* Property: DominantTypeID
		 * If <MixedTypes> is true, this is the topic type ID that appears the most.  If it's false, this
		 * is the topic type ID of all the members.
		 */
		public int DominantTypeID
			{
			get
				{  
				if (recalculateProperties)
					{  CalculateProperties();  }

				return dominantTypeID;  
				}
			}

		/* Property: TitleMatchesType
		 * Whether the group's <Title> matches the name of the <DominantTypeID>.
		 */
		public bool TitleMatchesType
			{
			get
				{  
				if (recalculateProperties)
					{  CalculateProperties();  }

				return titleMatchesType;  
				}
			}


		/* Property: ParentList
		 * The list of <Topics> the group appears in.
		 */
		public List<Topic> ParentList
			{
			get
				{  return topics;  }
			internal set
				{  topics = value;  }
			}



		// Group: Variables
		// __________________________________________________________________________

		/* var: topics
		 * The list of topics the group appears in, but the entire list is not the group.
		 */
		protected List<Topic> topics;

		/* var: header
		 * The group header, or null if this is a group without one.
		 */
		protected Topic header;

		/* var: startingIndex
		 * The index of the first <Topic> in the group.  If <header> is defined, it will be the index of that.
		 */
		protected int startingIndex;

		/* var: count
		 * The number of <Topics> in the group.
		 */
		protected int count;

		/* var: mixedTypes
		 * Whether the group has more than one topic type in it.
		 */
		protected bool mixedTypes;

		/* var: dominantTypeID
		 * The topic type ID of the dominant type in the group.  If <mixedTypes> is false, all of the topics
		 * will be of this type.
		 */
		protected int dominantTypeID;

		/* var: titleMatchesType
		 * Whether the group title matches the name of the dominant topic type ID.
		 */
		protected bool titleMatchesType;

		/* var: recalculateProperties
		 * If true, it means <mixedTypes>, <dominantTypeID>, and <titleMatchesType> need to be recalculated.
		 */
		protected bool recalculateProperties;

		}
	}

