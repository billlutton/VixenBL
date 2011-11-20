﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Vixen.Sys;

namespace VixenModules.App.Scheduler {
	partial class ScheduleItemEditForm : Form {
		private ScheduleItem _scheduleItem;
		private bool _internal;
		private Sequence _sequence;

		//private const string TIME_SPAN_FORMAT_STRING = @"hh\:mm";
		private readonly TimeSpan END_TIME_DELTA = TimeSpan.FromHours(6);
		private readonly TimeSpan END_TIME_INTERVAL = TimeSpan.FromMinutes(30);

		public ScheduleItemEditForm(ScheduleItem scheduleItem) {
			InitializeComponent();

			_ScheduleItem = scheduleItem;
		}

		private DateTime _StartDate {
			get { return dateTimePickerStartDate.Value; }
			set { dateTimePickerStartDate.Value = value; }
		}

		private DateTime? _EndDate {
			get {
				if(dateTimePickerEndDate.Enabled) {
					return dateTimePickerEndDate.Value;
				}
				return null;
			}
			set {
				if(value == null) {
					dateTimePickerEndDate.Enabled = false;
				} else {
					dateTimePickerEndDate.Value = value.Value;
					dateTimePickerEndDate.Enabled = true;
				}
			}
		}

		private TimeSpan? _StartTime {
			get { return _StringToTimeSpan(comboBoxStartTime.Text); }
			set { comboBoxStartTime.Text = _TimeSpanToString(value); }
		}

		//*** validation before assigning everything
		private TimeSpan? _EndTime {
			get {
				if(checkBoxRepeat.Checked) {
					return _StringToTimeSpan(comboBoxEndTime.Text);
				}
				return null;
			}
			set { comboBoxEndTime.Text = _TimeSpanToString(value); }
		}

		private int? _Interval {
			get {
				if(checkBoxInterval.Checked) return int.Parse(textBoxInterval.Text);
				return null;
			}
			set {
				textBoxInterval.Text = (value != null) ? value.ToString() : "";
			}
		}

		private RecurrenceType _RecurrenceType {
			get {
				if(checkBoxNoRecurrence.Checked) return RecurrenceType.None;
				return (RecurrenceType)comboBoxDateUnit.SelectedIndex + 1;
			}
			set {
				checkBoxNoRecurrence.Checked = false;
				recurrenceControls.SelectedIndex = (int)value;
				comboBoxDateUnit.SelectedIndex = (int)value - 1;
				dateTimePickerEndDate.Enabled = true;
				//_EndDate = _StartDate;
				switch(value) {
					case RecurrenceType.None:
						_EndDate = null;
						checkBoxNoRecurrence.Checked = true;
						break;
					case RecurrenceType.Daily:
						break;
					case RecurrenceType.Weekly:
						break;
					case RecurrenceType.MonthlyDateOfMonth:
						break;
					case RecurrenceType.MonthlyDayOfWeek:
						radioButtonWeekDayCount.Checked = true;
						break;
				}
			}
		}

		private int _DateUnitCount {
			get {
				if(comboBoxDateUnitCount.SelectedIndex == 0) return 1;
				return int.Parse(comboBoxDateUnitCount.Text);
			}
			set {
				if(value <= 1) {
					comboBoxDateUnitCount.SelectedIndex = 0;
				} else {
					comboBoxDateUnitCount.Text = value.ToString();
				}
			}
		}

		private int _DayCount {
			get {
				return comboBoxDayCount.SelectedIndex + 1;
			}
			set {
				comboBoxDayCount.SelectedIndex = value - 1;
			}
		}

		private bool _RepeatsWithinBlock {
			get { return checkBoxRepeat.Checked; }
			set { 
				checkBoxRepeat.Checked = value;
				if(!value) {
					_EndTime = null;
				}
			}
		}

