﻿#if !LW
namespace Waher.Persistence.Files.Test.Classes
#else
using Waher.Persistence.Files;
namespace Waher.Persistence.FilesLW.Test.Classes
#endif
{
	public class LocalNameSubclass1 : LocalNameBase
	{
		public int Value;
	}
}
