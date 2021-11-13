using System;

namespace Fiddler
{
	/// <summary>
	/// A builder class for <see cref="T:Fiddler.FiddlerCoreStartupSettings" />.
	/// </summary>
	// Token: 0x02000006 RID: 6
	public sealed class FiddlerCoreStartupSettingsBuilder : FiddlerCoreStartupSettingsBuilder<FiddlerCoreStartupSettingsBuilder, FiddlerCoreStartupSettings>, IFiddlerCoreStartupSettingsBuilder<FiddlerCoreStartupSettingsBuilder, FiddlerCoreStartupSettings>
	{
		/// <summary>
		/// Initializes a new instance of <see cref="T:Fiddler.FiddlerCoreStartupSettingsBuilder" />
		/// </summary>
		// Token: 0x06000061 RID: 97 RVA: 0x00003C85 File Offset: 0x00001E85
		public FiddlerCoreStartupSettingsBuilder()
			: base(new FiddlerCoreStartupSettings())
		{
		}
	}
}
