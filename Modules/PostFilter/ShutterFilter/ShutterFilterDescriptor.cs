﻿using System;
using VixenModules.OutputFilter.TaggedFilter;

namespace VixenModules.OutputFilter.ShutterFilter
{
    /// <summary>
    /// Descriptor for the Shutter Filter module.
    /// </summary>
    public class ShutterFilterDescriptor : TaggedFilterDescriptorBase<ShutterFilterModule, ShutterFilterData>
	{
		#region Private Static Fields

		/// <summary>
		/// GUID associated with the type.
		/// </summary>
		private static readonly Guid _typeId = new Guid("{72B86092-0A59-4E40-B0A8-9E5FAD0B8EB3}");

		#endregion

		#region Public Static Properties

		/// <summary>
		/// Module ID for the filter.
		/// </summary>
		public static Guid ModuleId => _typeId;

		#endregion

		#region IModuleDescriptor

		/// <summary>
		/// Refer to interface documentation.
		/// </summary>
		public override string TypeName => "Shutter Filter";

		/// <summary>
		/// Refer to interface documentation.
		/// </summary>
		public override Guid TypeId => _typeId;

		/// <summary>
		/// Refer to interface documentation.
		/// </summary>
		public override string Description => "An output filter that converts color intents into shutter commands.";

		#endregion
	}
}