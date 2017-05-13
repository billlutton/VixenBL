﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Common.Controls;
using Common.Controls.ColorManagement.ColorPicker;
using Vixen.Sys;
using VixenModules.App.ColorGradients;
using VixenModules.Editor.EffectEditor.Converters;
using VixenModules.Effect.Effect;
using Color = System.Drawing.Color;
using Control = System.Windows.Controls.Control;
using Cursors = System.Windows.Input.Cursors;
using Image = System.Windows.Controls.Image;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;

namespace VixenModules.Editor.EffectEditor.Controls
{
	public abstract class BaseInlineGradientEditor: Control
	{

		private static readonly Type ThisType = typeof(BaseInlineGradientEditor);
		private static readonly ColorGradientToImageConverter Converter = new ColorGradientToImageConverter();
		private bool _isDiscrete;
		protected static ColorPicker.Mode Mode = ColorPicker.Mode.HSV_RGB;
		protected static ColorPicker.Fader Fader = ColorPicker.Fader.HSV_H;
		protected Canvas _canvas;
		protected readonly List<SliderPoint> _points;
		private ColorGradient _holdValue;

		static BaseInlineGradientEditor()
		{
			DefaultStyleKeyProperty.OverrideMetadata(ThisType, new FrameworkPropertyMetadata(ThisType));
		}

		protected BaseInlineGradientEditor()
		{
			InitializeStyle();
			_points = new List<SliderPoint>();
			Loaded += InlineGradientEditor_Loaded;
		}

		private void InlineGradientEditor_Loaded(object sender, RoutedEventArgs e)
		{
			_canvas = (Canvas)Template.FindName("FaderCanvas", this);
			_canvas.MouseMove += _canvas_MouseMove; ;
			_canvas.MouseDown += CanvasOnMouseDown;
			_canvas.MouseLeftButtonDown += CanvasOnMouseLeftButtonDown;
			OnComponentChanged();
			OnGradientValueChanged();
		}

		protected void OnGradientValueChanged()
		{
			ReloadPoints();
			UpdateImage(GetColorGradientValue());
		}

		private void _canvas_MouseMove(object sender, MouseEventArgs e)
		{
			if (IsMouseOverAnyHandle())
			{
				Cursor = Cursors.SizeWE;
			}
			else
			{
				Cursor = Cursors.Arrow;
			}

		}

		protected override void OnMouseDown(MouseButtonEventArgs e)
		{
			base.OnMouseDown(e);
			e.Handled = true;
		}

		private void CanvasOnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			if (e.ClickCount == 2)
			{
				if (IsMouseOverAnyHandle())
				{
					//launch the color editor.
					ShowColorPicker();
				}
				e.Handled = true;
			}
		}

		private void CanvasOnMouseDown(object sender, MouseButtonEventArgs e)
		{
			var mousePoint = Mouse.GetPosition(sender as IInputElement);
			if (mousePoint.X < 0 || mousePoint.X > _canvas.Width || mousePoint.Y < 0 || mousePoint.Y > _canvas.Height) return;
			if (IsMouseOverAnyHandle()) return;
			AddColorPoint(mousePoint.X / _canvas.Width);
			e.Handled = true;
		}

		private void OnAddItem(SliderPoint sliderPoint)
		{
			sliderPoint.Parent = _canvas;
			sliderPoint.SliderShape.Style = SliderStyle;
			sliderPoint.SliderShape.KeyDown += GradientPoint_KeyDown; ;
			sliderPoint.PropertyChanged += GradientPoint_PropertyChanged;
			sliderPoint.DragCompleted += SliderPoint_DragCompleted;
			try
			{
				_canvas.Children.Add(sliderPoint.SliderShape);
			}
			catch { }
		}

		private void OnRemoveItem(SliderPoint sliderPoint)
		{
			sliderPoint.SliderShape.KeyDown -= GradientPoint_KeyDown;
			sliderPoint.PropertyChanged -= GradientPoint_PropertyChanged;
			sliderPoint.DragCompleted -= SliderPoint_DragCompleted;
			try
			{
				_canvas.Children.Remove(sliderPoint.SliderShape);
			}
			catch { }

			sliderPoint.Dispose();
		}

