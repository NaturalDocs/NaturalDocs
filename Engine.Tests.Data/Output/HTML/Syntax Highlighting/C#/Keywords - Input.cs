
/*
	Topic: Keywords

		--- Code
		async bool Function (string x)
			{
			if (x == null)
				{  return true;  }
			else
				{  return false;  }
			}
		---


	Topic: Traps

		--- Code
		string my_string = "string value";
		---


	Topic: @Identifiers

		Should not be highlighted as keywords.

		--- Code
		int Function (string x)
			{
			int @true = 1;
			int @false = 0;
			string @null = "";

			if (x == @null)
				{  return @true;  }
			else
				{  return @false;  }
			}
		---

*/
