using Waher.Persistence.Attributes;

namespace Waher.Runtime.Settings.HostSettingObjects
{
	/// <summary>
	/// String host setting object.
	/// </summary>
	public class StringHostSetting : HostSetting
	{
		private string value = string.Empty;

		/// <summary>
		/// String host setting object.
		/// </summary>
		public StringHostSetting()
		{
		}

		/// <summary>
		/// String host setting object.
		/// </summary>
		/// <param name="Host">Host name.</param>
		/// <param name="Key">Key name.</param>
		/// <param name="Value">Value.</param>
		public StringHostSetting(string Host, string Key, string Value)
			: base(Host, Key)
		{
			this.value = Value;
		}

		/// <summary>
		/// Value.
		/// </summary>
		[DefaultValueStringEmpty]
		public string Value
		{
			get => this.value;
			set => this.value = value;
		}

		/// <summary>
		/// Gets the value of the setting, as an object.
		/// </summary>
		/// <returns>Value object.</returns>
		public override object GetValueObject()
		{
			return this.value;
		}
	}
}