		private bool _RepeatsOnInterval {
			get { return checkBoxInterval.Checked; }
			set {
				checkBoxInterval.Checked = value;
				if(!value) {
					_Interval = null;
				}
			}
		}

		private ScheduleItem _ScheduleItem {
			get { return _scheduleItem; }
			set {
				_scheduleItem = value;

				_StartDate = value.StartDate;
				_EndDate = value.EndDate;
				_DateUnitCount = value.DateUnitCount;

				_StartTime = value.RunStartTime;

				_EndTime = value.RunEndTime;
				_RepeatsWithinBlock = value.RepeatsWithinBlock;

				_Interval = value.RepeatIntervalMinutes;
				_RepeatsOnInterval = value.RepeatsOnInterval;

				_RecurrenceType = value.RecurrenceType;

				switch(value.RecurrenceType) {
					case RecurrenceType.None:
						break;
					case RecurrenceType.Daily:
						break;
					case RecurrenceType.Weekly:
						_WeeklyDay = value.DayDate;
						break;
					case RecurrenceType.MonthlyDateOfMonth:
						_MonthlyDate = value.DayDate;
						break;
					case RecurrenceType.MonthlyDayOfWeek:
						_MonthlyDay = value.DayDate;
						_DayCount = value.DayCount;
						break;
				}

				try {
					_Sequence = Sequence.Load(value.SequenceFilePath);
				} catch {
					_Sequence = null;
				}
			}
		}

