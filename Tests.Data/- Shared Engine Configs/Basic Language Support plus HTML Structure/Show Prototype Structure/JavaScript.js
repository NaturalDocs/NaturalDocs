
function AddPrototypeBlockLabels ()
	{
	var sections = document.getElementsByClassName("PSection");

	for (var s = 0; s < sections.length; s++)
		{
		var section = sections[s];

		  // [0] should be PSection, [1] should be the type
		var sectionClass = section.classList[1];

		if (sectionClass == "PPlainSection")
			{
			var label = document.createElement("div");
			label.className = "PBlockLabel";
			label.innerHTML = sectionClass;

			section.appendChild(label);
			}
		else if (sectionClass == "PParameterSection")
			{
			var childBlocks = section.getElementsByTagName("div");

			for (var c = 0; c < childBlocks.length; c++)
				{
				var childBlock = childBlocks[c];
				var childBlockClass = childBlock.classList[0];

				// Filtering out PBlockLabel is needed because getElementsByTagName() returns a live list, meaning
				// anything we add here will be added to the array creating an infinite loop if we don't.
				if (childBlockClass != "PBlockLabel" &&
					childBlockClass != "PParameterCells")
					{
					var label = document.createElement("div");
					label.className = "PBlockLabel";
					label.innerHTML = childBlockClass;

					childBlock.appendChild(label);
					}
				}
			}
		}
	}