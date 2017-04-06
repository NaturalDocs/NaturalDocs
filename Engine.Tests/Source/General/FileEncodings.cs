
// This file is part of Natural Docs, which is Copyright © 2003-2017 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using NUnit.Framework;


namespace CodeClear.NaturalDocs.Engine.Tests.General
	{
	[TestFixture]
	public class FileEncodings : Framework.TestTypes.CommentDetection
		{

		[Test]
		public void All ()
			{
			TestFolder("General/File Encodings", "Shared ND Config/Basic Language Support");
			}

		}
	}