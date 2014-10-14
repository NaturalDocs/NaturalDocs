#if SHOW_EXECUTION_TIME

/* 
 * Class: GregValure.NaturalDocs.CLI.ExecutionTimer
 * ____________________________________________________________________________
 * 
 * A class to time certain operations in the engine.
 * 
 */

// This file is part of Natural Docs, which is Copyright © 2003-2013 Greg Valure.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;


namespace GregValure.NaturalDocs.CLI
	{
	public class ExecutionTimer
		{
		
		// Group: Functions
		// __________________________________________________________________________
		
		public ExecutionTimer ()
			{
			timingRecords = new List<TimingRecord>();
			}

		public string StatisticsToString ()
			{
			// First just calculate the column widths so we can format them nicely.  We'll save the formatted tick counts
			// so we don't have to regenerate them later.

			string[] formattedTicks = new string[timingRecords.Count];
			int nameColumnWidth = 0;
			int ticksColumnWidth = 0;

			for (int i = 0; i < timingRecords.Count; i++)
				{
				var timingRecord = timingRecords[i];

				if (timingRecord.Ticks != -1)
					{
					formattedTicks[i] = string.Format("{0:#,0}k", (timingRecord.Ticks / 1000));

					if (formattedTicks[i].Length > ticksColumnWidth)
						{  ticksColumnWidth = formattedTicks[i].Length;  }

					int nameWidth = timingRecord.Name.Length;
					int parentIndex = GetParent(i);

					while (parentIndex != -1)
						{
						nameWidth += 2;
						parentIndex = GetParent(parentIndex);
						}

					if (nameWidth > nameColumnWidth)
						{  nameColumnWidth = nameWidth;  }
					}
				}


			// Now generate the output

			System.Text.StringBuilder output = new System.Text.StringBuilder();

			for (int i = 0; i < timingRecords.Count; i++)
				{
				var timingRecord = timingRecords[i];

				if (timingRecord.Ticks != -1)
					{
					int nameWidth = 0;
					int parentIndex = GetParent(i);

					while (parentIndex != -1)
						{
						output.Append("- ");
						nameWidth += 2;
						parentIndex = GetParent(parentIndex);
						}

					output.Append(timingRecord.Name);
					nameWidth += timingRecord.Name.Length;

					output.Append(' ', nameColumnWidth + 1 - nameWidth);
					output.Append(' ', ticksColumnWidth - formattedTicks[i].Length);

					output.Append(formattedTicks[i] + " ticks");
					output.AppendLine();
					}
				}

			return output.ToString();
			}

		/* Function: GetParent
		 * If the <TimingRecord> at index i is fully contained in another timing record, returns the index of that record.
		 * If not, returns -1.
		 */
		protected int GetParent (int i)
			{
			var childRecord = timingRecords[i];

			for (;;)
				{
				i--;

				if (i < 0)
					{  return -1;  }

				if (timingRecords[i].Contains(childRecord))
					{  return i;  }
				}
			}

		public void Start (string timerName)
			{  
			foreach (var timingRecord in timingRecords)
				{
				if (timingRecord.Name == timerName)
					{  return;  }
				}

			var newTimingRecord = new TimingRecord(timerName);
			newTimingRecord.OnStart();
			timingRecords.Add(newTimingRecord);
			}

		public void End (string timerName)
			{
			foreach (var timingRecord in timingRecords)
				{
				if (timingRecord.Name == timerName)
					{
					timingRecord.OnEnd();
					break;
					}
				}
			}

		
		
		// Group: Variables
		// __________________________________________________________________________
		
		protected List<TimingRecord> timingRecords;



		/* __________________________________________________________________________
		 * 
		 * Class: TimingRecord
		 * __________________________________________________________________________
		 */
		protected class TimingRecord
			{

			// Group: Functions

			public TimingRecord (string name)
				{
				this.name = name;

				startTicks = -1;
				endTicks = -1;
				}

			public void OnStart ()
				{  startTicks = System.DateTime.Now.Ticks;  }

			public void OnEnd ()
				{  endTicks = System.DateTime.Now.Ticks;  }

			public bool Contains (TimingRecord other)
				{
				if (!IsComplete || !other.IsComplete)
					{  return false;  }

				return (startTicks <= other.startTicks && endTicks >= other.endTicks);
				}


			// Group: Properties

			public string Name
				{
				get
					{  return name;  }
				}

			public long Ticks
				{
				get
					{
					if (startTicks != -1 && endTicks != -1 && endTicks >= startTicks)
						{  return endTicks - startTicks;  }
					else
						{  return -1;  }
					}
				}

			public bool IsComplete
				{
				get
					{  return (startTicks != -1 && endTicks != -1);  }
				}


			// Group: Variables

			protected string name;

			protected long startTicks;
			protected long endTicks;
			}
		
		}
	}

#endif