		private void GradientPoint_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Delete)
			{
				DeletePoint();
				e.Handled = true;
			}
		}

		private void SliderPoint_DragCompleted(object sender, EventArgs e)
		{
			SetColorGradientValue(_holdValue);
			_holdValue = null;
		}


		private void GradientPoint_PropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName.Equals("center", StringComparison.OrdinalIgnoreCase))
			{
				if (_holdValue == null)
				{
					_holdValue = new ColorGradient(GetColorGradientValue());
				}
				SliderPoint point = sender as SliderPoint;
				if (point != null)
				{
					var pointIndexes = point.Tag as List<int>;
					var colorPoints = _holdValue.Colors;
					if (pointIndexes != null)
					{
						foreach (var index in pointIndexes)
						{
							colorPoints[index].Position = point.NormalizedPosition;
						}
					}

					UpdateImage(_holdValue);
				}
			}
		}

		#region Dependency Property Fields

		/// <summary>
		/// Identifies the <see cref="Component"/> dependency property.
		/// </summary>
		public static readonly DependencyProperty ComponentProperty = DependencyProperty.Register("Component", typeof(Object),
			ThisType, new PropertyMetadata(null, OnComponentChanged));

		public static readonly DependencyProperty SliderStyleProperty = DependencyProperty.Register("SliderStyle", typeof(Style), ThisType);

		#endregion Dependency Property Fields

		#region Properties

		public Style SliderStyle
		{
			get { return (Style)GetValue(SliderStyleProperty); }
			set { SetValue(SliderStyleProperty, value); }
		}

		public Object Component
		{
			get { return (Object)GetValue(ComponentProperty); }
			set { SetValue(ComponentProperty, value); }
		}

		protected bool IsDiscrete
		{
			get { return _isDiscrete; }
			set
			{
				_isDiscrete = value;
				UpdateImage(GetColorGradientValue());
			}
		}

		protected HashSet<Color> ValidColors { get; set; }


		#endregion

		#region Events


		#region PropertyEditingStarted Event

		/// <summary>
		/// Identifies the <see cref="PropertyEditingStarted"/> routed event.
		/// </summary>
		public static readonly RoutedEvent PropertyEditingStartedEvent =
			EffectPropertyEditorGrid.PropertyEditingStartedEvent.AddOwner(ThisType);

		/// <summary>
		/// Occurs when property editing is started.
		/// </summary>
		public event PropertyEditingEventHandler PropertyEditingStarted
		{
			add { AddHandler(PropertyEditingStartedEvent, value); }
			remove { RemoveHandler(PropertyEditingFinishedEvent, value); }
		}

		#endregion PropertyEditingStarted Event

		#region PropertyEditingFinished Event

		/// <summary>
		/// Identifies the <see cref="PropertyEditingFinished"/> routed event.
		/// </summary>
		public static readonly RoutedEvent PropertyEditingFinishedEvent =
			EffectPropertyEditorGrid.PropertyEditingFinishedEvent.AddOwner(ThisType);

		/// <summary>
		/// Occurs when property editing is finished.
		/// </summary>
		public event PropertyEditingEventHandler PropertyEditingFinished
		{
			add { AddHandler(PropertyEditingFinishedEvent, value); }
			remove { RemoveHandler(PropertyEditingFinishedEvent, value); }
		}

		#endregion PropertyEditingFinished Event

		#endregion Events

		#region PropertyDescriptor Property

		/// <summary>
		/// Identifies the <see cref="InlineGradientEditor.PropertyDescriptor"/> dependency property.
		/// </summary>
		public static readonly DependencyProperty PropertyDescriptorProperty =
			DependencyProperty.Register("PropertyDescriptor", typeof(PropertyDescriptor), ThisType);

		/// <summary>
		/// Underlying property descriptor. This is a dependency property.
		/// </summary>
		public PropertyDescriptor PropertyDescriptor
		{
			get { return (PropertyDescriptor)GetValue(PropertyDescriptorProperty); }
			set { SetValue(PropertyDescriptorProperty, value); }
		}

		#endregion PropertyDescriptor Property

		#region Property Changed Callbacks

		private static void OnComponentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var inlineGradientEditor = (BaseInlineGradientEditor)d;
			if (!inlineGradientEditor.IsInitialized)
				return;
			inlineGradientEditor.OnComponentChanged();
		}

		protected virtual void OnComponentChanged()
		{
			if (Component is BaseEffect)
			{
				var effect = (BaseEffect)Component;
				IsDiscrete = effect.HasDiscreteColors;
				if (IsDiscrete)
				{
					ValidColors = effect.GetValidColors();
				}
			}
			else if (Component is Array)
			{
				var effects = (BaseEffect[])Component;
				IsDiscrete = effects.Any(e => e.HasDiscreteColors);
				if (IsDiscrete)
				{
					ValidColors.Clear();
					foreach (var baseEffect in effects)
					{
						ValidColors.AddRange(baseEffect.GetValidColors());
					}
				}
			}
		}

		#endregion


		/// <summary>
		/// Raises the <see cref="PropertyEditingStarted"/> event.
		/// </summary>
		protected virtual void OnPropertyEditingStarted()
		{
			RaiseEvent(new PropertyEditingEventArgs(PropertyEditingStartedEvent, this, PropertyDescriptor));
		}

		/// <summary>
		/// Raises the <see cref="PropertyEditingFinished"/> event.
		/// </summary>
		protected virtual void OnPropertyEditingFinished()
		{
			RaiseEvent(new PropertyEditingEventArgs(PropertyEditingFinishedEvent, this, PropertyDescriptor));
		}


		private void InitializeStyle()
		{
			SliderStyle = new Style();
			var brush = new LinearGradientBrush(Colors.LightGray, System.Windows.Media.Color.FromRgb((byte)(Colors.LightGray.R * 0.5), (byte)(Colors.LightGray.G * 0.5), (byte)(Colors.LightGray.B * 0.5)), 0);
			SliderStyle.Setters.Add(new Setter(Shape.FillProperty, brush));
			brush = new LinearGradientBrush(Colors.Gray, System.Windows.Media.Color.FromRgb((byte)(Colors.Gray.R * 0.5), (byte)(Colors.Gray.G * 0.5), (byte)(Colors.Gray.B * 0.5)), 0);
			var trigger = new Trigger { Setters = { new Setter(Shape.FillProperty, brush) }, Value = true, Property = IsFocusedProperty };
			SliderStyle.Triggers.Add(trigger);
		}

		private bool IsMouseOverAnyHandle()
		{
			return _points.Any(q => q.IsMouseOver);
		}

		protected abstract ColorGradient GetColorGradientValue();

		protected abstract void SetColorGradientValue(ColorGradient cg);

		private void ShowColorPicker()
		{
			var selectedIndex = SelectedIndex;
			if (selectedIndex < 0) return;
			var handle = _points[selectedIndex];
			var colorPointIndexes = (List<int>)handle.Tag;
			ColorGradient holdValue;
			if (IsDiscrete)
			{
				holdValue = new ColorGradient(GetColorGradientValue());
				List<Color> selectedColors = new List<Color>();
				List<ColorPoint> colorPoints = new List<ColorPoint>();
				foreach (int index in colorPointIndexes)
				{
					ColorPoint pt = holdValue.Colors[index];
					if (pt == null)
						continue;
					colorPoints.Add(pt);
					selectedColors.Add(pt.Color.ToRGB().ToArgb());
				}

				using (DiscreteColorPicker picker = new DiscreteColorPicker())
				{
					picker.ValidColors = ValidColors;
					picker.SelectedColors = selectedColors;
					if (picker.ShowDialog() == DialogResult.OK)
					{
						if (!picker.SelectedColors.Any())
						{
							DeletePoint();
						}
						else
						{
							double position = colorPoints.First().Position;
							foreach (var colorPoint in colorPoints)
							{
								holdValue.Colors.Remove(colorPoint);
							}

							foreach (Color selectedColor in picker.SelectedColors)
							{
								ColorPoint newPoint = new ColorPoint(selectedColor, position);
								holdValue.Colors.Add(newPoint);
							}
							SetColorGradientValue(holdValue);
						}
					}
				}
			}
			else
			{
				if (colorPointIndexes.Count > 1)
				{
					//messageBox Arguments are (Text, Title, No Button Visible, Cancel Button Visible)
					MessageBoxForm.msgIcon = SystemIcons.Error; //this is used if you want to add a system icon to the message form.
					var messageBox = new MessageBoxForm("Non-discrete color gradient, >1 selected point. oops! please report it.", "Delete library gradient?", false, false);
					messageBox.ShowDialog();
				}
				holdValue = new ColorGradient(GetColorGradientValue());
				ColorPoint pt = holdValue.Colors[colorPointIndexes.FirstOrDefault()];
				if (pt == null)
					return;
				using (ColorPicker frm = new ColorPicker(Mode, Fader))
				{
					frm.LockValue_V = false;
					frm.Color = pt.Color;
					if (frm.ShowDialog() == DialogResult.OK)
					{
						pt.Color = frm.Color;
						Mode = frm.SecondaryMode;
						Fader = frm.PrimaryFader;
						SetColorGradientValue(holdValue);
					}
				}
			}
		}

		private int SelectedIndex
		{
			get
			{
				var selectedPoint = _points.FirstOrDefault(x => x.SliderShape.IsFocused);
				return selectedPoint != null ? _points.IndexOf(selectedPoint) : -1;
			}
		}

		private void ReloadPoints()
		{
			if (GetColorGradientValue() != null)
			{
				ClearPoints();
				var value = GetColorGradientValue();
				List<ColorPoint> sortedColors = new List<ColorPoint>(value.Colors.SortedArray());
				// we'll assume they're sorted, so any colorpoints at the same position are contiguous in the array
				for (int i = 0; i < sortedColors.Count; i++)
				{
					ColorPoint currentPoint = sortedColors[i];
					double currentPos = currentPoint.Position;
					List<int> indexes = new List<int>();

					indexes.Add(value.Colors.IndexOf(currentPoint));
					while (i + 1 < sortedColors.Count && sortedColors[i + 1].Position == currentPos)
					{
						indexes.Add(value.Colors.IndexOf(sortedColors[i + 1]));
						i++;
					}

					var point = AddPoint(currentPos);
					point.Tag = indexes;
				}
			}
		}


		protected void ClearPoints()
		{
			_points.ToList().ForEach(OnRemoveItem);
			_points.Clear();
		}

		private SliderPoint AddPoint(double position, bool isSelected = false)
		{
			var point = new SliderPoint(position, _canvas);

			if (point.Center >= 0 && point.Center <= _canvas.Width)
			{
				_points.Add(point);
				_points.Sort(new SliderPointPositionComparer());
				OnAddItem(point);
				if (isSelected)
				{
					point.SliderShape.Focus();
				}
			}

			return point;
		}

		private void DeletePoint()
		{
			var selectedIndex = SelectedIndex;
			if (selectedIndex >= 0)
			{
				var selectedPoint = _points[selectedIndex];
				List<int> colorPointIndexes = (List<int>) selectedPoint.Tag;
				var holdValue = new ColorGradient(GetColorGradientValue());

				List<ColorPoint> colorPoints = new List<ColorPoint>();
				foreach (int index in colorPointIndexes)
				{
					ColorPoint pt = holdValue.Colors[index];
					if (pt == null)
						continue;
					colorPoints.Add(pt);
				}

				foreach (var colorpoint in colorPoints)
				{
					holdValue.Colors.Remove(colorpoint);
				}
				SetColorGradientValue(holdValue);
			}
		}

		protected void AddColorPoint(double pos)
		{
			List<ColorPoint> newColorPoints;
			if (IsDiscrete)
			{
				newColorPoints = new List<ColorPoint>();

				if (ValidColors.Any())
				{
					newColorPoints.Add(new ColorPoint(ValidColors.FirstOrDefault(), pos));
				}

			}
			else
			{
				newColorPoints = new List<ColorPoint> { new ColorPoint(GetColorGradientValue().GetColorAt((float)pos), pos) };
			}

			var holdValue = new ColorGradient(GetColorGradientValue());
			foreach (ColorPoint newColorPoint in newColorPoints)
			{
				holdValue.Colors.Add(newColorPoint);
			}

			SetColorGradientValue(holdValue);
		}

		private void UpdateImage(ColorGradient colorGradient)
		{
			if (IsInitialized)
			{
				var template = Template;
				var imageControl = (Image)template.FindName("GradientImage", this);
				if (imageControl != null)
				{
					imageControl.Source = (BitmapImage)Converter.Convert(colorGradient, null, IsDiscrete, null);
				}
			}
		}
	}
}
