/* 
 * Class: CodeClear.NaturalDocs.Engine.Topics.GroupedTopics
 * ____________________________________________________________________________
 * 
 * A list of <Topics> divided into <TopicGroups>.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2017 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;


namespace CodeClear.NaturalDocs.Engine.Topics
	{
	public class GroupedTopics
		{

		// Group: Functions
		// __________________________________________________________________________


		/* Constructor: GroupedTopics
		 * Creates a new list with the passed topics and no grouping applied.
		 */
		public GroupedTopics (List<Topic> topics)
			{
			this.topics = topics;
			this.groups = new List<TopicGroup>();
			}


		/* Function: CreateGroup
		 * Creates a grouping in the topic list.  Groups do not have to encompass every topic but they must be created 
		 * in order and must not overlap.
		 */
		public void CreateGroup (int startingIndex, int count)
			{
			#if DEBUG
			if (groups.Count > 0)
				{
				var lastGroup = groups[groups.Count-1];

				if (startingIndex < lastGroup.StartingIndex)
					{  throw new Exception("You must add groups to GroupedTopics in order.");  }
				if (startingIndex < lastGroup.StartingIndex + lastGroup.Count)
					{  throw new Exception("Groups in GroupTopics cannot overlap.");  }
				}
			#endif

			groups.Add( new TopicGroup(topics, startingIndex, count) );
			}


		/* Function: RemoveGroupAndTopics
		 * Removes the group from the list and any <Topics> associated with it.
		 */
		public void RemoveGroupAndTopics (int groupIndex)
			{
			var groupToRemove = groups[groupIndex];

			if (groupToRemove.Count > 0)
				{  
				topics.RemoveRange(groupToRemove.StartingIndex, groupToRemove.Count);  

				for (int i = groupIndex + 1; i < groups.Count; i++)
					{  groups[i].StartingIndex -= groupToRemove.Count;  }
				}

			groups.RemoveAt(groupIndex);
			}


		/* Function: MergeGroupInto
		 * Adds the members of a group to the end of a group in another list.  The original group and its topics will be
		 * deleted.  If the original group has a header it will be discarded.
		 */
		public void MergeGroupInto (int groupIndex, GroupedTopics target, int targetGroupIndex)
			{
			#if DEBUG
			if (target == this)
				{  throw new Exception("Cannot use MergeGroupInto() to transfer members in the same list.");  }
			#endif

			var group = groups[groupIndex];

			int topicIndex_WithHeader = group.StartingIndex;
			int topicIndex_NoHeader = group.StartingIndex;
			int topicCount_WithHeader = group.Count;
			int topicCount_NoHeader = group.Count;

			if (group.Header != null)
				{
				topicIndex_NoHeader++;
				topicCount_NoHeader--;
				}

			var targetGroup = target.groups[targetGroupIndex];
			int targetTopicIndex = targetGroup.StartingIndex + targetGroup.Count;


			// Move the topics

			target.topics.InsertRange(targetTopicIndex, topics.GetRange(topicIndex_NoHeader, topicCount_NoHeader));
			topics.RemoveRange(topicIndex_WithHeader, topicCount_WithHeader);


			// Adjust the group counts and indexes

			groups.RemoveAt(groupIndex);

			for (int i = groupIndex; i < groups.Count; i++)
				{  groups[i].StartingIndex -= topicCount_WithHeader;  }

			targetGroup.Count += topicCount_NoHeader;
			targetGroup.MembersChanged();

			for (int i = targetGroupIndex + 1; i < target.groups.Count; i++)
				{  target.groups[i].StartingIndex += topicCount_NoHeader;  }
			}


		/* Function: MoveGroupTo
		 * Moves the group and its topics to the target list.  If a target group index is specified it will be inserted
		 * at that position.  If not it will be added to the end of the list.
		 */
		public void MoveGroupTo (int groupIndex, GroupedTopics target, int targetGroupIndex = -1)
			{
			#if DEBUG
			if (target == this)
				{  throw new Exception("Cannot use MoveGroupTo() to transfer members in the same list.");  }
			#endif

			if (targetGroupIndex == -1)
				{  targetGroupIndex = target.groups.Count;  }

			var group = groups[groupIndex];
			int topicIndex = group.StartingIndex;
			int topicCount = group.Count;

			int targetTopicIndex;
			if (target.groups.Count == 0)
				{  targetTopicIndex = target.topics.Count;  }
			else if (targetGroupIndex >= target.groups.Count)
				{
				var lastGroup = target.groups[ target.groups.Count - 1 ];
				targetTopicIndex = lastGroup.StartingIndex + lastGroup.Count;
				}
			else
				{  targetTopicIndex = target.groups[targetGroupIndex].StartingIndex;  }


			// Move the topics

			target.topics.InsertRange(targetTopicIndex, topics.GetRange(topicIndex, topicCount));
			topics.RemoveRange(topicIndex, topicCount);


			// Adjust the indexes of groups after the insertion.

			for (int i = groupIndex + 1; i < groups.Count; i++)
				{  groups[i].StartingIndex -= topicCount;  }

			for (int i = targetGroupIndex; i < target.groups.Count; i++)
				{  target.groups[i].StartingIndex += topicCount;  }


			// Move the group entry

			groups.RemoveAt(groupIndex);

			group.ParentList = target.topics;
			group.StartingIndex = targetTopicIndex;

			target.groups.Insert(targetGroupIndex, group);
			}


		/* Function: AppendToGroup
		 * Adds a <Topic> to the end of the group.
		 */
		public void AppendToGroup (int groupIndex, Topic topic)
			{
			var group = groups[groupIndex];

			topics.Insert(group.StartingIndex + group.Count, topic);
			group.Count++;
			group.MembersChanged();

			for (int i = groupIndex + 1; i < groups.Count; i++)
				{  groups[i].StartingIndex++;  }
			}



		// Group: Properties
		// __________________________________________________________________________


		/* Property: Topics
		 * The <Topics> in the list.
		 */
		public List<Topic> Topics
			{
			get
				{  return topics;  }
			}


		/* Property: Groups
		 * The <TopicGroups> in the list.
		 */
		public List<TopicGroup> Groups
			{
			get
				{  return groups;  }
			}



		// Group: Variables
		// __________________________________________________________________________


		/* var: topics
		 */
		protected List<Topic> topics;

		/* var: groups
		 */
		protected List<TopicGroup> groups;

		}
	}
