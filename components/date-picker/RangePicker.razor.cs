﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AntDesign.core.Extensions;
using AntDesign.Core.Extensions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace AntDesign
{
    public partial class RangePicker<TValue> : DatePickerBase<TValue>
    {
        private TValue _value;
        private TValue _lastValue;
        private TValue _swpValue;

        /// <summary>
        /// Gets or sets the value of the input. This should be used with two-way binding.
        /// </summary>
        /// <example>
        /// @bind-Value="model.PropertyName"
        /// </example>
        [Parameter]
        public override sealed TValue Value
        {
            get { return _value; }
            set
            {
                TValue orderedValue = SortValue(value);

                var hasChanged = _lastValue is null || (IsNullable ? !Enumerable.SequenceEqual(orderedValue as DateTime?[], _lastValue as DateTime?[]) :
                                                            !Enumerable.SequenceEqual(orderedValue as DateTime[], _lastValue as DateTime[]));
                if (hasChanged)
                {
                    _value = orderedValue;

                    _lastValue ??= CreateInstance();
                    Array.Copy(orderedValue as Array, _lastValue as Array, 2);

                    GetIfNotNull(_value, 0, (notNullValue) => PickerValues[0] = notNullValue);
                    GetIfNotNull(_value, 1, (notNullValue) => PickerValues[1] = notNullValue);

                    OnValueChange(orderedValue);
                }
            }
        }

        private DateTime[] _pickerValuesAfterInit = new DateTime[2];

        [Parameter]
        public EventCallback<DateRangeChangedEventArgs> OnChange { get; set; }

        private bool ShowFooter => !IsShowTime && (RenderExtraFooter != null || ShowRanges);

        private bool ShowRanges => Ranges != null;

        public RangePicker()
        {
            IsRange = true;

            DisabledDate = (date) =>
            {
                var array = Value as Array;

                int? index = null;

                if (_pickerStatus[0].IsValueSelected && _inputEnd.IsOnFocused)
                {
                    index = 0;
                }
                else if (_pickerStatus[1].IsValueSelected && _inputStart.IsOnFocused)
                {
                    index = 1;
                }

                if (index is null)
                {
                    return false;
                }

                DateTime? value = null;

                GetIfNotNull(Value, index.Value, notNullValue =>
                {
                    value = notNullValue;
                });

                if (value is null)
                {
                    return false;
                }

                var date1 = date.Date;
                var date2 = ((DateTime)value).Date;

                if (Picker == DatePickerType.Week)
                {
                    var date1Week = DateHelper.GetWeekOfYear(date1, Locale.FirstDayOfWeek);
                    var date2Week = DateHelper.GetWeekOfYear(date2, Locale.FirstDayOfWeek);
                    return index == 0 ? date1Week < date2Week : date1Week > date2Week;
                }
                else
                {
                    var formattedDate1 = DateHelper.FormatDateByPicker(date1, Picker);
                    var formattedDate2 = DateHelper.FormatDateByPicker(date2, Picker);
                    return index == 0 ? formattedDate1 < formattedDate2 : formattedDate1 > formattedDate2;
                }
            };
        }

        private async Task OnInputClick(int index)
        {
            _duringFocus = false;
            if (_duringManualInput)
            {
                return;
            }
            _openingOverlay = !_dropDown.IsOverlayShow();

            //Reset Picker to default in case the picker value was changed
            //but no value was selected (for example when a user clicks next
            //month but does not select any value)
            if (UseDefaultPickerValue[index] && DefaultPickerValue != null)
            {
                PickerValues[index] = _pickerValuesAfterInit[index];
            }
            await _dropDown.Show();

            if (index == 0)
            {
                // change start picker value
                if (!_inputStart.IsOnFocused && _pickerStatus[index].IsValueSelected && !UseDefaultPickerValue[index])
                {
                    GetIfNotNull(Value, index, notNullValue =>
                    {
                        ChangePickerValue(notNullValue, index);
                    });
                }

                ChangeFocusTarget(true, false);
            }
            else
            {
                // change end picker value
                if (!_inputEnd.IsOnFocused && _pickerStatus[index].IsValueSelected && !UseDefaultPickerValue[index])
                {
                    GetIfNotNull(Value, index, notNullValue =>
                    {
                        ChangePickerValue(notNullValue, index);
                    });
                }

                ChangeFocusTarget(false, true);
            }
        }

        private DateTime? _cacheDuringInput;
        private DateTime _pickerValueCache;

        protected void OnInput(ChangeEventArgs args, int index = 0)
        {
            if (args == null)
            {
                return;
            }
            var array = Value as Array;
            if (!_duringManualInput)
            {
                _duringManualInput = true;
                _cacheDuringInput = array.GetValue(index) as DateTime?;
                _pickerValueCache = PickerValues[index];
            }
            if (FormatAnalyzer.TryPickerStringConvert(args.Value.ToString(), out DateTime changeValue, false)
                && IsValidRange(changeValue, index, array))
            {
                array.SetValue(changeValue, index);
                _cacheDuringInput = changeValue;
                ChangePickerValue(changeValue, index);

                if (_isNotifyFieldChanged && (Form?.ValidateOnChange == true))
                {
                    EditContext?.NotifyFieldChanged(FieldIdentifier);
                }

                StateHasChanged();
            }
        }

        /// <summary>
        /// Method is called via EventCallBack if the keyboard key is no longer pressed inside the Input element.
        /// </summary>
        /// <param name="e">Contains the key (combination) which was pressed inside the Input element</param>
        /// <param name="index">Refers to picker index - 0 for starting date, 1 for ending date</param>
        protected async Task OnKeyDown(KeyboardEventArgs e, int index)
        {
            if (e == null) throw new ArgumentNullException(nameof(e));

            var key = e.Key.ToUpperInvariant();
            if (key == "ENTER" || key == "TAB" || key == "ESCAPE")
            {
                if (_duringManualInput)
                {
                    //A scenario when there are a lot of controls;
                    //It may happen that incorrect values were entered into one of the input
                    //followed by ENTER key. This event may be fired before input manages
                    //to get the value. Here we ensure that input will get that value.
                    await Task.Delay(5);
                    _duringManualInput = false;
                }
                var input = (index == 0 ? _inputStart : _inputEnd);
                if (string.IsNullOrWhiteSpace(input.Value))
                {
                    ClearValue(index, false);
                }
                else if (!await TryApplyInputValue(index, input.Value))
                    return;

                if (key == "ESCAPE" && _dropDown.IsOverlayShow())
                {
                    Close();
                    await Js.FocusAsync(input.Ref);
                    return;
                }

                if (index == 1)
                {
                    if (key != "TAB")
                    {
                        //needed only in wasm, details: https://github.com/dotnet/aspnetcore/issues/30070
                        await Task.Yield();
                        await Js.InvokeVoidAsync(JSInteropConstants.InvokeTabKey);
                        Close();
                    }
                    else if (!e.ShiftKey)
                    {
                        Close();
                        AutoFocus = false;
                    }
                }
                if (index == 0)
                {
                    if (key == "TAB" && e.ShiftKey)
                    {
                        Close();
                        AutoFocus = false;
                    }
                    else if (key != "TAB")
                    {
                        await Blur(0);
                        await Focus(1);
                    }
                }
                return;
            }
            if (key == "ARROWDOWN" && !_dropDown.IsOverlayShow())
            {
                await _dropDown.Show();
                return;
            }
            if (key == "ARROWUP" && _dropDown.IsOverlayShow())
            {
                Close();
                await Task.Yield();
                AutoFocus = true;
                return;
            }
        }

        private async Task<bool> TryApplyInputValue(int index, string inputValue)
        {
            if (FormatAnalyzer.TryPickerStringConvert(inputValue, out DateTime changeValue, false))
            {
                var array = Value as Array;
                array.SetValue(changeValue, index);
                var validationSuccess = await ValidateRange(index, changeValue, array);
                if (OnChange.HasDelegate)
                {
                    await OnChange.InvokeAsync(new DateRangeChangedEventArgs
                    {
                        Dates = new DateTime?[] { array.GetValue(0) as DateTime?, array.GetValue(1) as DateTime? },
                        DateStrings = new string[] { GetInputValue(0), GetInputValue(1) }
                    });
                }
                return validationSuccess;
            }
            return false;
        }

        private async Task<bool> ValidateRange(int index, DateTime newDate, Array array)
        {
            if (index == 0 && array.GetValue(1) is not null && ((DateTime)array.GetValue(1)).CompareTo(newDate) < 0)
            {
                ClearValue(1, false);
                await Blur(0);
                await Focus(1);
                return false;
            }
            else if (index == 1)
            {
                if (array.GetValue(0) is not null && newDate.CompareTo((DateTime)array.GetValue(0)) < 0)
                {
                    ClearValue(0, false);
                    await Blur(1);
                    await Focus(0);
                    return false;
                }
                else if (array.GetValue(0) is null)
                {
                    await Blur(1);
                    await Focus(0);
                    return false;
                }
            }
            return true;
        }

        private async Task OnFocus(int index)
        {
            _duringFocus = true;
            if (index == 0)
            {
                if (!_inputStart.IsOnFocused)
                {
                    await Blur(1);
                    await Focus(0);
                }
            }
            else
            {
                if (!_inputEnd.IsOnFocused)
                {
                    await Blur(0);
                    await Focus(1);
                }
            }
            AutoFocus = true;
        }

        protected override async Task OnBlur(int index)
        {
            //Await for Focus event - if it is going to happen, it will be
            //right after OnBlur. Best way to achieve that is to wait.
            //Task.Yield() does not work here.
            await Task.Delay(1);
            if (_duringFocus)
            {
                _duringFocus = false;
                _shouldRender = false;
                return;
            }
            if (_openingOverlay)
            {
                return;
            }

            if (_duringManualInput)
            {
                var array = Value as Array;

                if (!array.GetValue(index).Equals(_cacheDuringInput))
                {
                    //reset picker to Value
                    if (IsNullable)
                        array.SetValue(_cacheDuringInput, index);
                    else
                        array.SetValue(_cacheDuringInput.GetValueOrDefault(), index);

                    _pickerStatus[index].IsValueSelected = !(Value is null && (DefaultValue is not null || DefaultPickerValue is not null));
                    ChangePickerValue(_pickerValueCache, index);
                }
                _duringManualInput = false;
            }

            AutoFocus = false;
        }

        protected override void OnInitialized()
        {
            base.OnInitialized();
            RangePickerDefaults.ProcessDefaults(Value, DefaultValue, DefaultPickerValue, PickerValues, UseDefaultPickerValue);
            _pickerValuesAfterInit[0] = PickerValues[0];
            _pickerValuesAfterInit[1] = PickerValues[1];
            if (_value == null)
            {
                _value = CreateInstance();
                ValueChanged.InvokeAsync(_value);
            }
        }

        /// <summary>
        /// Handle change of values.
        /// When values are changed, PickerValues should point to those new values
        /// or current date if no values were passed.
        /// </summary>
        /// <param name="value"></param>
        protected override void OnValueChange(TValue value)
        {
            base.OnValueChange(value);
            //reset all only if not changed using picker
            if (_inputStart?.IsOnFocused != true && _inputEnd?.IsOnFocused != true) // is null or false
            {
                UseDefaultPickerValue[0] = false;
                UseDefaultPickerValue[1] = false;
                _pickerStatus[0].IsValueSelected = true;
                _pickerStatus[1].IsValueSelected = true;
            }
        }

        /// <summary>
        /// Get value by picker index
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public override DateTime? GetIndexValue(int index)
        {
            if (Value != null)
            {
                var array = Value as Array;
                var indexValue = array.GetValue(index);

                if (indexValue == null)
                {
                    return null;
                }

                return Convert.ToDateTime(indexValue, CultureInfo);
            }
            else if (!IsTypedValueNull(DefaultValue, index, out var defaultValue))
            {
                return defaultValue;
            }
            return null;
        }

        private static bool IsTypedValueNull(TValue value, int index, out DateTime? outValue)
        {
            outValue = (DateTime?)(value as Array)?.GetValue(index);
            return outValue == null;
        }

        public override void ChangeValue(DateTime value, int index = 0)
        {
            bool isValueInstantiated = Value == null;
            if (isValueInstantiated)
            {
                Value = CreateInstance();
            }
            UseDefaultPickerValue[index] = false;
            var array = Value as Array;

            array.SetValue(value, index);

            //if Value was just now instantiated then set the other index to existing DefaultValue
            if (isValueInstantiated && IsRange && DefaultValue != null)
            {
                var arrayDefault = DefaultValue as Array;
                int oppositeIndex = index == 1 ? 0 : 1;
                array.SetValue(arrayDefault.GetValue(oppositeIndex), oppositeIndex);
            }

            _pickerStatus[index].IsValueSelected = true;

            if (!IsShowTime && Picker != DatePickerType.Time)
            {
                _pickerStatus[index].IsNewValueSelected = true;

                if (_pickerStatus[0].IsNewValueSelected && _pickerStatus[1].IsNewValueSelected)
                {
                    Close();
                }
                // if the other DatePickerInput is disabled, then close picker panel
                else if (IsDisabled(Math.Abs(index - 1)))
                {
                    Close();
                }
            }

            if (OnChange.HasDelegate)
            {
                OnChange.InvokeAsync(new DateRangeChangedEventArgs
                {
                    Dates = new DateTime?[] { array.GetValue(0) as DateTime?, array.GetValue(1) as DateTime? },
                    DateStrings = new string[] { GetInputValue(0), GetInputValue(1) }
                });
            }

            if (_isNotifyFieldChanged && (Form?.ValidateOnChange == true))
            {
                EditContext?.NotifyFieldChanged(FieldIdentifier);
            }
        }

        public override void ClearValue(int index = -1, bool closeDropdown = true)
        {
            _isSetPicker = false;

            var array = CurrentValue as Array;
            int[] indexToClear = index == -1 ? new[] { 0, 1 } : new[] { index };

            foreach (var i in indexToClear)
            {
                if (!IsNullable && DefaultValue != null)
                {
                    var defaults = DefaultValue as Array;
                    array.SetValue(defaults.GetValue(i), i);
                }
                else
                {
                    array.SetValue(default, i);
                }
                _pickerStatus[i].IsValueSelected = false;
                PickerValues[i] = _pickerValuesAfterInit[i];
                ResetPlaceholder(i);
            }

            if (closeDropdown)
                Close();
            if (OnClearClick.HasDelegate)
                OnClearClick.InvokeAsync(null);

            _dropDown.SetShouldRender(true);
        }

        private void GetIfNotNull(TValue value, int index, Action<DateTime> notNullAction)
        {
            var array = value as Array;
            var indexValue = array.GetValue(index);

            if (!IsNullable)
            {
                DateTime dateTime = Convert.ToDateTime(indexValue, CultureInfo);
                if (dateTime != DateTime.MinValue)
                {
                    notNullAction?.Invoke(dateTime);
                }
            }
            if (IsNullable && indexValue != null)
            {
                notNullAction?.Invoke(Convert.ToDateTime(indexValue, CultureInfo));
            }
        }

        private TValue CreateInstance()
        {
            if (DefaultValue is not null)
                return (TValue)(DefaultValue as Array).Clone();

            if (IsNullable)
            {
                return (TValue)Array.CreateInstance(typeof(DateTime?), 2).Clone();
            }
            else
            {
                return (TValue)Array.CreateInstance(typeof(DateTime), 2).Clone();
            }
        }

        protected override bool TryParseValueFromString(string value, out TValue result, out string validationErrorMessage)
        {
            result = default;
            validationErrorMessage = $"{FieldIdentifier.FieldName} field isn't valid.";

            if (string.IsNullOrEmpty(value))
            {
                return false;
            }

            string[] values = value.Split(",");

            if (values.Length != 2)
            {
                return false;
            }

            var success0 = BindConverter.TryConvertTo<DateTime>(values[0], CultureInfo, out var dateTime0);
            var success1 = BindConverter.TryConvertTo<DateTime>(values[1], CultureInfo, out var dateTime1);

            if (success0 && success1)
            {
                result = CreateInstance();

                var array = result as Array;

                array.SetValue(dateTime0, 0);
                array.SetValue(dateTime1, 1);

                validationErrorMessage = null;

                return true;
            }

            return false;
        }

        private void OverlayVisibleChange(bool visible)
        {
            _openingOverlay = false;
            _duringFocus = false;
            OnOpenChange.InvokeAsync(visible);
            InvokeInternalOverlayVisibleChanged(visible);
        }

        private async Task OnSuffixIconClick()
        {
            await Focus();
            await OnInputClick(0);
        }

        private bool IsValidRange(DateTime newValue, int newValueIndex, Array rangeValues)
        {
            return newValueIndex switch
            {
                0 when newValue > (rangeValues.GetValue(1) as DateTime?) => false,
                1 when newValue < (rangeValues.GetValue(0) as DateTime?) => false,
                _ => true
            };
        }
    }
}
