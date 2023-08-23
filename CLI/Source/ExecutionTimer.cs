/*
 * Class: CodeClear.NaturalDocs.CLI.ExecutionTimer
 * ____________________________________________________________________________
 *
 * A class to time certain operations in the engine.
 *
 */

// This file is part of Natural Docs, which is Copyright © 2003-2023 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;


namespace CodeClear.NaturalDocs.CLI
	{
	public class ExecutionTimer
		{

		// Group: Functions
		// __________________________________________________________________________

		public ExecutionTimer ()
			{
			timingRecords = new List<TimingRecord>();
			}

		public void Start (string timerName, string parentName = "Total Execution")
			{
			foreach (var timingRecord in timingRecords)
				{
				if (timingRecord.Name == timerName)
					{  return;  }
				}

			if (timerName == "Total Execution")
				{  parentName = null;  }

			var newTimingRecord = new TimingRecord(timerName, parentName);
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

		public string BuildStatistics ()
			{
			// First just calculate the column widths so we can format them nicely.  We'll save the formatted tick counts
			// so we don't have to regenerate them later.

			string[] formattedTime = new string[timingRecords.Count];
			int nameColumnWidth = 0;
			int timeColumnWidth = 0;

			for (int i = 0; i < timingRecords.Count; i++)
				{
				var timingRecord = timingRecords[i];

				formattedTime[i] = string.Format("{0:#,0}ms", timingRecord.Milliseconds);

				if (formattedTime[i].Length > timeColumnWidth)
					{  timeColumnWidth = formattedTime[i].Length;  }

				int nameWidth = timingRecord.Name.Length;
				TimingRecord parent = GetParent(timingRecord);

				while (parent != null)
					{
					nameWidth += 2;
					parent = GetParent(parent);
					}

				if (nameWidth > nameColumnWidth)
					{  nameColumnWidth = nameWidth;  }
				}


			// Now generate the output

			System.Text.StringBuilder output = new System.Text.StringBuilder();

			for (int i = 0; i < timingRecords.Count; i++)
				{
				var timingRecord = timingRecords[i];

				int nameWidth = 0;
				TimingRecord parent = GetParent(timingRecord);

				while (parent != null)
					{
					output.Append("- ");
					nameWidth += 2;
					parent = GetParent(parent);
					}

				output.Append(timingRecord.Name);
				nameWidth += timingRecord.Name.Length;

				output.Append(' ', nameColumnWidth + 2 - nameWidth);
				output.Append(' ', timeColumnWidth - formattedTime[i].Length);

				output.Append(formattedTime[i]);

				parent = GetParent(timingRecord);
				if (parent != null)
					{
					output.Append(string.Format(" {0,3}%", ((timingRecord.Milliseconds * 100) / parent.Milliseconds) ));
					}

				output.AppendLine();
				}

			return output.ToString();
			}


		/* Function: BuildCSVHeadings
		 * Returns a list of timer names suitable for serving as headings in a CSV file.
		 */
		public string BuildCSVHeadings ()
			{
			System.Text.StringBuilder output = new System.Text.StringBuilder();

			for (int i = 0; i < timingRecords.Count; i++)
				{
				var timingRecord = timingRecords[i];

				if (i > 0)
					{  output.Append(',');  }

				output.Append('"' + timingRecord.Name + " (ms)\"");
				}

			return output.ToString();
			}


		/* Function: BuildCSVValues
		 * Returns a list of timer values suitable for serving as a row in a CSV file.
		 */
		public string BuildCSVValues ()
			{
			System.Text.StringBuilder output = new System.Text.StringBuilder();

			for (int i = 0; i < timingRecords.Count; i++)
				{
				var timingRecord = timingRecords[i];

				if (i > 0)
					{  output.Append(',');  }

				output.Append(timingRecord.Milliseconds);
				}

			return output.ToString();
			}



		// Group: Support Functions
		// __________________________________________________________________________

		protected TimingRecord Get (string name)
			{
			foreach (var timingRecord in timingRecords)
				{
				if (timingRecord.Name == name)
					{  return timingRecord;  }
				}

			return null;
			}

		protected TimingRecord GetParent (TimingRecord timingRecord)
			{
			if (timingRecord.ParentName == null)
				{  return null;  }
			else
				{  return Get(timingRecord.ParentName);  }
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

			public TimingRecord (string name, string parentName = null)
				{
				this.name = name;
				this.parentName = parentName;

				stopWatch = new System.Diagnostics.Stopwatch();
				}

			public void OnStart ()
				{  stopWatch.Start();  }

			public void OnEnd ()
				{  stopWatch.Stop();  }


			// Group: Properties

			public string Name
				{
				get
					{  return name;  }
				}

			public string ParentName
				{
				get
					{  return parentName;  }
				}

			public long Milliseconds
				{
				get
					{  return stopWatch.ElapsedMilliseconds;  }
				}

			public bool IsComplete
				{
				get
					{  return !stopWatch.IsRunning;  }
				}


			// Group: Variables

			protected string name;
			protected string parentName;

			protected System.Diagnostics.Stopwatch stopWatch;
			}

		}
	}
