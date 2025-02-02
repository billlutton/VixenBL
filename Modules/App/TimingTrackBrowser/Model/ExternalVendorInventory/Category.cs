﻿using System.Collections.Generic;
using System.Xml.Serialization;
using Common.WPFCommon.ViewModel;

namespace VixenModules.App.TimingTrackBrowser.Model.ExternalVendorInventory
{
	public class Category : BindableBase
	{
		[XmlElement("id")]
		public string Id { get; set; }

		[XmlElement("name")]
		public string Name { get; set; }

		[XmlArray("categories")]
		[XmlArrayItem("category", typeof(Category))]
		public List<Category> Categories { get; set; }
	}
}