		private Sequence _LoadSequence(string filePath) {
			Cursor = Cursors.WaitCursor;
			try {
				return Sequence.Load(filePath);
			} catch(Exception ex) {
				MessageBox.Show(ex.Message, "Vixen Scheduler", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				return null;
			} finally {
				Cursor = Cursors.Default;
			}
		}

		private int _WeeklyDay {
			get {
				if(radioButtonSunday.Checked) return 0;
				else if(radioButtonMonday.Checked) return 1;
				else if(radioButtonTuesday.Checked) return 2;
				else if(radioButtonWednesday.Checked) return 3;
				else if(radioButtonThursday.Checked) return 4;
				else if(radioButtonFriday.Checked) return 5;
				else if(radioButtonSaturday.Checked) return 6;
				else return 0;
			}
			set {
				switch(value) {
					case 0:
						radioButtonSunday.Checked = true;
						break;
					case 1:
						radioButtonMonday.Checked = true;
						break;
					case 2:
						radioButtonTuesday.Checked = true;
						break;
					case 3:
						radioButtonWednesday.Checked = true;
						break;
					case 4:
						radioButtonThursday.Checked = true;
						break;
					case 5:
						radioButtonFriday.Checked = true;
						break;
					case 6:
						radioButtonSaturday.Checked = true;
						break;
				}
			}
		}

		private int _MonthlyDay {
			get { return comboBoxDow.SelectedIndex; }
			set { comboBoxDow.SelectedIndex = value; }
		}

		private int _MonthlyDate {
			get {
				if(radioButtonLastDay.Checked) {
					return ScheduleService.LastDay;
				} else if(radioButtonSpecificDate.Checked) {
					return int.Parse(textBoxSpecificDate.Text);
				}
				return 0;
			}
			set {
				if(value == ScheduleService.LastDay) {
					radioButtonLastDay.Checked = true;
				} else {
					radioButtonSpecificDate.Checked = true;
					textBoxSpecificDate.Text = value.ToString();
				}
			}
		}
		
		private Sequence _Sequence {
			get { return _sequence; }
			set {
				_sequence = value;
				if(_sequence!= null) {
					labelWhat.ForeColor = Color.Black;
					labelWhat.Text = _sequence.Name;
				} else {
					labelWhat.ForeColor = Color.Red;
					labelWhat.Text = "Not Selected";
				}
			}
		}

		private DateTime _TimeSpanToDateTime(TimeSpan timeSpan) {
			return DateTime.Today + timeSpan;
		}

		private TimeSpan? _StringToTimeSpan(string value) {
			DateTime date;
			if(DateTime.TryParse(value, out date)) {
				return date.TimeOfDay;
			}
			return null;
		}

		private string _TimeSpanToString(TimeSpan? value) {
			return (value != null) ? _TimeSpanToDateTime(value.Value).ToShortTimeString() : "";
		}

		private void checkBoxRepeat_CheckedChanged(object sender, EventArgs e) {
			panelInterval.Enabled = checkBoxRepeat.Checked;
		}

		private void comboBoxDateUnit_SelectedIndexChanged(object sender, EventArgs e) {
			recurrenceControls.SelectedIndex = comboBoxDateUnit.SelectedIndex + 1;
		}

		private void checkBoxNoRecurrence_CheckedChanged(object sender, EventArgs e) {
			if(!_internal) {
				_internal = true;
				if(checkBoxNoRecurrence.Checked) {
					_RecurrenceType = RecurrenceType.None;
					panelRecurrence.Enabled = false;
				} else {
					//dateTimePickerEndDate.Enabled = true;
					_RecurrenceType = RecurrenceType.Daily;
					panelRecurrence.Enabled = true;
				}
				_internal = false;
			}
		}

		private void buttonOK_Click(object sender, EventArgs e) {
			_ScheduleItem.StartDate = _StartDate;
			if(_EndDate != null) {
				_ScheduleItem.EndDate = _EndDate.Value;
			}

			_ScheduleItem.RecurrenceType = _RecurrenceType;
			_ScheduleItem.DateUnitCount = _DateUnitCount;
			
			switch(_RecurrenceType) {
				case RecurrenceType.None:
					break;
				case RecurrenceType.Daily:
					break;
				case RecurrenceType.Weekly:
					_ScheduleItem.DayDate = _WeeklyDay;
					break;
				case RecurrenceType.MonthlyDateOfMonth:
					_ScheduleItem.DayDate = _MonthlyDate;
					break;
				case RecurrenceType.MonthlyDayOfWeek:
					_ScheduleItem.DayDate = _MonthlyDay;
					_ScheduleItem.DayCount = _DayCount;
					break;
			}

			_ScheduleItem.RunStartTime = _StartTime.GetValueOrDefault();
			if(_EndTime != null) {
				_ScheduleItem.RunEndTime = _EndTime.Value;
			}
			_ScheduleItem.RepeatsWithinBlock = _RepeatsWithinBlock;
			if(_Interval != null) {
				_ScheduleItem.RepeatIntervalMinutes = _Interval.Value;
			}
			_ScheduleItem.RepeatsOnInterval = _RepeatsOnInterval;
			_ScheduleItem.SequenceFilePath = (_Sequence != null) ? _Sequence.FilePath : null;
		}

		private void comboBoxStartTime_Leave(object sender, EventArgs e) {
			if(_StartTime.HasValue) {
				bool invalidEndTime =
					_EndTime == null ||
					_EndTime < _StartTime;
				if(invalidEndTime) {
					comboBoxEndTime.Items.Clear();
					TimeSpan endTime = _StartTime.Value + END_TIME_DELTA;
					for(TimeSpan time = _StartTime.Value; time <= endTime; time += END_TIME_INTERVAL) {
						comboBoxEndTime.Items.Add(_TimeSpanToString(time));
					}
				}
			}
		}

		private void buttonSelectSequence_Click(object sender, EventArgs e) {
			openFileDialog.InitialDirectory = Sequence.DefaultDirectory;
			if(openFileDialog.ShowDialog() == DialogResult.OK) {
				Sequence sequence = _LoadSequence(openFileDialog.FileName);
				_Sequence = sequence;
			}
		}

		private void buttonSelectProgram_Click(object sender, EventArgs e) {

		}

		private void buttonNewProgram_Click(object sender, EventArgs e) {

		}
	}
}